/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using BbsNavigator.Models;
using BbsNavigator.Transfers;
using System.Windows;

namespace BbsNavigator.Views;

public partial class BbsTerminalView
{
    private readonly LoginPromptBuffer _loginPrompts = new();
    private CancellationTokenSource? _textCancellation;
    private Task<bool>? _textOperation;
    private bool _textPreviewOpen;
    private int _connectionEpoch;

    private async void SessionCommandBox_OnCommandExecuted(object sender, Mosaic.UI.Wpf.Controls.CommandExecutedEventArgs e)
    {
        e.Handled = true;
        await RunTextOperationAsync(
            token =>
            {
                ScrollLockToggle.IsChecked = false;
                Terminal.ScrollToBottom();
                return SendPacedAsync(e.Command + "\r", 0, 0, false, token);
            },
            "Command sent.");
    }

    /// <summary>
    /// Gets whether this terminal's transport is currently connected.
    /// </summary>
    public bool IsConnected => !_disposed && _connection.IsConnected;

    private async Task<DownloadResumeAction> ChooseDownloadResumeAsync(PartialDownloadInfo partial, CancellationToken token)
    {
        return await Dispatcher.InvokeAsync(() =>
        {
            token.ThrowIfCancellationRequested();
            var answer = Mosaic.UI.Wpf.Controls.MessageBox.Show(
                $"An interrupted download of {partial.FileName} has {partial.BytesReceived:N0} of {partial.TotalBytes:N0} bytes.\n\nResume it? Saved bytes will be verified first. If verification is unavailable or fails, the download restarts.\n\nYes: verify and resume\nNo: restart from the beginning\nCancel: keep the partial file and stop",
                "Resume download", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            token.ThrowIfCancellationRequested();
            return answer switch
            {
                MessageBoxResult.Yes => DownloadResumeAction.Resume,
                MessageBoxResult.No => DownloadResumeAction.Restart,
                _ => DownloadResumeAction.Cancel
            };
        });
    }

    private int CharacterDelay => Profile.PasteCharacterDelayOverride < 0
        ? Math.Clamp(_settings.PastePacingMilliseconds, 0, 1000)
        : Math.Clamp(Profile.PasteCharacterDelayOverride, 0, 1000);

    /// <summary>
    /// Cancels the current login, paste, or composed-message send immediately.
    /// </summary>
    public void StopSending() => _textCancellation?.Cancel();

    private void StopText_OnClick(object sender, RoutedEventArgs e) => StopSending();

    private async Task StopSendingAsync()
    {
        StopSending();
        if (_textOperation != null) await _textOperation;
    }

    private Task<bool> RunTextOperationAsync(Func<CancellationToken, Task> operation, string completed)
    {
        if (_disposed || _manualDisconnect || !_connection.IsConnected || _transferActive || _textCancellation != null)
        {
            ShowTransientStatus("Connect to the board and finish the current send or transfer first.");
            return Task.FromResult(false);
        }
        return _textOperation = RunTextOperationCoreAsync(operation, completed);
    }

    private async Task<bool> RunTextOperationCoreAsync(Func<CancellationToken, Task> operation, string completed)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken);
        _textCancellation = cancellation;
        Terminal.SendKeyboardInputToConnection = false;
        StopTextButton.Visibility = Visibility.Visible;
        UpdateTransferButtons();
        try
        {
            await operation(cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!_disposed) ShowTransientStatus(completed);
            return true;
        }
        catch (OperationCanceledException)
        {
            if (!_disposed) ShowTransientStatus("Sending stopped. Text already sent cannot be recalled.");
            return false;
        }
        catch (Exception ex)
        {
            if (!_disposed) ShowTransientStatus($"Sending stopped: {ex.Message}");
            return false;
        }
        finally
        {
            _textCancellation = null;
            if (!_disposed)
            {
                Terminal.SendKeyboardInputToConnection = !_transferActive;
                StopTextButton.Visibility = Visibility.Collapsed;
                UpdateTransferButtons();
            }
        }
    }

    private async Task SendPacedAsync(string text, int characterDelay, int lineDelay, bool showProgress, CancellationToken token)
    {
        string problem = TerminalText.EncodingProblem(text, Profile.TerminalEncoding.ToEncoding());
        if (problem.Length > 0)
            throw new InvalidOperationException(showProgress ? problem : "Login text cannot be represented by the session encoding.");
        await TerminalText.SendAsync(text, characterDelay, lineDelay, async (chunk, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            if (!_connection.IsConnected) throw new InvalidOperationException("The connection closed.");
            await Terminal.SendTextAsync(chunk, 0, ct);
        }, (sent, total) =>
        {
            if (showProgress && !_disposed) StatusText.Text = $"Sending {sent:N0} / {total:N0} characters…";
        }, token);
    }

    /// <summary>
    /// Reviews and sends prepared text using this board's encoding and pacing preferences.
    /// </summary>
    public async Task<bool> SendPreparedTextAsync(string text, bool preview = true)
    {
        if (_disposed || !_connection.IsConnected || _transferActive || _textCancellation != null || _textPreviewOpen)
        {
            ShowTransientStatus("Connect to the board and finish the current send or transfer first.");
            return false;
        }
        if (string.IsNullOrEmpty(text)) return false;
        int connectionEpoch = _connectionEpoch;
        int characterDelay = CharacterDelay;
        int lineDelay = Math.Clamp(Profile.PasteLineDelayMilliseconds, 0, 10000);
        if (preview || TerminalText.EncodingProblem(text, Profile.TerminalEncoding.ToEncoding()).Length > 0)
        {
            _textPreviewOpen = true;
            try
            {
                var window = new TextSendWindow(text, Profile.Name, Profile.TerminalEncoding.ToEncoding(), characterDelay, lineDelay)
                { Owner = Window.GetWindow(this) };
                if (window.ShowDialog() != true) return false;
                text = window.TextToSend;
                characterDelay = window.CharacterDelay;
                lineDelay = window.LineDelay;
                if (window.SavePacing)
                {
                    Profile.PasteCharacterDelayOverride = characterDelay;
                    Profile.PasteLineDelayMilliseconds = lineDelay;
                }
            }
            finally { _textPreviewOpen = false; }
        }

        if (connectionEpoch != _connectionEpoch)
        {
            ShowTransientStatus("The connection changed while reviewing text. Open the BBS editor and preview again before sending.");
            return false;
        }

        return await RunTextOperationAsync(async token =>
        {
            bool bracketed = Terminal.IsBracketedPasteEnabled;
            if (bracketed) await Terminal.SendTextAsync("\x1b[200~", 0, token);
            try { await SendPacedAsync(text, characterDelay, lineDelay, true, token); }
            finally
            {
                if (bracketed && !_disposed && _connection.IsConnected)
                    await Terminal.SendTextAsync("\x1b[201~", 0);
            }
        }, "Text sent.");
    }

    private Task<bool> RunLoginAsync(bool automatic)
    {
        if (!automatic) _loginPrompts.KeepCurrentLine();
        return RunTextOperationAsync(async token =>
        {
            // Copy the steps so editing a profile cannot change an in-flight login.
            var steps = Profile.LoginSteps.Select(s => new LoginStep { Action = s.Action, Text = s.Text, Seconds = s.Seconds }).ToList();
            bool sequence = Profile.UseLoginSequence;
            bool needsCredentials = !sequence || steps.Any(s => s.Action is LoginAction.SendUsername or LoginAction.SendPassword);
            BbsCredentials? credentials = needsCredentials ? await GetCredentialsAsync() : null;
            token.ThrowIfCancellationRequested();
            if (sequence)
            {
                await LoginSequenceRunner.RunAsync(steps, credentials, _loginPrompts,
                    (text, ct) => SendPacedAsync(text, CharacterDelay, Profile.PasteLineDelayMilliseconds, false, ct),
                    status => StatusText.Text = status, token);
            }
            else
            {
                if (credentials == null) throw new InvalidOperationException("No saved credentials are available.");
                string text = _loginTokens.Replace(Profile.LoginMacro ?? string.Empty, match =>
                    match.Groups[1].Value.ToUpperInvariant() switch
                    {
                        "USERNAME" => credentials.UserName,
                        "PASSWORD" => credentials.Password,
                        _ => "\r"
                    });
                await SendPacedAsync(text, CharacterDelay, Profile.PasteLineDelayMilliseconds, false, token);
            }
        }, "Login sequence sent.");
    }
}
