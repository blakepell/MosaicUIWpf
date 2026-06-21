/*
 * Derives from: https://github.com/JamesnetGroup/smartdate
 * License: MIT
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents the scrollable grid of day cells displayed inside the <see cref="DatePicker"/> popup.
    /// </summary>
    public class CalendarBox : ListBox
    {
        /// <summary>
        /// Initializes static members of the <see cref="CalendarBox"/> class.
        /// </summary>
        static CalendarBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CalendarBox), new FrameworkPropertyMetadata(typeof(CalendarBox)));
        }
    }
}
