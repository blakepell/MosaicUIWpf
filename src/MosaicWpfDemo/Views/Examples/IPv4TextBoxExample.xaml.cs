/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates binding, validation, focus, appearance, and state scenarios for <see cref="IPv4TextBox"/>.
    /// </summary>
    public partial class IPv4TextBoxExample : INotifyPropertyChanged
    {
        /// <summary>
        /// Stores the address used by the two-way binding example.
        /// </summary>
        private string _ipAddress = "192.168.1.25";

        /// <summary>
        /// Stores the result message for the most recent interactive assignment.
        /// </summary>
        private string _assignmentResult = "Edit a segment to update the view model.";

        /// <summary>
        /// Initializes a new instance of the <see cref="IPv4TextBoxExample"/> class.
        /// </summary>
        public IPv4TextBoxExample()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the address used by the two-way binding example.
        /// </summary>
        /// <value>The complete address synchronized with the bound control.</value>
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if (SetField(ref _ipAddress, value))
                {
                    AssignmentResult = "Accepted complete address from the editor.";
                }
            }
        }

        /// <summary>
        /// Gets or sets the result of the most recent interactive assignment.
        /// </summary>
        /// <value>The status message displayed beside the bound control.</value>
        public string AssignmentResult
        {
            get => _assignmentResult;
            set => SetField(ref _assignmentResult, value);
        }

        /// <summary>
        /// Assigns a valid address through the example view-model property.
        /// </summary>
        /// <param name="sender">The button that initiated the assignment.</param>
        /// <param name="e">The routed-event data.</param>
        private void AssignValidAddress_Click(object sender, RoutedEventArgs e)
        {
            IpAddress = "10.20.30.40";
            AssignmentResult = "Accepted 10.20.30.40 through the two-way binding.";
        }

        /// <summary>
        /// Attempts an invalid external assignment and reports the retained control value.
        /// </summary>
        /// <param name="sender">The button that initiated the assignment.</param>
        /// <param name="e">The routed-event data.</param>
        private void AssignInvalidAddress_Click(object sender, RoutedEventArgs e)
        {
            const string attemptedAddress = "256.1.1.1";
            BoundAddress.SetCurrentValue(IPv4TextBox.TextProperty, attemptedAddress);
            AssignmentResult = $"Rejected {attemptedAddress}; retained {BoundAddress.Text}.";
        }

        /// <summary>
        /// Updates a backing field and raises <see cref="PropertyChanged"/> when its value changes.
        /// </summary>
        /// <typeparam name="T">The type of value stored by the field.</typeparam>
        /// <param name="field">The backing field to update.</param>
        /// <param name="value">The proposed field value.</param>
        /// <param name="propertyName">The property name associated with the field.</param>
        /// <returns><see langword="true"/> if the field changed; otherwise, <see langword="false"/>.</returns>
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
