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
using System.Windows.Controls;
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
        [InlineData(MosaicThemeMode.Blackest)]
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
        public void Rounded_Buttons_Update_Existing_Controls_And_Respect_The_Active_Theme()
        {
            RunSta(() =>
            {
                var manager = CreateManager(MosaicThemeMode.Light);
                var button = new Button { Content = "OK" };
                var accent = new Controls.AccentButton { Content = "Apply" };
                var split = new Controls.SplitButton { Content = "More" };
                var panel = new StackPanel { Children = { button, accent, split } };
                panel.Resources.MergedDictionaries.Add(manager);

                Assert.Equal(new CornerRadius(0), GetBorder(button, "border").CornerRadius);
                Assert.IsType<SolidColorBrush>(button.Background);

                manager.UseRoundedButtons = true;

                var border = GetBorder(button, "border");
                Assert.Equal(new CornerRadius(3), border.CornerRadius);
                Assert.Equal(new Thickness(1), border.BorderThickness);
                var lightBackground = Assert.IsType<LinearGradientBrush>(border.Background).GradientStops[0].Color;
                Assert.Equal(new CornerRadius(3), GetBorder(accent, "PART_Root").CornerRadius);
                Assert.Equal(new CornerRadius(3), GetBorder(split, "PART_Root").CornerRadius);
                Assert.IsType<LinearGradientBrush>(split.Background);

                foreach (var theme in new[] { MosaicThemeMode.Dark, MosaicThemeMode.Black, MosaicThemeMode.Blackest, MosaicThemeMode.Blue })
                {
                    manager.Theme = theme;
                    border = GetBorder(button, "border");
                    Assert.Equal(new CornerRadius(3), border.CornerRadius);
                    Assert.NotEqual(lightBackground, Assert.IsType<LinearGradientBrush>(border.Background).GradientStops[0].Color);
                }

                button.IsEnabled = false;
                Assert.IsType<LinearGradientBrush>(GetBorder(button, "border").Background);
                button.IsEnabled = true;

                manager.Theme = MosaicThemeMode.HighContrast;
                border = GetBorder(button, "border");
                Assert.Equal(new CornerRadius(3), border.CornerRadius);
                Assert.Equal(Colors.Black, Assert.IsType<SolidColorBrush>(border.Background).Color);

                manager.Theme = MosaicThemeMode.Light;
                manager.UseRoundedButtons = false;
                border = GetBorder(button, "border");
                Assert.Equal(new CornerRadius(0), border.CornerRadius);
                Assert.Equal(new Thickness(0.8), border.BorderThickness);
                Assert.IsType<SolidColorBrush>(border.Background);
            });
        }

        private static Border GetBorder(Control control, string name)
        {
            control.ApplyTemplate();
            return Assert.IsType<Border>(control.Template.FindName(name, control));
        }

        [Theory]
        [InlineData(MosaicThemeMode.Dark, "#44474D", "#3F4247", "#232325", "#00203B", "#00325E", "#383A3F", "#37393D", "#2D2F32")]
        [InlineData(MosaicThemeMode.Black, "#44474D", "#3F4247", "#232325", "#00203B", "#00325E", "#383A3F", "#37393D", "#2D2F32")]
        [InlineData(MosaicThemeMode.Blackest, "#272A31", "#21242A", "#484C54", "#006CB7", "#007FD1", "#191B21", "#17191E", "#3C4048")]
        public void Rounded_Dark_Palettes_Use_The_Reference_Fills_And_Borders(
            MosaicThemeMode theme, string restingTop, string restingBottom, string restingBorder,
            string hoverBorder, string pressedBorder, string disabledTop, string disabledBottom, string disabledBorder)
        {
            RunSta(() =>
            {
                var manager = CreateManager(theme);
                manager.UseRoundedButtons = true;

                AssertState("Static", restingTop, restingBottom, restingBorder);
                AssertState("MouseOver", "#004986", "#00437B", hoverBorder);
                AssertState("Pressed", "#00579B", "#005AA0", pressedBorder);
                AssertState("Disabled", disabledTop, disabledBottom, disabledBorder);
                Assert.Same(manager["Button.Pressed.Background"], manager["SplitButton.Open.Background"]);
                Assert.Same(manager["Button.Pressed.Border"], manager["SplitButton.Open.Border"]);

                void AssertState(string state, string top, string bottom, string border)
                {
                    var fill = Assert.IsType<LinearGradientBrush>(manager[$"Button.{state}.Background"]);
                    Assert.Equal((Color)ColorConverter.ConvertFromString(top), fill.GradientStops[0].Color);
                    Assert.Equal((Color)ColorConverter.ConvertFromString(bottom), fill.GradientStops[1].Color);
                    Assert.Equal((Color)ColorConverter.ConvertFromString(border),
                        Assert.IsType<SolidColorBrush>(manager[$"Button.{state}.Border"]).Color);
                }
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
        public void CycleTheme_Visits_Every_Theme()
        {
            RunSta(() =>
            {
                var manager = CreateManager(MosaicThemeMode.Light);
                var visited = new List<MosaicThemeMode>();

                for (int i = 0; i < Enum.GetValues<MosaicThemeMode>().Length; i++)
                {
                    manager.CycleTheme();
                    visited.Add(manager.Theme);
                }

                Assert.Equal(
                    new[] { MosaicThemeMode.Dark, MosaicThemeMode.Black, MosaicThemeMode.Blackest, MosaicThemeMode.Blue, MosaicThemeMode.HighContrast, MosaicThemeMode.Light },
                    visited);
            });
        }
    }
}
