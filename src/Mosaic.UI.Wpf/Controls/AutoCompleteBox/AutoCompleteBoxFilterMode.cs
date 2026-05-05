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
    /// Defines the built-in filtering behavior for <see cref="AutoCompleteBox"/>.
    /// </summary>
    public enum AutoCompleteBoxFilterMode
    {
        /// <summary>
        /// Suggestions match when their display text contains the search text.
        /// </summary>
        Contains,

        /// <summary>
        /// Suggestions match when their display text starts with the search text.
        /// </summary>
        StartsWith,

        /// <summary>
        /// Suggestions are filtered with <see cref="AutoCompleteBox.FilterPredicate"/>.
        /// </summary>
        Custom
    }
}
