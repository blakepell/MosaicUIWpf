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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The item container used by <see cref="HorizontalListBox"/>. Renders as a single cell whose
    /// fill color communicates whether it is toggled on or off.
    /// </summary>
    public class HorizontalListBoxItem : ListBoxItem
    {
        /// <summary>
        /// Initializes static metadata for the <see cref="HorizontalListBoxItem"/> class.
        /// </summary>
        static HorizontalListBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HorizontalListBoxItem), new FrameworkPropertyMetadata(typeof(HorizontalListBoxItem)));
        }

        /// <summary>
        /// The corner radius of the cell. The default style binds this to the owning
        /// <see cref="HorizontalListBox.ItemCornerRadius"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius of the cell.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)this.GetValue(CornerRadiusProperty);
            set => this.SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(HorizontalListBoxItem),
            new FrameworkPropertyMetadata(new CornerRadius(4)));
    }
}
