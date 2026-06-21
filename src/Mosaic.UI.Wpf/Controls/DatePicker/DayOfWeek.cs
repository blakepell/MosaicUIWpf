/*
 * Derives from: https://github.com/JamesnetGroup/smartdate
 * License: MIT
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a label that displays an abbreviated day-of-week header (such as "Sun", "Mon") in the calendar grid.
    /// </summary>
    public class DayOfWeek : Label
    {
        /// <summary>
        /// Initializes static members of the <see cref="DayOfWeek"/> class.
        /// </summary>
        static DayOfWeek()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DayOfWeek), new FrameworkPropertyMetadata(typeof(DayOfWeek)));
        }
    }
}
