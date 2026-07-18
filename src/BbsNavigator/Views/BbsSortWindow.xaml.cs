/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Builds an ordered collection of sort criteria for the BBS directory.
    /// </summary>
    public partial class BbsSortWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BbsSortWindow"/> class.
        /// </summary>
        public BbsSortWindow()
        {
            InitializeComponent();
            Criteria.Add(new BbsSortCriterion());
            DataContext = this;
            UpdateAddButton();
        }

        /// <summary>
        /// Gets the editable sort levels in priority order.
        /// </summary>
        public ObservableCollection<BbsSortCriterion> Criteria { get; } = new();

        /// <summary>
        /// Gets the profile fields available for sorting.
        /// </summary>
        public IReadOnlyList<BbsSortField> AvailableFields { get; } = Enum.GetValues<BbsSortField>();

        /// <summary>
        /// Gets the available sort directions.
        /// </summary>
        public IReadOnlyList<BbsSortDirection> AvailableDirections { get; } = Enum.GetValues<BbsSortDirection>();

        private void AddCriterion_OnClick(object sender, RoutedEventArgs e)
        {
            BbsSortField field = AvailableFields.First(candidate => Criteria.All(item => item.Field != candidate));
            Criteria.Add(new BbsSortCriterion { Field = field });
            UpdateAddButton();
        }

        private void RemoveCriterion_OnClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is BbsSortCriterion criterion)
            {
                Criteria.Remove(criterion);
                UpdateAddButton();
            }
        }

        private void Apply_OnClick(object sender, RoutedEventArgs e)
        {
            if (Criteria.Count == 0)
            {
                ShowWarning("Add at least one sort level.");
                return;
            }

            if (Criteria.GroupBy(item => item.Field).Any(group => group.Count() > 1))
            {
                ShowWarning("Each Sort By field can only be used once.");
                return;
            }

            DialogResult = true;
        }

        private void UpdateAddButton()
        {
            AddCriterionButton.IsEnabled = Criteria.Count < AvailableFields.Count;
        }

        private static void ShowWarning(string message)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(
                message,
                "Sort BBS Directory",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
