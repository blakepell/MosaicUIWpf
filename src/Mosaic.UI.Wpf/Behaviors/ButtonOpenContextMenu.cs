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
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Behavior that opens the context menu of a button when clicked.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// <Button Content="Open menu">
    ///     <Button.ContextMenu>
    ///         <ContextMenu>
    ///             <MenuItem Header="First option" />
    ///             <MenuItem Header="Second option" />
    ///         </ContextMenu>
    ///     </Button.ContextMenu>
    ///     <i:Interaction.Behaviors>
    ///         <behaviors:OpenContextMenuBehavior />
    ///     </i:Interaction.Behaviors>
    /// </Button>
    /// ]]>
    /// </example>
    public class OpenContextMenuBehavior : Behavior<ButtonBase>
    {
        /// <summary>
        /// Called when the behavior is attached to the button.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += AssociatedObject_Click;
        }

        /// <summary>
        /// Called when the behavior is detached from the button.
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.Click -= AssociatedObject_Click;
            base.OnDetaching();
        }

        /// <summary>
        /// Called when the button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObject_Click(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.ContextMenu is ContextMenu menu)
            {
                menu.PlacementTarget = AssociatedObject;
                menu.IsOpen = true;
            }
        }
    }
}