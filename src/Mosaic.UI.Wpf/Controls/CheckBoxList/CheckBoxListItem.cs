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
    /// The item container used by <see cref="CheckBoxList"/>.
    /// </summary>
    public class CheckBoxListItem : ListBoxItem
    {
        /// <summary>
        /// Initializes static metadata for the <see cref="CheckBoxListItem"/> class.
        /// </summary>
        static CheckBoxListItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CheckBoxListItem), new FrameworkPropertyMetadata(typeof(CheckBoxListItem)));
        }
    }
}
