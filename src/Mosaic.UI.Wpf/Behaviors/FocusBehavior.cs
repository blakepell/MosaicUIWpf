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
    /// Sets the focus to the control when it is loaded.  If this is used on multiple controls the
    /// last control it's used on will get the focus.
    /// <example>
    /// <code><![CDATA[
    /// <Window xmlns:b="clr-namespace:Sandbox.Behaviors"
    ///         xmlns:i="http://schemas.microsoft.com/xaml/behaviors">
    ///     <TextBox>
    ///         <i:Interaction.Behaviors>
    ///             <b:FocusBehavior />
    ///         </i:Interaction.Behaviors>
    ///     </TextBox>
    /// </Window>
    /// ]]></code>
    /// </example>
    /// </summary>
    public class FocusBehavior : Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Focus();
        }
    }
}
