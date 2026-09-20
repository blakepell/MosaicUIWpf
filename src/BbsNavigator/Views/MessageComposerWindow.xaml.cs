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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BbsNavigator.Views;

/// <summary>
/// Composes and saves a board-specific draft independently of the connection lifetime.
/// </summary>
public partial class MessageComposerWindow : Window
{
    private readonly MessageDraftStore _store;
    private readonly Func<string, Task<bool>> _send;
    private readonly Action _stop;
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private bool _loaded;
    private bool _dirty;

    /// <summary>
    /// Initializes a new instance of the MessageComposerWindow class.
    /// </summary>
    public MessageComposerWindow(BbsProfile profile, string dataFolder, Func<string, Task<bool>> send, Action stop)
    {
        _store = new(dataFolder, profile.Id);
        _send = send;
        _stop = stop;
        InitializeComponent();
        BoardText.Text = $"Message for {profile.Name}";
        Title = $"Compose — {profile.Name}";
        try { DraftBox.Text = _store.Load(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DraftStatus.Text = $"Could not load the draft: {ex.Message}";
            DraftBox.IsReadOnly = true;
            SendButton.IsEnabled = false;
            return;
        }
        _loaded = true;
        _saveTimer.Tick += (_, _) => SaveDraft();
        Closing += (_, e) =>
        {
            _saveTimer.Stop();
            if (!SaveDraft())
            {
                e.Cancel = Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "The latest draft could not be saved. Close and discard the unsaved changes?",
                    "Unsaved draft", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes;
            }
            if (!e.Cancel) _stop();
        };
    }

    private void Draft_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loaded) return;
        _dirty = true;
        DraftStatus.Text = "Saving draft…";
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private bool SaveDraft()
    {
        _saveTimer.Stop();
        if (!_dirty) return true;
        try
        {
            _store.Save(DraftBox.Text);
            _dirty = false;
            DraftStatus.Text = "Draft saved locally";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DraftStatus.Text = $"Draft not saved: {ex.Message}. Copy your text before closing.";
            return false;
        }
    }

    private void Quote_OnClick(object sender, RoutedEventArgs e)
    {
        string selected = DraftBox.SelectedText;
        if (selected.Length == 0) return;
        DraftBox.SelectedText = string.Join(Environment.NewLine, TerminalText.Normalize(selected).Split('\r').Select(line => "> " + line));
    }

    private async void Send_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WrapBox.Text, out int columns) || columns is < 0 or > 300 || columns is > 0 and < 20)
        {
            DraftStatus.Text = "Choose 20–300 columns, or 0 to leave lines unchanged.";
            return;
        }
        if (!SaveDraft()) return;
        SendButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        try
        {
            bool sent = await _send(TerminalText.Wrap(DraftBox.Text, columns));
            DraftStatus.Text = sent ? "Text sent; your draft is still saved." : "Text not sent completely; your draft is still saved.";
        }
        catch (Exception ex) { DraftStatus.Text = $"Could not send: {ex.Message}"; }
        finally { SendButton.IsEnabled = true; StopButton.IsEnabled = false; }
    }

    private void Stop_OnClick(object sender, RoutedEventArgs e) => _stop();
}
