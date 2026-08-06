/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using Mosaic.UI.Wpf.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class WindowCloseConfirmationBehaviorExample
    {
        public WindowCloseConfirmationBehaviorExample()
        {
            InitializeComponent();
        }

        private void OpenDialogButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Close Confirmation Example",
                Width = 420,
                Height = 240,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = CreateDialogContent()
            };

            Interaction.GetBehaviors(dialog).Add(new WindowCloseConfirmationBehavior
            {
                IsEnabled = ConfirmationEnabledCheckBox.IsChecked == true,
                Message = ConfirmationMessageTextBox.Text
            });

            dialog.ShowDialog();
            ResultTextBlock.Text = "The example dialog was closed.";
        }

        private static UIElement CreateDialogContent()
        {
            var closeButton = new Button
            {
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = "Close dialog"
            };

            closeButton.Click += (_, _) => Window.GetWindow(closeButton)?.Close();

            return new StackPanel
            {
                Margin = new Thickness(24),
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Margin = new Thickness(0, 0, 0, 20),
                        Text = "Try closing this dialog. The attached behavior will ask for confirmation when enabled.",
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    },
                    closeButton
                }
            };
        }
    }
}
