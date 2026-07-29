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
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class CopyTextBoxTests
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
        private static CopyTextBox Realize(CopyTextBox control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/CopyTextBox/CopyTextBox.xaml")
            };

            control.Style = (Style)dictionary[typeof(CopyTextBox)];
            control.Measure(new Size(300, 28));
            control.Arrange(new Rect(0, 0, 300, 28));
            control.ApplyTemplate();

            return control;
        }

        [Fact]
        public void ShippedTemplate_IsAppliedAndBuildsAVisualTree()
        {
            RunSta(() =>
            {
                var control = Realize(new CopyTextBox { Text = "hello" });

                Assert.NotNull(control.Template);
                Assert.True(VisualTreeHelper.GetChildrenCount(control) > 0);
            });
        }

        [Fact]
        public void ShowToast_IsOffByDefault()
        {
            RunSta(() => Assert.False(new CopyTextBox().ShowToast));
        }

        [Fact]
        public void CopyIcon_HasADefaultGlyph()
        {
            RunSta(() => Assert.NotNull(Realize(new CopyTextBox()).CopyIcon));
        }

        [Fact]
        public void Copy_RaisesTextCopiedWithTheControlsText()
        {
            RunSta(() =>
            {
                var control = Realize(new CopyTextBox { Text = "copy me" });
                TextCopiedEventArgs? args = null;

                control.TextCopied += (_, e) => args = e;
                control.Copy();

                Assert.NotNull(args);
                Assert.Equal("copy me", args!.Text);
            });
        }

        [Fact]
        public void Copy_ExecutesTheCommandWithTheTextWhenNoParameterIsSet()
        {
            RunSta(() =>
            {
                var control = Realize(new CopyTextBox { Text = "token" });
                object? parameter = null;

                control.Command = new DelegateCommand(p => parameter = p);
                control.Copy();

                Assert.Equal("token", parameter);
            });
        }

        [Fact]
        public void Copy_DoesNotThrowWhenTheTextIsEmpty()
        {
            RunSta(() =>
            {
                var control = Realize(new CopyTextBox());
                control.Copy();
            });
        }

        private sealed class DelegateCommand(Action<object?> execute) : System.Windows.Input.ICommand
        {
            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => execute(parameter);
        }
    }
}
