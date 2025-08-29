/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a single parameter that can be set from XAML
    /// </summary>
    [ContentProperty(nameof(Value))]
    public class SideMenuParameter
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    /// <summary>
    /// A collection of parameters that can be set from XAML
    /// </summary>
    [ContentProperty(nameof(Parameters))]
    public class SideMenuParameterCollection : Collection<SideMenuParameter>
    {
        public ObservableCollection<SideMenuParameter> Parameters { get; } = new();

        protected override void InsertItem(int index, SideMenuParameter item)
        {
            Parameters.Insert(index, item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            Parameters.RemoveAt(index);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, SideMenuParameter item)
        {
            Parameters[index] = item;
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            Parameters.Clear();
            base.ClearItems();
        }

        /// <summary>
        /// Converts this collection to a Dictionary
        /// </summary>
        /// <returns>Dictionary representation of the parameters</returns>
        public Dictionary<string, object?> ToDictionary()
        {
            return Parameters.ToDictionary(p => p.Key, p => p.Value);
        }
    }
}
