/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Extensions
{
    /// <summary>
    /// <see cref="ListView"/> extension methods.
    /// </summary>
    public static class ListViewExtensions
    {
        /// <summary>
        /// Scrolls to the last item in the <see cref="ListView"/>.
        /// </summary>
        /// <param name="listView"></param>
        public static void ScrollToLastItem(this ListView listView)
        {
            if (listView.Items.Count == 0)
            {
                return;
            }

            listView.ScrollIntoView(listView.Items[^1]);
        }

        /// <summary>
        /// Removes a column by the header name.
        /// </summary>
        /// <param name="lv"></param>
        /// <param name="headerName"></param>
        public static void RemoveHeader(this ListView lv, string headerName)
        {
            if (lv?.View is GridView gridView)
            {
                foreach (var column in gridView.Columns)
                {
                    if (column.Header as string == headerName && gridView.Columns.Remove(column))
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a header by its position.
        /// </summary>
        /// <param name="lv"></param>
        /// <param name="position"></param>
        public static void RemoveHeader(this ListView lv, int position)
        {
            if (lv?.View is GridView gridView && position >= gridView.Columns.Count - 1)
            {
                var columnToRemove = gridView.Columns[position];

                if (gridView.Columns.Remove(columnToRemove))
                {
                    return;
                }
            }
        }
    }
}
