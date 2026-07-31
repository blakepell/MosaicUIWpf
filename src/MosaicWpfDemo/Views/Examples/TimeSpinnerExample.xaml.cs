/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using Argus.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates the <see cref="TimeSpinner"/> control. The view is its own view model, which keeps every
    /// binding in the example a real two way binding against an INotifyPropertyChanged source.
    /// </summary>
    [ObservableObject]
    public partial class TimeSpinnerExample
    {
        [ObservableProperty]
        private TimeOnly? _appointmentTime = new(9, 30);

        /// <summary>
        /// Counts how many times the first spinner raised SelectedTimeChanged, which shows that assigning the same
        /// effective minute twice does not raise a duplicate notification.
        /// </summary>
        [ObservableProperty]
        private int _appointmentChangeCount;

        [ObservableProperty]
        private TimeOnly? _optionalTime;

        [ObservableProperty]
        private TimeOnly? _clearableTime = new(13, 0);

        [ObservableProperty]
        private TimeOnly? _meetingTime = new(14, 15);

        [ObservableProperty]
        private TimeOnly? _reminderTime = new(7, 0);

        [ObservableProperty]
        private TimeOnly? _liveTime = new(12, 0);

        [ObservableProperty]
        private TimeOnly? _openedAt = new(8, 45);

        [ObservableProperty]
        private TimeOnly? _themeTime = new(17, 30);

        /// <summary>
        /// The TimeSpan mirror of a separate spinner, showing the interop path for models that store a time of day
        /// as a <see cref="TimeSpan"/> rather than as a <see cref="TimeOnly"/>.
        /// </summary>
        [ObservableProperty]
        private TimeSpan? _shiftStart = new(6, 0, 0);

        /// <summary>
        /// The earliest time the business hours example accepts.
        /// </summary>
        public TimeOnly EarliestAppointment { get; } = new(8, 0);

        /// <summary>
        /// The latest time the business hours example accepts, deliberately off a fifteen minute step so the
        /// snapping behavior at the upper bound is visible.
        /// </summary>
        public TimeOnly LatestAppointment { get; } = new(17, 30);

        public TimeSpinnerExample()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Appointment_OnSelectedTimeChanged(object? sender, TimeSpinnerTimeChangedEventArgs e)
        {
            this.AppointmentChangeCount++;
        }

        private void ButtonCycleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            AppServices.GetRequiredService<ThemeManager>().CycleTheme();
        }
    }
}
