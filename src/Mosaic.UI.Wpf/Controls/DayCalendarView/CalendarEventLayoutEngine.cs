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
    /// Assigns interval items to reusable horizontal columns within overlap clusters.
    /// </summary>
    internal static class CalendarEventLayoutEngine
    {
        internal static void AssignColumns(List<CalendarEventLayoutItem> items, List<DateTime> columnEnds)
        {
            items.Sort(static (left, right) =>
            {
                var comparison = left.Start.CompareTo(right.Start);
                return comparison != 0 ? comparison : right.End.CompareTo(left.End);
            });

            var clusterStart = 0;
            while (clusterStart < items.Count)
            {
                var clusterEnd = clusterStart + 1;
                var latestEnd = items[clusterStart].End;

                while (clusterEnd < items.Count && items[clusterEnd].Start < latestEnd)
                {
                    if (items[clusterEnd].End > latestEnd)
                    {
                        latestEnd = items[clusterEnd].End;
                    }

                    clusterEnd++;
                }

                AssignCluster(items, clusterStart, clusterEnd, columnEnds);
                clusterStart = clusterEnd;
            }
        }

        private static void AssignCluster(List<CalendarEventLayoutItem> items, int startIndex, int endIndex, List<DateTime> columnEnds)
        {
            columnEnds.Clear();

            for (var itemIndex = startIndex; itemIndex < endIndex; itemIndex++)
            {
                var item = items[itemIndex];
                var column = 0;

                while (column < columnEnds.Count && columnEnds[column] > item.Start)
                {
                    column++;
                }

                if (column == columnEnds.Count)
                {
                    columnEnds.Add(item.End);
                }
                else
                {
                    columnEnds[column] = item.End;
                }

                item.ColumnIndex = column;
            }

            var columnCount = Math.Max(1, columnEnds.Count);
            for (var itemIndex = startIndex; itemIndex < endIndex; itemIndex++)
            {
                items[itemIndex].ColumnCount = columnCount;
            }
        }
    }

    /// <summary>
    /// Stores an event presenter and its calculated overlap-column assignment.
    /// </summary>
    internal sealed class CalendarEventLayoutItem
    {
        internal required CalendarEventPresenter Presenter { get; init; }

        internal required DateTime Start { get; init; }

        internal required DateTime End { get; init; }

        internal int ColumnIndex { get; set; }

        internal int ColumnCount { get; set; } = 1;
    }
}
