/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Closes the current window if the escape button is clicked.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    ///     <i:Interaction.Behaviors>
    ///         <behaviors:CloseWindowOnEscapeBehavior />
    ///     </i:Interaction.Behaviors>
    /// ]]>
    /// </code>
    /// </example>
    public class CloseWindowOnEscapeBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }

        /// <summary>
        /// Processes the OnKeyDown event for the <see cref="TextBox"/>.  This will not set handled
        /// to true so that other subscribers of this will receive their event.  The DialogResult is
        /// set to false in this case.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                AssociatedObject.DialogResult = false;
                AssociatedObject.Close();
            }
        }
    }
}
