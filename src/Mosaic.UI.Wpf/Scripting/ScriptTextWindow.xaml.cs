/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Scripting
{
    /// <summary>
    /// A themed, resizable text dialog used by the <c>ui</c> script module.  In read-only mode it
    /// displays text in a scrollable editor with a single Close button; in input mode it shows a
    /// prompt above an editable text box with OK and Cancel buttons.  The chrome mirrors the Mosaic
    /// <see cref="Controls.MessageBox"/>.
    /// </summary>
    internal partial class ScriptTextWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptTextWindow"/> class.
        /// </summary>
        /// <param name="text">The text to display (read-only) or the prompt to show (input).</param>
        /// <param name="readOnly">Whether the dialog displays text rather than collecting input.</param>
        public ScriptTextWindow(string text, bool readOnly)
        {
            InitializeComponent();

            this.Title = readOnly ? "Script Output" : "Input";
            TitleTextBlock.Text = this.Title;

            if (readOnly)
            {
                PromptTextBlock.Visibility = Visibility.Collapsed;
                Editor.Text = text ?? string.Empty;
                Editor.IsReadOnly = true;
                Editor.AcceptsReturn = true;
                Editor.FontFamily = (FontFamily)FindResource(Themes.MosaicTheme.MonospaceFontFamily);
                OkButton.Content = "Close";
                CancelButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                PromptTextBlock.Text = text ?? string.Empty;
                this.Height = 220;
            }

            this.Loaded += (_, _) => Editor.Focus();
            this.PreviewKeyDown += OnPreviewKeyDown;
        }

        /// <summary>
        /// Gets the text in the editor.
        /// </summary>
        public string Text => Editor.Text;

        /// <summary>
        /// Accepts the dialog.
        /// </summary>
        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        /// <summary>
        /// Cancels the dialog.
        /// </summary>
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        /// <summary>
        /// Allows the window to be dragged by its title bar.
        /// </summary>
        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// Dismisses the dialog when the user presses <see cref="Key.Escape"/>.
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.DialogResult = false;
            }
        }
    }
}
