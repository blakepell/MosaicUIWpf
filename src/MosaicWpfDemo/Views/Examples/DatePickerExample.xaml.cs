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

namespace MosaicWpfDemo.Views.Examples
{
    [ObservableObject]
    public partial class DatePickerExample
    {
        [ObservableProperty]
        private DateTime? _selectedDate = DateTime.Today.AddDays(2);

        [ObservableProperty]
        private DateTime? _todayDate = DateTime.Today;

        [ObservableProperty]
        private DateTime? _dueDate = DateTime.Today.AddDays(14);

        public DatePickerExample()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
