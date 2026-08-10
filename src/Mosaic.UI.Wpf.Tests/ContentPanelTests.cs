/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class ContentPanelTests
    {
        /// <summary>
        /// Runs the test body on an STA thread, which WPF controls require.
        /// </summary>
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

        /// <summary>
        /// Applies the control's shipped style and realizes its template, which also proves the XAML parses.
        /// </summary>
        private static ContentPanel Realize(ContentPanel control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/ContentPanel/ContentPanel.xaml")
            };

            control.Style = (Style)dictionary[typeof(ContentPanel)];
            control.Measure(new Size(400, 400));
            control.Arrange(new Rect(0, 0, 400, 400));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        [Fact]
        public void Defaults_To_Open_With_The_Chevron_Hidden()
        {
            RunSta(() =>
            {
                var panel = Realize(new ContentPanel { Content = new TextBlock { Text = "Body" } });
                var chevron = (ToggleButton)panel.Template.FindName("PART_ChevronButton", panel);
                var area = (FrameworkElement)panel.Template.FindName("PART_CollapseArea", panel);

                Assert.True(panel.IsOpen);
                Assert.False(panel.ShowChevron);
                Assert.Equal(Visibility.Collapsed, chevron.Visibility);
                Assert.Equal(Visibility.Visible, area.Visibility);
            });
        }

        [Fact]
        public void ShowChevron_Reveals_The_Toggle_Bound_To_IsOpen()
        {
            RunSta(() =>
            {
                var panel = Realize(new ContentPanel { ShowChevron = true, Content = new TextBlock { Text = "Body" } });
                var chevron = (ToggleButton)panel.Template.FindName("PART_ChevronButton", panel);

                Assert.Equal(Visibility.Visible, chevron.Visibility);
                Assert.True(chevron.IsChecked);

                // The two-way binding pushes the toggle state back onto the control.
                chevron.IsChecked = false;
                Assert.False(panel.IsOpen);

                chevron.IsChecked = true;
                Assert.True(panel.IsOpen);
            });
        }

        [Fact]
        public void Panel_Templated_While_Closed_Collapses_Without_Animating()
        {
            RunSta(() =>
            {
                var panel = Realize(new ContentPanel { ShowChevron = true, IsOpen = false, Content = new TextBlock { Text = "Body" } });
                var area = (FrameworkElement)panel.Template.FindName("PART_CollapseArea", panel);

                Assert.Equal(Visibility.Collapsed, area.Visibility);
                Assert.Equal(0, area.Height);
            });
        }

        [Fact]
        public void Toggling_IsOpen_Raises_The_Opened_And_Closed_Events()
        {
            RunSta(() =>
            {
                var panel = Realize(new ContentPanel { ShowChevron = true, Content = new TextBlock { Text = "Body" } });
                int opened = 0;
                int closed = 0;

                panel.Opened += (_, _) => opened++;
                panel.Closed += (_, _) => closed++;

                panel.IsOpen = false;
                Assert.Equal(0, opened);
                Assert.Equal(1, closed);

                panel.IsOpen = true;
                Assert.Equal(1, opened);
                Assert.Equal(1, closed);
            });
        }
    }
}
