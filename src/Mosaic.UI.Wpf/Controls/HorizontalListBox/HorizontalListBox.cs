/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Windows.Automation.Peers;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A <see cref="ListBox"/> variant that lays its items out horizontally as a row of cells which
    /// toggle on and off when clicked. Unlike <see cref="CheckBoxList"/> there is no checkbox glyph;
    /// selection is conveyed entirely by the cell's fill color.
    /// </summary>
    /// <remarks>
    /// The control defaults to <see cref="SelectionMode.Multiple"/> which gives each cell simple
    /// click-to-toggle behavior. Set <see cref="Selector.SelectionMode"/> to
    /// <see cref="SelectionMode.Single"/> for a segmented control style picker where only one cell
    /// may be active at a time.
    /// </remarks>
    [DefaultEvent(nameof(SelectionChanged))]
    [DefaultProperty(nameof(ItemsSource))]
    public class HorizontalListBox : ListBox
    {
        /// <summary>
        /// Initializes static metadata for the <see cref="HorizontalListBox"/> class.
        /// </summary>
        static HorizontalListBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HorizontalListBox), new FrameworkPropertyMetadata(typeof(HorizontalListBox)));
            SelectionModeProperty.OverrideMetadata(typeof(HorizontalListBox), new FrameworkPropertyMetadata(SelectionMode.Multiple));
        }

        /// <summary>
        /// The amount of space rendered between each cell.
        /// </summary>
        [Category("Layout")]
        [Description("The amount of space rendered between each cell.")]
        public double ItemSpacing
        {
            get => (double)this.GetValue(ItemSpacingProperty);
            set => this.SetValue(ItemSpacingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemSpacingProperty = DependencyProperty.Register(
            nameof(ItemSpacing), typeof(double), typeof(HorizontalListBox),
            new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The corner radius applied to each cell. Use a radius of 0 with an <see cref="ItemSpacing"/>
        /// of 0 for a flush, segmented appearance.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius applied to each cell.")]
        public CornerRadius ItemCornerRadius
        {
            get => (CornerRadius)this.GetValue(ItemCornerRadiusProperty);
            set => this.SetValue(ItemCornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemCornerRadiusProperty = DependencyProperty.Register(
            nameof(ItemCornerRadius), typeof(CornerRadius), typeof(HorizontalListBox),
            new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// The minimum width of each cell. Keeps short values such as day abbreviations evenly sized.
        /// </summary>
        [Category("Layout")]
        [Description("The minimum width of each cell.")]
        public double ItemMinWidth
        {
            get => (double)this.GetValue(ItemMinWidthProperty);
            set => this.SetValue(ItemMinWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemMinWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemMinWidthProperty = DependencyProperty.Register(
            nameof(ItemMinWidth), typeof(double), typeof(HorizontalListBox),
            new FrameworkPropertyMetadata(40.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The padding applied inside each cell.
        /// </summary>
        [Category("Layout")]
        [Description("The padding applied inside each cell.")]
        public Thickness ItemPadding
        {
            get => (Thickness)this.GetValue(ItemPaddingProperty);
            set => this.SetValue(ItemPaddingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemPaddingProperty = DependencyProperty.Register(
            nameof(ItemPadding), typeof(Thickness), typeof(HorizontalListBox),
            new FrameworkPropertyMetadata(new Thickness(10, 5, 10, 5), FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The background brush used by a cell that is toggled on. When null the theme accent brush is used.
        /// </summary>
        [Category("Brushes")]
        [Description("The background brush used by a cell that is toggled on.")]
        public Brush? SelectedBackground
        {
            get => (Brush?)this.GetValue(SelectedBackgroundProperty);
            set => this.SetValue(SelectedBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedBackgroundProperty = DependencyProperty.Register(
            nameof(SelectedBackground), typeof(Brush), typeof(HorizontalListBox),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The foreground brush used by a cell that is toggled on. When null the theme selection foreground is used.
        /// </summary>
        [Category("Brushes")]
        [Description("The foreground brush used by a cell that is toggled on.")]
        public Brush? SelectedForeground
        {
            get => (Brush?)this.GetValue(SelectedForegroundProperty);
            set => this.SetValue(SelectedForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedForegroundProperty = DependencyProperty.Register(
            nameof(SelectedForeground), typeof(Brush), typeof(HorizontalListBox),
            new FrameworkPropertyMetadata(null));

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new HorizontalListBoxItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is HorizontalListBoxItem || base.IsItemItsOwnContainerOverride(item);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new HorizontalListBoxAutomationPeer(this);
        }
    }
}
