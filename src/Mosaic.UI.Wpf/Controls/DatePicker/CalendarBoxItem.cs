/*
 * Derives from: https://github.com/JamesnetGroup/smartdate
 * License: MIT
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a single day cell within a <see cref="CalendarBox"/>.
    /// </summary>
    public class CalendarBoxItem : ListBoxItem
    {
        /// <summary>
        /// Gets or sets the date formatted as <c>yyyyMMdd</c>, used as the selection key in <see cref="CalendarBox"/>.
        /// </summary>
        /// <value>
        /// A string in <c>yyyyMMdd</c> format, or <see langword="null"/> if not yet assigned.
        /// </value>
        public string? DateFormat { get; set; }

        /// <summary>
        /// The calendar date this item represents.
        /// </summary>
        public DateTime Date;

        /// <summary>
        /// Identifies the <see cref="IsCurrentMonth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCurrentMonthProperty =
            DependencyProperty.Register(nameof(IsCurrentMonth), typeof(bool), typeof(CalendarBoxItem), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether this day falls within the month currently shown in the calendar.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the day belongs to the displayed month; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool IsCurrentMonth
        {
            get => (bool)GetValue(IsCurrentMonthProperty);
            set => SetValue(IsCurrentMonthProperty, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="CalendarBoxItem"/> class.
        /// </summary>
        static CalendarBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CalendarBoxItem), new FrameworkPropertyMetadata(typeof(CalendarBoxItem)));
        }
    }
}
