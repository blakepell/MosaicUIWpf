/*
 * Derives from: https://github.com/JamesnetGroup/smartdate
 * License: MIT
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents the toggle button that opens and closes the calendar popup inside a <see cref="DatePicker"/>.
    /// </summary>
    public class CalendarSwitch : System.Windows.Controls.Primitives.ToggleButton
    {
        /// <summary>
        /// Initializes static members of the <see cref="CalendarSwitch"/> class.
        /// </summary>
        static CalendarSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CalendarSwitch), new FrameworkPropertyMetadata(typeof(CalendarSwitch)));
        }
    }
}
