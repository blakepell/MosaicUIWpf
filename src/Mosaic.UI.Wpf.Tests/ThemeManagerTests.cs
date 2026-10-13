/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Themes;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Tests for <see cref="ThemeManager"/> theme composition. These run on a manually created STA thread,
    /// matching the pattern used by the other WPF tests in this project.
    /// </summary>
    public class ThemeManagerTests
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

        [Theory]
        [InlineData(MosaicThemeMode.Light)]
        [InlineData(MosaicThemeMode.Dark)]
        [InlineData(MosaicThemeMode.Blue)]
        [InlineData(MosaicThemeMode.HighContrast)]
        [InlineData(MosaicThemeMode.Black)]
        public void Every_Theme_Loads_With_Native_Styles(MosaicThemeMode theme)
        {
            RunSta(() =>
            {
                var manager = CreateManager(theme);

                Assert.IsType<SolidColorBrush>(manager[MosaicTheme.WindowBackgroundBrush]);
                Assert.IsType<SolidColorBrush>(manager[MosaicTheme.ControlTextBackgroundBrush]);
                Assert.NotNull(manager[typeof(System.Windows.Controls.TextBox)]);
            });
        }

        [Fact]
        public void Black_Theme_Uses_Black_Window_And_Text_Surfaces()
        {
            RunSta(() =>
            {
                var manager = CreateManager(MosaicThemeMode.Black);

                Assert.Equal(Colors.Black, (Color)manager[MosaicTheme.WindowBackgroundColor]);
                Assert.Equal(Colors.Black, (Color)manager[MosaicTheme.ControlTextBackgroundColor]);
                Assert.Equal(Colors.Black, (Color)manager[SystemColors.WindowColorKey]);
            });
        }

        [Fact]
        public void Black_Theme_Defines_Every_Key_The_Dark_Theme_Defines()
        {
            RunSta(() =>
            {
                var dark = new ResourceDictionary { Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Dark/Dark.xaml") };
                var black = new ResourceDictionary { Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Black/Black.xaml") };
                var darkSystem = new ResourceDictionary { Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Dark/SystemColors.xaml") };
                var blackSystem = new ResourceDictionary { Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Themes/Black/SystemColors.xaml") };

                Assert.Empty(dark.Keys.Cast<object>().Where(key => !black.Contains(key)));
                Assert.Empty(darkSystem.Keys.Cast<object>().Where(key => !blackSystem.Contains(key)));
            });
        }

        [Fact]
        public void CycleTheme_Visits_Every_Theme_Including_Black()
        {
            RunSta(() =>
            {
                var manager = CreateManager(MosaicThemeMode.Light);
                var visited = new List<MosaicThemeMode>();

                for (int i = 0; i < 5; i++)
                {
                    manager.CycleTheme();
                    visited.Add(manager.Theme);
                }

                Assert.Equal(
                    new[] { MosaicThemeMode.Dark, MosaicThemeMode.Black, MosaicThemeMode.Blue, MosaicThemeMode.HighContrast, MosaicThemeMode.Light },
                    visited);
            });
        }
    }
}
