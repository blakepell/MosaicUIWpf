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
    public partial class CloseWindowOnEscapeExample
    {
        public CloseWindowOnEscapeExample()
        {
            InitializeComponent();
        }

        private void OpenWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = "Press Escape to close",
                Width = 360,
                Height = 220,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new Grid
                {
                    Margin = new Thickness(12),
                    Children =
                    {
                        new TextBlock
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 18,
                            FontWeight = FontWeights.SemiBold,
                            Text = "Press Escape to close"
                        }
                    }
                }
            };

            Interaction.GetBehaviors(window).Add(new CloseWindowOnEscapeBehavior());
            var result = window.ShowDialog();
        }
    }
}
