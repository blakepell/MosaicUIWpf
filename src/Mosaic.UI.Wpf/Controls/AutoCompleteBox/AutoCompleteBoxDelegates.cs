/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Filters an item for an <see cref="AutoCompleteBox"/> search operation.
    /// </summary>
    /// <param name="item">The candidate item.</param>
    /// <param name="searchText">The current search text.</param>
    /// <returns><c>true</c> when the item should be shown as a suggestion.</returns>
    public delegate bool AutoCompleteItemFilter(object item, string searchText);

    /// <summary>
    /// Looks up suggestions for an <see cref="AutoCompleteBox"/> search operation.
    /// </summary>
    /// <param name="searchText">The current search text.</param>
    /// <param name="cancellationToken">A token that is canceled when the query is superseded.</param>
    /// <returns>The matching suggestion items.</returns>
    public delegate Task<IEnumerable?> AutoCompleteItemsProvider(string searchText, CancellationToken cancellationToken);
}
