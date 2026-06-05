/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
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

        /// <summary>
        /// Identifies the <see cref="EnableDragAndDropReordering"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableDragAndDropReorderingProperty = DependencyProperty.Register(
            nameof(EnableDragAndDropReordering), typeof(bool), typeof(TabControl), new FrameworkPropertyMetadata(false, OnEnableDragAndDropReorderingChanged));

        /// <summary>
        /// Gets or sets a value indicating whether tab items can be reordered by dragging and dropping them.
        /// </summary>
        public bool EnableDragAndDropReordering
        {
            get => (bool)GetValue(EnableDragAndDropReorderingProperty);
            set => SetValue(EnableDragAndDropReorderingProperty, value);
        }

        static TabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TabControl), new FrameworkPropertyMetadata(typeof(TabControl)));
        }

        /// <summary>
        /// Applies the drag-and-drop attached properties when the reordering setting changes.
        /// </summary>
        private static void OnEnableDragAndDropReorderingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TabControl tabControl)
            {
                ApplyDragAndDropReordering(tabControl, (bool)e.NewValue);
            }
        }

        /// <summary>
        /// Enables or disables Gong drag-drop reordering for the tab control.
        /// </summary>
        private static void ApplyDragAndDropReordering(TabControl tabControl, bool enable)
        {
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(tabControl, enable);
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(tabControl, enable);
        }
    }

    /// <summary>
    /// Represents a single tab item within a <see cref="System.Windows.Controls.TabControl"/>.
    /// </summary>
    public class TabItem : System.Windows.Controls.TabItem
    {
        /// <summary>
        /// Identifies the <see cref="HeaderFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderFontSizeProperty = DependencyProperty.Register(
            nameof(HeaderFontSize), typeof(double), typeof(TabItem), new FrameworkPropertyMetadata(14d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the font size used by the tab header.
        /// </summary>
        public double HeaderFontSize
        {
            get => (double)GetValue(HeaderFontSizeProperty);
            set => SetValue(HeaderFontSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderFontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderFontFamilyProperty = DependencyProperty.Register(
            nameof(HeaderFontFamily), typeof(FontFamily), typeof(TabItem), new FrameworkPropertyMetadata(new FontFamily("Segoe UI"), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the font family used by the tab header.
        /// </summary>
        public FontFamily HeaderFontFamily
        {
            get => (FontFamily)GetValue(HeaderFontFamilyProperty);
            set => SetValue(HeaderFontFamilyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderFontStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderFontStyleProperty = DependencyProperty.Register(
            nameof(HeaderFontStyle), typeof(FontStyle), typeof(TabItem), new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the font style used by the tab header.
        /// </summary>
        public FontStyle HeaderFontStyle
        {
            get => (FontStyle)GetValue(HeaderFontStyleProperty);
            set => SetValue(HeaderFontStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderFontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderFontWeightProperty = DependencyProperty.Register(
            nameof(HeaderFontWeight), typeof(FontWeight), typeof(TabItem), new FrameworkPropertyMetadata(FontWeights.Bold, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the font weight used by the tab header.
        /// </summary>
        public FontWeight HeaderFontWeight
        {
            get => (FontWeight)GetValue(HeaderFontWeightProperty);
            set => SetValue(HeaderFontWeightProperty, value);
        }

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