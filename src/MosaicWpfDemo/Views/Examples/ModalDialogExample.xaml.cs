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
using System.Windows;
using System.Windows.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ModalDialogExample
    {
        public ModalDialogExample()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The element the dialog blurs and overlays: the main window's content so the whole
        /// app dims behind the dialog.
        /// </summary>
        private UIElement DialogHost => Window.GetWindow(this)?.Content as UIElement ?? this;

        /// <summary>
        /// Creates a dialog pre-configured from the option checkboxes.
        /// </summary>
        private ModalDialog CreateDialog(string title, string description)
        {
            return new ModalDialog
            {
                Title = title,
                Description = description,
                CloseOnBackdropClick = BackdropCloseCheckBox.IsChecked == true,
                CloseOnEscape = EscapeCloseCheckBox.IsChecked == true,
                ShowCloseButton = CloseButtonCheckBox.IsChecked == true
            };
        }

        private void ShowResult(bool? result)
        {
            ResultTextBlock.Text = result switch
            {
                true => "Result: true (confirmed)",
                false => "Result: false (cancelled)",
                null => "Result: null (dismissed via close button, Escape or backdrop)"
            };
        }

        private async void OnSimpleClick(object sender, RoutedEventArgs e)
        {
            var dialog = this.CreateDialog("Simple Dialog", "Content can be any element; buttons inside can close the dialog.");

            var okButton = new AccentButton { Content = "OK", MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
            okButton.Click += (_, _) => dialog.Close(true);

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "Everything behind this dialog is blurred and dimmed, while the dialog itself stays sharp in the adorner layer.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            });
            panel.Children.Add(okButton);
            dialog.Content = panel;

            this.ShowResult(await dialog.ShowAsync(this.DialogHost));
        }

        private async void OnConfirmationClick(object sender, RoutedEventArgs e)
        {
            var dialog = this.CreateDialog("Delete 3 Items?", "This action cannot be undone.");

            // Buttons anywhere inside the content can find their dialog via ModalDialog.FindHost.
            var cancel = new Button { Content = "Cancel", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0) };
            cancel.Click += (s, _) => ModalDialog.FindHost((DependencyObject)s!)?.Close(false);

            var confirm = new AccentButton { Content = "Delete", MinWidth = 90, AccentButtonType = AccentButtonType.FluentRed };
            confirm.Click += (s, _) => ModalDialog.FindHost((DependencyObject)s!)?.Close(true);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(cancel);
            buttons.Children.Add(confirm);
            dialog.Content = buttons;

            this.ShowResult(await dialog.ShowAsync(this.DialogHost));
        }

        private async void OnFormClick(object sender, RoutedEventArgs e)
        {
            var dialog = this.CreateDialog("Rename Item", "Enter the new name and press Save.");

            var nameBox = new TextBox { Text = "New Document", Margin = new Thickness(0, 0, 0, 16) };

            var save = new AccentButton { Content = "Save", MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
            save.Click += (_, _) => dialog.Close(true);

            var panel = new StackPanel();
            panel.Children.Add(nameBox);
            panel.Children.Add(save);
            dialog.Content = panel;

            bool? result = await dialog.ShowAsync(this.DialogHost);
            ResultTextBlock.Text = result == true
                ? $"Result: true — renamed to \"{nameBox.Text}\""
                : "Result: rename cancelled";
        }

        private async void OnNoBlurClick(object sender, RoutedEventArgs e)
        {
            var dialog = this.CreateDialog("No Blur", "BlurRadius is 0 here, so only the dimmed backdrop separates the dialog.");
            dialog.BlurRadius = 0;

            var okButton = new AccentButton { Content = "Close", MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
            okButton.Click += (_, _) => dialog.Close(true);
            dialog.Content = okButton;

            this.ShowResult(await dialog.ShowAsync(this.DialogHost));
        }
    }
}
