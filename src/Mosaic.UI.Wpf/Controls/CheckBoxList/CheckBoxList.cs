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
    /// A <see cref="ListBox"/> variant that defaults to multiple selection and displays a checkbox beside each item.
    /// </summary>
    [DefaultEvent(nameof(SelectionChanged))]
    [DefaultProperty(nameof(ItemsSource))]
    public class CheckBoxList : ListBox
    {
        /// <summary>
        /// Initializes static metadata for the <see cref="CheckBoxList"/> class.
        /// </summary>
        static CheckBoxList()
        {
            SelectionModeProperty.OverrideMetadata(typeof(CheckBoxList), new FrameworkPropertyMetadata(SelectionMode.Multiple));
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new CheckBoxListItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is CheckBoxListItem || base.IsItemItsOwnContainerOverride(item);
        }
    }
}
