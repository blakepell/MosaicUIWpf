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
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;
using Xunit;
using Hyperlink = Mosaic.UI.Wpf.Controls.Hyperlink;

namespace Mosaic.UI.Wpf.Tests
{
    public class HyperlinkTests
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
        /// Applies the control's shipped style and realizes its template.  The default style is not
        /// resolved automatically outside of a hosting application (the library merges its
        /// dictionaries through ThemeManager rather than the theme lookup), so the control's
        /// dictionary is loaded explicitly here.  Doing so also proves the XAML parses.
        /// </summary>
        private static Hyperlink Realize(Hyperlink control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/HyperLink/Hyperlink.xaml")
            };

            control.Style = (Style)dictionary[typeof(Hyperlink)];
            control.Measure(new Size(300, 28));
            control.Arrange(new Rect(0, 0, 300, 28));
            control.ApplyTemplate();

            return control;
        }

        /// <summary>
        /// Walks the realized visual tree and returns the <see cref="TextBlock"/> the template renders the link with.
        /// </summary>
        private static TextBlock GetRenderedTextBlock(Hyperlink control)
        {
            var child = VisualTreeHelper.GetChild(control, 0);
            var textBlock = child as TextBlock;

            Assert.NotNull(textBlock);

            return textBlock!;
        }

        [Fact]
        public void ShippedTemplate_IsAppliedAndBuildsAVisualTree()
        {
            RunSta(() =>
            {
                var control = Realize(new Hyperlink { Text = "hello" });

                Assert.NotNull(control.Template);
                Assert.True(VisualTreeHelper.GetChildrenCount(control) > 0);
                Assert.Equal("hello", GetRenderedTextBlock(control).Text);
            });
        }

        [Fact]
        public void ChangingText_AfterFirstRender_UpdatesTheRenderedText()
        {
            RunSta(() =>
            {
                // This is the recycled-list-row / re-bound-view-model case.  The rendered text used to stay
                // on whatever the control was first realized with.
                var control = Realize(new Hyperlink { Text = "first" });
                var textBlock = GetRenderedTextBlock(control);

                Assert.Equal("first", textBlock.Text);

                control.Text = "second";

                Assert.Equal("second", control.DisplayText);
                Assert.Equal("second", textBlock.Text);
            });
        }

        [Fact]
        public void ChangingNavigateUri_AfterFirstRender_UpdatesTheRenderedTextAndToolTip()
        {
            RunSta(() =>
            {
                var control = Realize(new Hyperlink { NavigateUri = "https://www.blakepell.com" });
                var textBlock = GetRenderedTextBlock(control);

                Assert.Equal("https://www.blakepell.com", textBlock.Text);
                Assert.Equal("https://www.blakepell.com", textBlock.ToolTip);

                control.NavigateUri = "https://www.apexgate.net";

                Assert.Equal("https://www.apexgate.net", control.DisplayText);
                Assert.Equal("https://www.apexgate.net", textBlock.Text);
                Assert.Equal("https://www.apexgate.net", control.AutoToolTip);
                Assert.Equal("https://www.apexgate.net", textBlock.ToolTip);
            });
        }

        [Fact]
        public void Text_TakesPrecedenceOverNavigateUri_AndClearingItFallsBack()
        {
            RunSta(() =>
            {
                var control = Realize(new Hyperlink { Text = "Blake's Site", NavigateUri = "https://www.blakepell.com" });
                var textBlock = GetRenderedTextBlock(control);

                Assert.Equal("Blake's Site", textBlock.Text);

                // The URI is still what the tooltip advertises, so the user can see where the link goes.
                Assert.Equal("https://www.blakepell.com", textBlock.ToolTip);

                control.Text = null;

                Assert.Equal("https://www.blakepell.com", control.DisplayText);
                Assert.Equal("https://www.blakepell.com", textBlock.Text);
            });
        }

        [Fact]
        public void DisplayText_IsEmptyWhenNeitherTextNorNavigateUriIsSet()
        {
            RunSta(() =>
            {
                var control = Realize(new Hyperlink());

                Assert.Equal(string.Empty, control.DisplayText);
                Assert.Equal(string.Empty, GetRenderedTextBlock(control).Text);
                Assert.Null(control.AutoToolTip);
            });
        }

        [Fact]
        public void AutoToolTip_PrefersAnExplicitlyAssignedToolTip()
        {
            RunSta(() =>
            {
                var control = Realize(new Hyperlink { NavigateUri = "https://www.blakepell.com", ToolTip = "Go to Blake's site" });

                Assert.Equal("Go to Blake's site", control.AutoToolTip);
                Assert.Equal("Go to Blake's site", GetRenderedTextBlock(control).ToolTip);
            });
        }

        [Fact]
        public void AutoToolTip_DescribesCommandLinksThatHaveNoUri()
        {
            RunSta(() =>
            {
                var control = Realize(new Hyperlink { Text = "Do the thing", Command = new RelayCommand(() => { }) });

                Assert.Equal("This link will execute code defined by the application.", control.AutoToolTip);
            });
        }

        [Fact]
        public void DisablingAutoToolTip_ClearsTheRenderedToolTip()
        {
            RunSta(() =>
            {
                var control = Realize(new Hyperlink { NavigateUri = "https://www.blakepell.com" });
                var textBlock = GetRenderedTextBlock(control);

                Assert.NotNull(textBlock.ToolTip);

                control.EnableAutoToolTip = false;

                Assert.Null(control.AutoToolTip);
                Assert.Null(textBlock.ToolTip);
            });
        }

        [Fact]
        public void OnClick_ReturnsTheSameCommandInstanceEveryTime()
        {
            RunSta(() =>
            {
                var control = new Hyperlink();

                Assert.Same(control.OnClick, control.OnClick);
            });
        }
    }
}
