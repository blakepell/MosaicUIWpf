/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ValidationSummaryPanelExample
    {
        private readonly SampleFormViewModel _viewModel;

        public ValidationSummaryPanelExample()
        {
            InitializeComponent();
            _viewModel = new SampleFormViewModel();
            DataContext = _viewModel;
        }

        private void ValidateButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Trigger validation on all properties
            _viewModel.ValidateAll();
            ValidationSummary.Refresh();
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.ClearValidation();
            ValidationSummary.Refresh();
        }

        private void AddManualErrorButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ValidationSummary.AddError("This is a manually added error message.", "Custom Field");
        }
    }

    /// <summary>
    /// Sample view model with validation support using IDataErrorInfo.
    /// </summary>
    public class SampleFormViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private string _age = string.Empty;
        private bool _isValidating;

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Age
        {
            get => _age;
            set { _age = value; OnPropertyChanged(); }
        }

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                if (!_isValidating) return string.Empty;

                return columnName switch
                {
                    nameof(FirstName) when string.IsNullOrWhiteSpace(FirstName) => "First name is required.",
                    nameof(LastName) when string.IsNullOrWhiteSpace(LastName) => "Last name is required.",
                    nameof(Email) when string.IsNullOrWhiteSpace(Email) => "Email is required.",
                    nameof(Email) when !IsValidEmail(Email) => "Please enter a valid email address.",
                    nameof(Age) when string.IsNullOrWhiteSpace(Age) => "Age is required.",
                    nameof(Age) when !int.TryParse(Age, out int age) => "Age must be a number.",
                    nameof(Age) when int.TryParse(Age, out int age) && (age < 18 || age > 120) => "Age must be between 18 and 120.",
                    _ => string.Empty
                };
            }
        }

        public void ValidateAll()
        {
            _isValidating = true;
            OnPropertyChanged(nameof(FirstName));
            OnPropertyChanged(nameof(LastName));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(Age));
        }

        public void ClearValidation()
        {
            _isValidating = false;
            OnPropertyChanged(nameof(FirstName));
            OnPropertyChanged(nameof(LastName));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(Age));
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
