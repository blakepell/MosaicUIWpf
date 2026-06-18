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

namespace MosaicWpfDemo.Views.Examples
{
    public partial class MessageBoxExample
    {
        public MessageBoxExample()
        {
            InitializeComponent();
        }

        private void OnInformationClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Your changes have been saved.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowResult(result);
        }

        private void OnWarningClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("You are running low on disk space.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            ShowResult(result);
        }

        private void OnErrorClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("The operation could not be completed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ShowResult(result);
        }

        private void OnQuestionClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to apply these changes?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
            ShowResult(result);
        }

        private void OnOkCancelClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will overwrite the existing file.", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            ShowResult(result);
        }

        private void OnYesNoCancelClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Save changes before closing?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            ShowResult(result);
        }

        private void OnLongMessageClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Your changes have been saved. They haven't really been saved but I need more text to show in this message box.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ShowResult(result);
        }

        private void ShowResult(MessageBoxResult result)
        {
            ResultTextBlock.Text = $"You clicked: {result}";
        }

    }
}
