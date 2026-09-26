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
using Mosaic.UI.Wpf.Scripting;

namespace BbsNavigator.Views;

public partial class BbsTerminalView
{
    private ScriptEnvironment? _aliasEnvironment;
    private CancellationTokenSource? _aliasCancellation;

    /// <summary>
    /// Occurs when the BBS sends text, with ANSI escape sequences removed. Raised on the
    /// connection's thread.
    /// </summary>
    internal event Action<string>? TextReceived;

    /// <summary>
    /// Gets or sets a callback that registers application objects, such as <c>win</c> and
    /// <c>panels</c>, into the environment alias scripts run in.
    /// </summary>
    public Action<ScriptEnvironment>? ConfigureScriptEnvironment { get; set; }

    /// <summary>
    /// Gets the token that ends alias script waits when scripts are stopped or the session closes.
    /// </summary>
    internal CancellationToken ScriptCancellationToken => _aliasCancellation?.Token ?? _lifetimeToken;

    /// <summary>
    /// Gets the text currently on the terminal screen.
    /// </summary>
    internal string ScreenText => Terminal.Text;

    /// <summary>
    /// Cancels every running alias script for this session. Scripts stop at their next wait or send.
    /// </summary>
    public void StopAliasScripts()
    {
        Interlocked.Exchange(ref _aliasCancellation, null)?.Cancel();
    }

    /// <summary>
    /// Runs the alias named by the first word of <paramref name="input"/>, if this board has one.
    /// </summary>
    /// <param name="input">The text entered in the command box.</param>
    /// <returns><see langword="true"/> when an enabled alias matched and its script was started.</returns>
    private bool TryRunAlias(string input)
    {
        if (Profile.Aliases.Count == 0 || !AliasInput.TryParse(input, out string name, out var arguments, out string remainder))
        {
            return false;
        }

        Alias? alias = Profile.Aliases.FirstOrDefault(a =>
            a.Enabled && string.Equals(a.AliasExpression.Trim(), name, StringComparison.OrdinalIgnoreCase));

        if (alias == null)
        {
            return false;
        }

        alias.Count++;
        _ = RunAliasAsync(alias.AliasExpression, AliasInput.Expand(alias.Command, arguments, remainder));
        return true;
    }

    private async Task RunAliasAsync(string name, string code)
    {
        if (_disposed)
        {
            return;
        }

        _aliasCancellation ??= CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken);
        CancellationToken token = _aliasCancellation.Token;

        try
        {
            await GetAliasEnvironment().ExecuteAsync(code, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            if (!_disposed)
            {
                ShowTransientStatus($"Alias '{name}' stopped.");
            }
        }
        catch (Exception ex)
        {
            if (!_disposed)
            {
                ShowTransientStatus($"Alias '{name}' failed: {(ex.InnerException ?? ex).Message}");
            }
        }
    }

    /// <summary>
    /// Gets the environment shared by this session's alias scripts, so values stored in
    /// <c>globals</c> by one alias are visible to the next.
    /// </summary>
    private ScriptEnvironment GetAliasEnvironment()
    {
        if (_aliasEnvironment != null)
        {
            return _aliasEnvironment;
        }

        var environment = new ScriptEnvironment();
        ConfigureScriptEnvironment?.Invoke(environment);
        environment.RegisterModule(new TerminalScriptCommands(this));
        return _aliasEnvironment = environment;
    }

    /// <summary>
    /// Sends text from a script using the session encoding. Text sends are not paced.
    /// </summary>
    /// <param name="text">The text to send.</param>
    internal Task SendScriptTextAsync(string text)
    {
        if (_disposed || !_connection.IsConnected)
        {
            throw new InvalidOperationException("The terminal is not connected.");
        }

        if (_transferActive)
        {
            throw new InvalidOperationException("A file transfer is in progress.");
        }

        return SendPacedAsync(text, 0, 0, false, ScriptCancellationToken);
    }

    /// <summary>
    /// Writes text to the screen as if the BBS had sent it.
    /// </summary>
    /// <param name="text">The text, which may contain ANSI escape sequences.</param>
    internal void EchoScriptText(string text)
    {
        if (!_disposed)
        {
            Terminal.Add(text);
        }
    }

    /// <summary>
    /// Shows a script's message in the status bar.
    /// </summary>
    /// <param name="message">The message.</param>
    internal void ShowScriptStatus(string message)
    {
        if (!_disposed)
        {
            ShowTransientStatus(message);
        }
    }

    private void RaiseTextReceived(string data)
    {
        if (TextReceived is { } handler)
        {
            handler(_escapeSequences.Replace(data, string.Empty));
        }
    }
}
