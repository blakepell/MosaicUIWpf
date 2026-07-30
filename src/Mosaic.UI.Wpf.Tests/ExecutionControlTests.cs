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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class ExecutionControlTests
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
        private static ExecutionControl Realize(ExecutionControl control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/ExecutionControl/ExecutionControl.xaml")
            };

            control.Style = (Style)dictionary[typeof(ExecutionControl)];
            control.Measure(new Size(200, 40));
            control.Arrange(new Rect(0, 0, 200, 40));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        private static ButtonBase Button(ExecutionControl control, string partName)
        {
            return (ButtonBase)control.Template.FindName(partName, control);
        }

        private static Path FirstIcon(ButtonBase button)
        {
            return FindVisualChild<Path>(button)!;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T match)
                {
                    return match;
                }

                var found = FindVisualChild<T>(child);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        [Fact]
        public void ShippedTemplate_IsAppliedAndBuildsAllThreeButtons()
        {
            RunSta(() =>
            {
                var control = Realize(new ExecutionControl());

                Assert.NotNull(control.Template);
                Assert.NotNull(Button(control, "PART_PlayButton"));
                Assert.NotNull(Button(control, "PART_PauseButton"));
                Assert.NotNull(Button(control, "PART_StopButton"));
            });
        }

        [Fact]
        public void Buttons_AreDisabledWhenTheirCommandCannotExecute()
        {
            RunSta(() =>
            {
                var control = Realize(new ExecutionControl
                {
                    PlayCommand = new DelegateCommand(canExecute: false),
                    StopCommand = new DelegateCommand(canExecute: true)
                });

                control.UpdateLayout();

                Assert.False(Button(control, "PART_PlayButton").IsEnabled);
                Assert.True(Button(control, "PART_StopButton").IsEnabled);
            });
        }

        [Fact]
        public void Icon_UsesTheDisabledBrushWhenTheCommandCannotExecute()
        {
            RunSta(() =>
            {
                var control = Realize(new ExecutionControl
                {
                    PlayBrush = Brushes.Green,
                    StopBrush = Brushes.Red,
                    DisabledBrush = Brushes.Gray,
                    PlayCommand = new DelegateCommand(canExecute: false),
                    StopCommand = new DelegateCommand(canExecute: true)
                });

                control.UpdateLayout();

                Assert.Equal(Brushes.Gray, FirstIcon(Button(control, "PART_PlayButton")).Fill);
                Assert.Equal(Brushes.Red, FirstIcon(Button(control, "PART_StopButton")).Fill);
            });
        }

        [Fact]
        public void Clicking_RaisesTheRoutedEventAndExecutesTheCommand()
        {
            RunSta(() =>
            {
                var command = new DelegateCommand(canExecute: true);
                var control = Realize(new ExecutionControl { PlayCommand = command, PlayCommandParameter = "go" });
                var raised = 0;

                control.PlayClick += (_, _) => raised++;
                ((Button)Button(control, "PART_PlayButton")).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                Assert.Equal(1, raised);
            });
        }

        [Fact]
        public void ShowPauseButton_CollapsesThePauseButton()
        {
            RunSta(() =>
            {
                var control = Realize(new ExecutionControl { ShowPauseButton = false });
                control.UpdateLayout();

                Assert.Equal(Visibility.Collapsed, Button(control, "PART_PauseButton").Visibility);
                Assert.Equal(Visibility.Visible, Button(control, "PART_PlayButton").Visibility);
            });
        }

        private sealed class DelegateCommand(bool canExecute) : ICommand
        {
            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => canExecute;

            public void Execute(object? parameter)
            {
            }
        }
    }
}
