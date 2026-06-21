/*
 * Derives from: https://github.com/JamesnetGroup/smartdate
 * License: MIT
 */

using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a date picker control that displays a popup calendar for date selection.
    /// </summary>
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    [TemplatePart(Name = "PART_Switch", Type = typeof(CalendarSwitch))]
    [TemplatePart(Name = "PART_ListBox", Type = typeof(CalendarBox))]
    [TemplatePart(Name = "PART_Left", Type = typeof(ChevronButton))]
    [TemplatePart(Name = "PART_Right", Type = typeof(ChevronButton))]
    public class DatePicker : Control
    {
        private Popup? _popup;
        private CalendarSwitch? _switch;
        private CalendarBox? _listbox;
        private ChevronButton? _leftButton;
        private ChevronButton? _rightButton;

        /// <summary>
        /// Identifies the <see cref="KeepPopupOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty KeepPopupOpenProperty =
            DependencyProperty.Register(nameof(KeepPopupOpen), typeof(bool), typeof(DatePicker), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether the calendar popup remains open after a date is selected.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the popup stays open after selection; otherwise, <see langword="false"/>. The default is <see langword="true"/>.
        /// </value>
        public bool KeepPopupOpen
        {
            get => (bool)GetValue(KeepPopupOpenProperty); set => SetValue(KeepPopupOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CurrentMonth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentMonthProperty =
            DependencyProperty.Register(nameof(CurrentMonth), typeof(DateTime), typeof(DatePicker), new PropertyMetadata(default(DateTime)));

        /// <summary>
        /// Gets or sets the month currently displayed in the calendar popup.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> whose year and month determine the displayed calendar page.
        /// </value>
        public DateTime CurrentMonth
        {
            get => (DateTime)GetValue(CurrentMonthProperty); set => SetValue(CurrentMonthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedDate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(DatePicker), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the date selected by the user.
        /// </summary>
        /// <value>
        /// The selected <see cref="DateTime"/>, or <see langword="null"/> if no date has been chosen.
        /// </value>
        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="DatePicker"/> class.
        /// </summary>
        static DatePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DatePicker), new FrameworkPropertyMetadata(typeof(DatePicker)));
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            if (_popup != null)
            {
                _popup.Closed -= _popup_Closed;
            }

            if (_switch != null)
            {
                _switch.Click -= _switch_Click;
            }

            if (_listbox != null)
            {
                _listbox.MouseLeftButtonUp -= _listbox_MouseLeftButtonUp;
            }

            if (_leftButton != null)
            {
                _leftButton.Click -= LeftButton_Click;
            }

            if (_rightButton != null)
            {
                _rightButton.Click -= RightButton_Click;
            }

            base.OnApplyTemplate();

            _popup = GetTemplateChild("PART_Popup") as Popup;
            _switch = GetTemplateChild("PART_Switch") as CalendarSwitch;
            _listbox = GetTemplateChild("PART_ListBox") as CalendarBox;
            _leftButton = GetTemplateChild("PART_Left") as ChevronButton;
            _rightButton = GetTemplateChild("PART_Right") as ChevronButton;

            if (_popup != null)
            {
                _popup.Closed += _popup_Closed;
            }

            if (_switch != null)
            {
                _switch.Click += _switch_Click;
            }

            if (_listbox != null)
            {
                _listbox.MouseLeftButtonUp += _listbox_MouseLeftButtonUp;
            }

            if (_leftButton != null)
            {
                _leftButton.Click += LeftButton_Click;
            }

            if (_rightButton != null)
            {
                _rightButton.Click += RightButton_Click;
            }
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            MoveMonthClick(-1);
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            MoveMonthClick(1);
        }

        /// <summary>
        /// Shifts the displayed calendar by the specified number of months and regenerates the calendar view.
        /// </summary>
        /// <param name="month">The number of months to advance (positive) or go back (negative).</param>
        private void MoveMonthClick(int month)
        {
            GenerateCalendar(CurrentMonth.AddMonths(month));
        }

        private void _popup_Closed(object? sender, EventArgs e)
        {
            if (_switch != null)
            {
                _switch.IsChecked = false;
            }
        }

        private void _switch_Click(object sender, RoutedEventArgs e)
        {
            if (_switch?.IsChecked == true && _popup != null)
            {
                GenerateCalendar(SelectedDate ?? DateTime.Now);
                _popup.IsOpen = true;
            }
        }

        private void _listbox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_listbox?.SelectedItem is CalendarBoxItem selected)
            {
                SelectedDate = selected.Date;
                GenerateCalendar(selected.Date);

                if (_popup != null)
                {
                    _popup.IsOpen = KeepPopupOpen;
                }
            }
        }

        /// <summary>
        /// Populates the calendar list box with <see cref="CalendarBoxItem"/> entries for the month containing <paramref name="current"/>.
        /// </summary>
        /// <param name="current">The date whose year and month determine which calendar page to render.</param>
        private void GenerateCalendar(DateTime current)
        {
            if (_listbox == null)
            {
                return;
            }

            if (current.ToString("yyyyMM") == CurrentMonth.ToString("yyyyMM") && _listbox.Items.Count > 0)
            {
                UpdateSelectedValue();
                return;
            }

            CurrentMonth = current;
            _listbox.Items.Clear();
            DateTime fDayOfMonth = new(current.Year, current.Month, 1);
            DateTime lDayOfMonth = fDayOfMonth.AddMonths(1).AddDays(-1);

            int fOffset = (int)fDayOfMonth.DayOfWeek;
            int lOffset = 6 - (int)lDayOfMonth.DayOfWeek;

            DateTime fDay = fDayOfMonth.AddDays(-fOffset);
            DateTime lDay = lDayOfMonth.AddDays(lOffset);

            for (DateTime day = fDay; day <= lDay; day = day.AddDays(1))
            {
                CalendarBoxItem boxItem = new()
                {
                    Date = day,
                    DateFormat = day.ToString("yyyyMMdd"),
                    Content = day.Day,
                    IsCurrentMonth = day.Month == current.Month
                };

                _listbox.Items.Add(boxItem);
            }
            UpdateSelectedValue();
        }

        private void UpdateSelectedValue()
        {
            if (_listbox != null && SelectedDate != null)
            {
                _listbox.SelectedValue = SelectedDate.Value.ToString("yyyyMMdd");
            }
        }
    }
}
