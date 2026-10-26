/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Themes;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Tests for <see cref="ListViewHelper"/> alternating rows and the theme brushes that back them.
    /// </summary>
    public class ListViewHelperTests
    {
        private static void RunSta(Action action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static ThemeManager CreateManager(MosaicThemeMode theme)
        {
            var manager = new ThemeManager();
            manager.BeginInit();
            manager.Theme = theme;
            manager.Native = true;
            manager.EndInit();
            return manager;
        }

        private static Border? GetRowBorder(ListView listView, int index)
        {
            var container = (ListViewItem)listView.ItemContainerGenerator.ContainerFromIndex(index);
            return container.Template.FindName("Bd", container) as Border;
        }

        [Theory]
        [InlineData(MosaicThemeMode.Light)]
        [InlineData(MosaicThemeMode.Dark)]
        [InlineData(MosaicThemeMode.Blue)]
        [InlineData(MosaicThemeMode.HighContrast)]
        [InlineData(MosaicThemeMode.Black)]
        public void Every_Theme_Defines_Distinct_Row_Brushes(MosaicThemeMode theme)
        {
            RunSta(() =>
            {
                var manager = CreateManager(theme);

                Assert.IsType<SolidColorBrush>(manager[MosaicTheme.ListViewItemBackgroundBrush]);
                Assert.IsType<SolidColorBrush>(manager[MosaicTheme.ListViewItemAlternateBackgroundBrush]);

                var row = Assert.IsType<Color>(manager[MosaicTheme.ListViewItemBackgroundColor]);
                var alternate = Assert.IsType<Color>(manager[MosaicTheme.ListViewItemAlternateBackgroundColor]);

                Assert.NotEqual(row, alternate);
            });
        }

        [Fact]
        public void Enabling_Sets_AlternationCount_And_Disabling_Restores_It()
        {
            RunSta(() =>
            {
                var listView = new ListView();

                ListViewHelper.SetIsAlternatingRowsEnabled(listView, true);
                Assert.Equal(2, listView.AlternationCount);

                ListViewHelper.SetIsAlternatingRowsEnabled(listView, false);
                Assert.Equal(0, listView.AlternationCount);
            });
        }

        [Fact]
        public void Consumer_AlternationCount_Is_Left_Alone()
        {
            RunSta(() =>
            {
                var listView = new ListView { AlternationCount = 3 };

                ListViewHelper.SetIsAlternatingRowsEnabled(listView, true);
                Assert.Equal(3, listView.AlternationCount);

                ListViewHelper.SetIsAlternatingRowsEnabled(listView, false);
                Assert.Equal(3, listView.AlternationCount);
            });
        }

        [Fact]
        public void Native_Style_Applies_Alternating_Backgrounds_Only_When_Enabled()
        {
            RunSta(() =>
            {
                var manager = CreateManager(MosaicThemeMode.Dark);
                var row = (Color)manager[MosaicTheme.ListViewItemBackgroundColor];
                var alternate = (Color)manager[MosaicTheme.ListViewItemAlternateBackgroundColor];

                var listView = new ListView { ItemsSource = new[] { "One", "Two", "Three" } };
                var host = new Border { Child = listView };
                host.Resources.MergedDictionaries.Add(manager);

                ListViewHelper.SetIsAlternatingRowsEnabled(listView, true);
                host.Measure(new Size(300, 300));
                host.Arrange(new Rect(0, 0, 300, 300));
                host.UpdateLayout();

                Assert.Equal(row, Assert.IsType<SolidColorBrush>(GetRowBorder(listView, 0)?.Background).Color);
                Assert.Equal(alternate, Assert.IsType<SolidColorBrush>(GetRowBorder(listView, 1)?.Background).Color);
                Assert.Equal(row, Assert.IsType<SolidColorBrush>(GetRowBorder(listView, 2)?.Background).Color);

                // Selection must still win over the stripe.
                listView.SelectedIndex = 1;
                host.UpdateLayout();
                Assert.NotEqual(alternate, Assert.IsType<SolidColorBrush>(GetRowBorder(listView, 1)?.Background).Color);
                listView.SelectedIndex = -1;

                ListViewHelper.SetIsAlternatingRowsEnabled(listView, false);
                host.UpdateLayout();

                Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(GetRowBorder(listView, 1)?.Background).Color);
            });
        }
    }
}
