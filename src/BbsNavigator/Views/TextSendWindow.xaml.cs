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
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace BbsNavigator.Views;

/// <summary>
/// Previews terminal input and validates encoding and pacing before transmission.
/// </summary>
public partial class TextSendWindow : Window
{
    private readonly Encoding _encoding;

    /// <summary>
    /// Initializes a new instance of the TextSendWindow class.
    /// </summary>
    public TextSendWindow(string text, string board, Encoding encoding, int characterDelay, int lineDelay)
    {
        _encoding = encoding;
        InitializeComponent();
        DestinationText.Text = $"Send to {board}";
        CharacterDelayBox.Text = characterDelay.ToString();
        LineDelayBox.Text = lineDelay.ToString();
        PreviewBox.Text = text;
        ValidateText();
    }

    /// <summary>
    /// Gets the edited text with terminal line endings.
    /// </summary>
    public string TextToSend => TerminalText.Normalize(PreviewBox.Text);
    /// <summary>
    /// Gets the validated character delay.
    /// </summary>
    public int CharacterDelay { get; private set; }
    /// <summary>
    /// Gets the validated line delay.
    /// </summary>
    public int LineDelay { get; private set; }
    /// <summary>
    /// Gets whether the selected delays should be saved on this profile.
    /// </summary>
    public bool SavePacing => RememberPacing.IsChecked == true;

    private void Preview_OnTextChanged(object sender, TextChangedEventArgs e) => ValidateText();
    private void ValidateText()
    {
        if (ValidationText == null || PreviewBox == null || SendButton == null) return;
        string problem = TerminalText.EncodingProblem(TextToSend, _encoding);
        ValidationText.Text = problem.Length > 0 ? problem : $"{TextToSend.Length:N0} characters · {TextToSend.Count(c => c == '\r') + 1:N0} lines";
        SendButton.IsEnabled = problem.Length == 0 && TextToSend.Length > 0;
    }
    private void Send_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CharacterDelayBox.Text, out int characterDelay) || characterDelay is < 0 or > 1000 ||
            !int.TryParse(LineDelayBox.Text, out int lineDelay) || lineDelay is < 0 or > 10000)
        {
            ValidationText.Text = "Use 0–1000 ms per character and 0–10000 ms per line.";
            return;
        }
        CharacterDelay = characterDelay;
        LineDelay = lineDelay;
        DialogResult = true;
    }
}
