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
    /// Provides data for the <see cref="TableSizePicker.TableSizeSelected"/> event which is raised when the
    /// user commits a table size by clicking (or pressing <c>Enter</c>/<c>Space</c> on) a cell.
    /// </summary>
    public sealed class TableSizeSelectedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableSizeSelectedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event that is being raised.</param>
        /// <param name="source">The object raising the event.</param>
        /// <param name="rowCount">The number of committed rows.</param>
        /// <param name="columnCount">The number of committed columns.</param>
        public TableSizeSelectedEventArgs(RoutedEvent routedEvent, object source, int rowCount, int columnCount)
            : base(routedEvent, source)
        {
            this.RowCount = rowCount;
            this.ColumnCount = columnCount;
        }

        /// <summary>
        /// The number of rows in the committed table size.
        /// </summary>
        public int RowCount { get; }

        /// <summary>
        /// The number of columns in the committed table size.
        /// </summary>
        public int ColumnCount { get; }

        /// <summary>
        /// Invokes strongly typed <see cref="EventHandler{TEventArgs}"/> handlers directly which avoids the
        /// reflection based fallback in <see cref="RoutedEventArgs"/>.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The element the handler is attached to.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            if (genericHandler is EventHandler<TableSizeSelectedEventArgs> handler)
            {
                handler(genericTarget, this);
                return;
            }

            base.InvokeEventHandler(genericHandler, genericTarget);
        }
    }
}
