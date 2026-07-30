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
    /// Demonstrates the <see cref="DateSpinner"/> control. The view is its own view model, which keeps every
    /// binding in the example a real two way binding against an INotifyPropertyChanged source.
    /// </summary>
    [ObservableObject]
    public partial class DateSpinnerExample
    {
        [ObservableProperty]
        private DateTime? _arrivalDate = DateTime.Today.AddDays(7);

        /// <summary>
        /// Counts how many times the first spinner raised SelectedDateChanged, which shows that assigning the same
        /// effective day twice does not raise a duplicate notification.
        /// </summary>
        [ObservableProperty]
        private int _arrivalChangeCount;

        [ObservableProperty]
        private DateTime? _optionalDate;

        [ObservableProperty]
        private DateTime? _clearableDate = DateTime.Today;

        [ObservableProperty]
        private DateTime? _expirationDate = DateTime.Today.AddYears(3);

        [ObservableProperty]
        private DateTime? _modelYear = new DateTime(DateTime.Today.Year, 1, 1);

        [ObservableProperty]
        private DateTime? _anniversary = new DateTime(DateTime.Today.Year, 6, 14);

        [ObservableProperty]
        private DateTime? _appointmentDate = DateTime.Today.AddDays(3);

        [ObservableProperty]
        private DateTime? _dateOfBirth;

        [ObservableProperty]
        private DateTime? _liveDate = DateTime.Today;

        [ObservableProperty]
        private DateTime? _issuedDate = DateTime.Today.AddDays(-45);

        [ObservableProperty]
        private DateTime? _cultureDate = DateTime.Today;

        [ObservableProperty]
        private DateTime? _themeDate = DateTime.Today;

        /// <summary>
        /// The earliest appointment that can be booked. Supplying bounds from the view model rather than from a
        /// XAML string is the reliable way to set them, since not every date representation converts from a string.
        /// </summary>
        public DateTime EarliestAppointment { get; } = DateTime.Today;

        /// <summary>
        /// The latest appointment that can be booked, deliberately mid month so the boundary day filtering is
        /// visible on the day wheel.
        /// </summary>
        public DateTime LatestAppointment { get; } = DateTime.Today.AddMonths(3).AddDays(-11);

        /// <summary>
        /// The lower bound for the date of birth example.
        /// </summary>
        public DateTime EarliestBirthDate { get; } = new(1900, 1, 1);

        /// <summary>
        /// Today, exposed so a maximum date can be bound in XAML.
        /// </summary>
        public DateTime Today { get; } = DateTime.Today;

        public DateSpinnerExample()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Arrival_OnSelectedDateChanged(object? sender, DateSpinnerDateChangedEventArgs e)
        {
            this.ArrivalChangeCount++;
        }

        private void ButtonCycleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            AppServices.GetRequiredService<ThemeManager>().CycleTheme();
        }
    }
}
