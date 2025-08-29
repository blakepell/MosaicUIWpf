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
    /// Specifies the placement of the active indicator for a tab.
    /// </summary>
    /// <remarks>
    /// The active indicator visually highlights the currently selected tab. Use this enumeration to
    /// configure whether the indicator appears at the top or bottom of the tab.
    /// </remarks>
    public enum TabActiveIndicatorPlacement
    {
        Top,
        Bottom
    }

    /// <summary>
    /// Represents a tab control that allows users to switch between multiple tabs.
    /// </summary>
    public class TabControl : System.Windows.Controls.TabControl
    {
        /// <summary>
        /// Identifies the <see cref="ActiveIndicatorPlacement"/> dependency property, which determines the placement of
        /// the active indicator within a <see cref="TabControl"/>.
        /// </summary>
        public static readonly DependencyProperty ActiveIndicatorPlacementProperty = DependencyProperty.Register(
            nameof(ActiveIndicatorPlacement), typeof(TabActiveIndicatorPlacement), typeof(TabControl), new FrameworkPropertyMetadata(TabActiveIndicatorPlacement.Top, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the placement of the active indicator within a tab control.
        /// </summary>
        public TabActiveIndicatorPlacement ActiveIndicatorPlacement
        {
            get => (TabActiveIndicatorPlacement)GetValue(ActiveIndicatorPlacementProperty);
            set => SetValue(ActiveIndicatorPlacementProperty, value);
        }

        static TabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TabControl), new FrameworkPropertyMetadata(typeof(TabControl)));
        }
    }

    /// <summary>
    /// Represents a single tab item within a <see cref="System.Windows.Controls.TabControl"/>.
    /// </summary>
    public class TabItem : System.Windows.Controls.TabItem
    {
        /// <summary>
        /// Identifies the ActiveIndicatorPlacement dependency property, which determines the placement of the active
        /// indicator within a <see cref="TabItem"/>.
        /// </summary>
        public static readonly DependencyProperty ActiveIndicatorPlacementProperty = TabControl.ActiveIndicatorPlacementProperty.AddOwner(
            typeof(TabItem), new FrameworkPropertyMetadata(TabActiveIndicatorPlacement.Top, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the placement of the active indicator within a tab control.
        /// </summary>
        public TabActiveIndicatorPlacement ActiveIndicatorPlacement
        {
            get => (TabActiveIndicatorPlacement)GetValue(ActiveIndicatorPlacementProperty);
            set => SetValue(ActiveIndicatorPlacementProperty, value);
        }

        static TabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TabItem), new FrameworkPropertyMetadata(typeof(TabItem)));
        }
    }
}