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
    /// The unit of time the X-axis of a <see cref="LineGraph"/> is divided into. The unit decides both where
    /// the tick marks fall and how their labels are formatted.
    /// </summary>
    public enum LineGraphTimeUnit
    {
        /// <summary>
        /// The graph picks the unit that best fits the span of time covered by the plotted points.
        /// </summary>
        Auto,

        /// <summary>
        /// Ticks fall on minute boundaries and are labelled with the time of day.
        /// </summary>
        Minutes,

        /// <summary>
        /// Ticks fall on hour boundaries and are labelled with the time of day, plus the date when the graph
        /// spans more than one day.
        /// </summary>
        Hours,

        /// <summary>
        /// Ticks fall at midnight and are labelled with the month and day.
        /// </summary>
        Days,

        /// <summary>
        /// Ticks fall on the first day of the week and are labelled with the month and day.
        /// </summary>
        Weeks,

        /// <summary>
        /// Ticks fall on the first of the month and are labelled with the month and year.
        /// </summary>
        Months,

        /// <summary>
        /// Ticks fall on the first of January and are labelled with the year.
        /// </summary>
        Years
    }
}
