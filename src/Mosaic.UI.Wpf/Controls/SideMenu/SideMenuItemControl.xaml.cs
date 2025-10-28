/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a control for displaying and interacting with a single item in a side menu.
    /// </summary>
    public partial class SideMenuItemControl
    {
        /// <summary>
        /// Identifies the <see cref="MenuItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MenuItemProperty =
            DependencyProperty.Register(nameof(MenuItem), typeof(SideMenuItem), typeof(SideMenuItemControl), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the side menu item associated with this control.
        /// </summary>
        public SideMenuItem? MenuItem
        {
            get => (SideMenuItem?)GetValue(MenuItemProperty);
            set => SetValue(MenuItemProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SideMenu"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SideMenuProperty =
            DependencyProperty.Register(nameof(SideMenu), typeof(SideMenu), typeof(SideMenuItemControl));

        /// <summary>
        /// Gets or sets the <see cref="SideMenu"/> associated with the control.
        /// </summary>
        public SideMenu? SideMenu
        {
            get => (SideMenu?)GetValue(SideMenuProperty);
            set => SetValue(SideMenuProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Compact"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CompactProperty =
            DependencyProperty.Register(nameof(Compact), typeof(bool), typeof(SideMenuItemControl), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the menu item is displayed in compact mode (icon above text).
        /// </summary>
        public bool Compact
        {
            get => (bool)GetValue(CompactProperty);
            set => SetValue(CompactProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SideMenuItemControl"/> class.
        /// </summary>
        public SideMenuItemControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the click event for a menu item in the side menu.
        /// </summary>
        /// <remarks>This method marks the event as handled and performs actions based on the state of the
        /// menu item. If the menu item has child items, it toggles its expansion state. Additionally, it selects the
        /// menu item in the associated side menu.</remarks>
        /// <param name="sender">The source of the event, typically the UI element that was clicked.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> containing event data, such as mouse button details.</param>
        private void OnItemClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (MenuItem != null && SideMenu != null)
            {
                // If the item has children, toggle expansion
                if (MenuItem.HasChildren)
                {
                    MenuItem.IsExpanded = !MenuItem.IsExpanded;
                }

                SideMenu.SelectItem(MenuItem);
            }
        }

        /// <summary>
        /// Handles the click event for an expander control.
        /// </summary>
        /// <remarks>This method marks the event as handled to prevent further propagation.</remarks>
        /// <param name="sender">The source of the event, typically the expander control that was clicked.</param>
        /// <param name="e">The event data associated with the click action.</param>
        private void OnExpanderClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    } 
}
