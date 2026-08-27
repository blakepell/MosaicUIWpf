/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates the <see cref="DayCalendarView"/> control with custom source objects and MVVM commands.
    /// </summary>
    public partial class DayCalendarViewExample
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DayCalendarViewExample"/> class.
        /// </summary>
        public DayCalendarViewExample()
        {
            InitializeComponent();
            DataContext = new DayCalendarViewExampleViewModel();
        }
    }

    /// <summary>
    /// Provides the observable state and commands used by the day-calendar example.
    /// </summary>
    public partial class DayCalendarViewExampleViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DayCalendarViewExampleViewModel"/> class.
        /// </summary>
        public DayCalendarViewExampleViewModel()
        {
            SelectedDate = DateTime.Today;
            var day = SelectedDate.Date;

            Events =
            [
                Create(day, 8, 5, 8, 52, "Daily planning", "Single event — full width"),

                // Two overlapping events with intentionally irregular minute values.
                Create(day, 9, 7, 10, 43, "Development meeting", "Starts 9:07 AM; ends 10:43 AM"),
                Create(day, 9, 25, 10, 20, "Candidate interview", "Conference Room 2"),

                // Three events active at the same time, producing three columns.
                Create(day, 11, 5, 12, 20, "Design review", "Studio A"),
                Create(day, 11, 15, 12, 10, "Customer call", "Teams"),
                Create(day, 11, 30, 12, 0, "Release check", "Operations"),

                // A chained partial-overlap cluster that reuses freed columns.
                Create(day, 13, 0, 14, 0, "Architecture session", "Event A"),
                Create(day, 13, 45, 14, 30, "Partner sync", "Event B"),
                Create(day, 14, 15, 15, 0, "Documentation", "Event C"),

                // This event demonstrates clipping at the beginning of the selected day.
                new DayCalendarDemoEvent
                {
                    Begin = day.AddMinutes(-30),
                    Finish = day.AddHours(1),
                    Subject = "Overnight maintenance",
                    Location = "Read-only; clipped to 12:00 AM – 1:00 AM",
                    IsReadOnly = true,
                    Color = Brushes.MediumSeaGreen
                }
            ];
        }

        /// <summary>
        /// Gets the date displayed by the example.
        /// </summary>
        public DateTime SelectedDate { get; }

        /// <summary>
        /// Gets the custom event objects displayed by the calendar.
        /// </summary>
        public ObservableCollection<DayCalendarDemoEvent> Events { get; }

        /// <summary>
        /// Gets or sets the selected custom event object.
        /// </summary>
        [ObservableProperty]
        public partial DayCalendarDemoEvent? SelectedEvent { get; set; }

        /// <summary>
        /// Gets or sets the interaction status displayed beneath the calendar.
        /// </summary>
        [ObservableProperty]
        public partial string Status { get; set; } = "Ready. The source objects use custom property names configured through the calendar path properties.";

        [RelayCommand]
        private void OpenEvent(DayCalendarDemoEvent? calendarEvent)
        {
            if (calendarEvent != null)
            {
                Status = $"Activated “{calendarEvent.Subject}” ({calendarEvent.Begin:h:mm tt} – {calendarEvent.Finish:h:mm tt}).";
            }
        }

        [RelayCommand]
        private void ChangeEventTime(CalendarEventTimeChangedEventArgs? proposal)
        {
            if (proposal?.Event is not DayCalendarDemoEvent calendarEvent)
            {
                return;
            }

            calendarEvent.Begin = proposal.NewStart;
            calendarEvent.Finish = proposal.NewEnd;
            Status = $"Moved “{calendarEvent.Subject}” to {proposal.NewStart:h:mm tt} – {proposal.NewEnd:h:mm tt}.";
        }

        private static DayCalendarDemoEvent Create(
            DateTime day,
            int startHour,
            int startMinute,
            int endHour,
            int endMinute,
            string subject,
            string location)
        {
            return new DayCalendarDemoEvent
            {
                Begin = day.AddHours(startHour).AddMinutes(startMinute),
                Finish = day.AddHours(endHour).AddMinutes(endMinute),
                Subject = subject,
                Location = location
            };
        }
    }

    /// <summary>
    /// Represents an application-specific event whose property names are mapped by <see cref="DayCalendarView"/>.
    /// </summary>
    public partial class DayCalendarDemoEvent : ObservableObject
    {
        /// <summary>
        /// Gets or sets the event start.
        /// </summary>
        [ObservableProperty]
        public partial DateTime Begin { get; set; }

        /// <summary>
        /// Gets or sets the event end.
        /// </summary>
        [ObservableProperty]
        public partial DateTime Finish { get; set; }

        /// <summary>
        /// Gets or sets the event title.
        /// </summary>
        [ObservableProperty]
        public partial string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event's optional secondary text.
        /// </summary>
        [ObservableProperty]
        public partial string? Location { get; set; }

        /// <summary>
        /// Gets or sets the optional event background override.
        /// </summary>
        [ObservableProperty]
        public partial Brush? Color { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the event cannot be moved by dragging.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the event cannot be moved; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        [ObservableProperty]
        public partial bool IsReadOnly { get; set; }
    }
}
