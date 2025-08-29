/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that enables a <see cref="Button"/> to copy text from a specified <see cref="TextBox"/> to the
    /// clipboard.
    /// </summary>
    /// <remarks>
    /// <code><![CDATA[
    /// <TextBox x:Name="MyTextBox" Width="200" Height="25" />
    /// <Button Content="Copy">
    ///     <i:Interaction.Behaviors>
    ///         <behaviors1:AvalonEditCopyBehavior TargetTextEditor="{Binding ElementName=XamlEditor}" />
    ///     </i:Interaction.Behaviors>
    /// </Button>
    /// ]]></code>
    /// </remarks>
    public class AvalonEditCopyBehavior : Behavior<Button>
    {
        /// <summary>
        /// Identifies the <see cref="TargetTextEditor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetTextEditorProperty = DependencyProperty.Register(nameof(TargetTextEditor), typeof(TextEditor), typeof(AvalonEditCopyBehavior), new PropertyMetadata(default(TextEditor)));

        /// <summary>
        /// Gets or sets the target <see cref="TextEditor"/> instance associated with this property.
        /// </summary>
        public TextEditor? TargetTextEditor
        {
            get => (TextEditor)GetValue(TargetTextEditorProperty);
            set => SetValue(TargetTextEditorProperty, value);
        }

        /// <summary>
        /// When the behavior is attached to the associated object (a <see cref="Button"/>),
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject != null)
            {
                AssociatedObject.Click += OnButtonClick;
            }
        }

        /// <summary>
        /// When the behavior is detached to the associated object (a <see cref="Button"/>),
        /// </summary>
        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Click -= OnButtonClick;
            }

            base.OnDetaching();
        }

        /// <summary>
        /// Handles the button click event to copy text from the target text box to the clipboard.
        /// </summary>
        /// <remarks>If the <see cref="TargetTextEditor"/> is null, the method exits without performing any
        /// action. The method prioritizes copying the selected text from the target text box. If no text is selected, 
        /// it copies the entire text content of the text box. If the text to copy is empty or null, no action is taken.
        /// Any exceptions that occur while setting the clipboard text are logged using <see
        /// cref="System.Diagnostics.Trace"/>.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the button click.</param>
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (TargetTextEditor == null)
            {
                return;
            }

            string? textToCopy = !string.IsNullOrEmpty(TargetTextEditor.SelectedText) ? TargetTextEditor.SelectedText : TargetTextEditor.Text;

            if (!string.IsNullOrEmpty(textToCopy))
            {
                try
                {
                    Clipboard.SetText(textToCopy, TextDataFormat.Text);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error copying text: {ex.Message}");
                }
            }
        }
    }
}
