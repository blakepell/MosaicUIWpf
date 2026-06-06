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
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;

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
    ///         <behaviors:OpenContextMenuBehavior Placement="Right" />
    ///     </i:Interaction.Behaviors>
    /// </Button>
    /// ]]>
    /// </example>
    public class OpenContextMenuBehavior : Behavior<ButtonBase>
    {
        /// <summary>
        /// The placement of the context menu relative to the button.
        /// </summary>
        [Category("Common")]
        [Description("The placement of the context menu relative to the button.")]
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(OpenContextMenuBehavior), new PropertyMetadata(PlacementMode.Bottom));

        /// <summary>
        /// The placement of the context menu relative to the button.
        /// </summary>
        public PlacementMode Placement
        {
            get => (PlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

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
                menu.Placement = Placement;
                menu.IsOpen = true;
            }
        }
    }
}