/*
 * Derives from: https://github.com/JamesnetGroup/smartdate
 * License: MIT
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a navigation button styled with a chevron arrow used to move between months in a <see cref="DatePicker"/>.
    /// </summary>
    public class ChevronButton : Button
    {
        /// <summary>
        /// Initializes static members of the <see cref="ChevronButton"/> class.
        /// </summary>
        static ChevronButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChevronButton), new FrameworkPropertyMetadata(typeof(ChevronButton)));
        }
    }
}
