/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Clears the contents of a <see cref="TextBoxBase"/> when the Escape key is pressed.
    /// </summary>
    /// <remarks>
    /// This will handle all TextBoxBase controls but the specific implementations need to be accounted
    /// for 
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// <TextBox Width="200" Height="30" Margin="10">
    ///     <i:Interaction.Behaviors>
    ///         <local:TextBoxClearTextOnEscapeBehavior />
    ///     </i:Interaction.Behaviors>
    /// </TextBox>
    /// ]]>
    /// </code>
    /// </example>
    public class TextBoxClearTextOnEscapeBehavior : Behavior<TextBoxBase>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyDown += OnKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.KeyDown -= OnKeyDown;
        }
        
        /// <summary>
        /// Processes the OnKeyDown event for the <see cref="TextBoxBase"/>.  This will not set handled
        /// to true so that other subscribers of this will receive their event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// This will check for <see cref="TextBox" /> and <see cref="RichTextBox" /> and handle those
        /// correctly.  If those aren't found, it will fallback and attempt to set the "Text" property
        /// via reflection if one is found on the control.
        /// </remarks>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Escape && AssociatedObject.IsFocused)
                {
                    if (AssociatedObject is TextBox tb)
                    {
                        tb.Clear();
                    }
                    else if (AssociatedObject is RichTextBox rtb)
                    {
                        rtb.Document.Blocks.Clear();
                    }
                    else
                    {
                        // Fallback: Check if the control has a "Text" property and can be written to.
                        var textProperty = AssociatedObject.GetType().GetProperty("Text");
                    
                        if (textProperty != null && textProperty.CanWrite)
                        {
                            textProperty.SetValue(AssociatedObject, string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}