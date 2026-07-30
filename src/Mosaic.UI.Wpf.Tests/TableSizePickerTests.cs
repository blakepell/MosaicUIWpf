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
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class TableSizePickerTests
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
        private static TableSizePicker Realize(TableSizePicker control)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/TableSizePicker/TableSizePicker.xaml")
            };

            control.Style = (Style)dictionary[typeof(TableSizePicker)];
            control.Measure(new Size(400, 400));
            control.Arrange(new Rect(0, 0, 400, 400));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        [Fact]
        public void Defaults_To_An_Eight_By_Eight_Grid_With_No_Selection()
        {
            RunSta(() =>
            {
                var picker = Realize(new TableSizePicker());

                Assert.Equal(8, picker.RowCount);
                Assert.Equal(8, picker.ColumnCount);
                Assert.Equal(64, picker.Cells.Count);
                Assert.Equal(0, picker.SelectedRowCount);
                Assert.Equal(0, picker.SelectedColumnCount);
                Assert.Equal("Insert Table", picker.SelectionText);
            });
        }

        [Fact]
        public void Template_Lays_The_Cells_Out_In_A_Uniform_Grid()
        {
            RunSta(() =>
            {
                var picker = Realize(new TableSizePicker { RowCount = 5, ColumnCount = 6 });
                var host = (ItemsControl)picker.Template.FindName("PART_Grid", picker);

                Assert.True(host.ActualWidth > 0);
                Assert.True(host.ActualHeight > 0);
                Assert.Equal(30, host.Items.Count);

                var panel = FindVisualChild<UniformGrid>(host);
                Assert.NotNull(panel);
                Assert.Equal(5, panel!.Rows);
                Assert.Equal(6, panel.Columns);
                Assert.Equal(30, panel.Children.Count);
            });
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
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
        public void Invalid_Dimensions_Are_Coerced()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker { RowCount = 0, ColumnCount = -5 };

                Assert.Equal(1, picker.RowCount);
                Assert.Equal(1, picker.ColumnCount);
                Assert.Single(picker.Cells);

                picker.RowCount = 500;
                Assert.Equal(50, picker.RowCount);
            });
        }

        [Fact]
        public void Changing_Dimensions_Rebuilds_The_Grid_And_Coerces_The_Selection()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker();
                picker.Select(6, 7);

                Assert.Equal(6, picker.SelectedRowCount);
                Assert.Equal(7, picker.SelectedColumnCount);

                picker.RowCount = 4;
                picker.ColumnCount = 3;

                Assert.Equal(12, picker.Cells.Count);
                Assert.Equal(4, picker.SelectedRowCount);
                Assert.Equal(3, picker.SelectedColumnCount);
            });
        }

        [Fact]
        public void Select_Raises_The_Routed_Event_And_Requests_Close()
        {
            RunSta(() =>
            {
                var picker = Realize(new TableSizePicker());

                TableSizeSelectedEventArgs? selected = null;
                int closeCount = 0;

                picker.TableSizeSelected += (_, e) => selected = e;
                picker.RequestClose += (_, _) => closeCount++;

                picker.Select(4, 6);

                Assert.NotNull(selected);
                Assert.Equal(4, selected!.RowCount);
                Assert.Equal(6, selected.ColumnCount);
                Assert.Equal(1, closeCount);
                Assert.Equal(4, picker.SelectedRowCount);
                Assert.Equal(6, picker.SelectedColumnCount);
                Assert.Equal(0, picker.PreviewRowCount);
                Assert.Equal("4 × 6 Table", picker.SelectionText);

                // Every cell in the region is committed, everything outside of it is not.
                Assert.All(picker.Cells, cell => Assert.Equal(cell.Row <= 4 && cell.Column <= 6, cell.IsCommittedSelected));
            });
        }

        [Fact]
        public void Select_Ignores_Sizes_Outside_The_Grid()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker();
                picker.Select(0, 3);
                picker.Select(3, 99);

                Assert.Equal(0, picker.SelectedRowCount);
                Assert.Equal(0, picker.SelectedColumnCount);
            });
        }

        [Fact]
        public void Clear_Resets_Everything()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker();
                picker.Select(3, 3);
                picker.ClearCommand.Execute(null);

                Assert.Equal(0, picker.SelectedRowCount);
                Assert.Equal(0, picker.SelectedColumnCount);
                Assert.Equal("Insert Table", picker.SelectionText);
                Assert.All(picker.Cells, cell => Assert.False(cell.IsCommittedSelected || cell.IsPreviewSelected));
            });
        }

        [Fact]
        public void OnShow_Honors_ClearSelectionOnShow()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker { ClearSelectionOnShow = false };
                picker.Select(2, 2);
                picker.OnShow();

                Assert.Equal(2, picker.SelectedRowCount);

                picker.ClearSelectionOnShow = true;
                picker.OnShow();

                Assert.Equal(0, picker.SelectedRowCount);
            });
        }

        [Fact]
        public void Keyboard_Moves_The_Preview_And_Commits()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker();
                var window = new Window
                {
                    Width = 400,
                    Height = 400,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    ShowActivated = false,
                    Content = picker
                };

                window.Show();
                picker.Focus();

                TableSizeSelectedEventArgs? selected = null;
                picker.TableSizeSelected += (_, e) => selected = e;

                void Press(Key key)
                {
                    picker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(picker), 0, key)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    });
                }

                // The first arrow press lands on 1x1 rather than moving past it.
                Press(Key.Right);
                Assert.Equal(1, picker.PreviewRowCount);
                Assert.Equal(1, picker.PreviewColumnCount);

                Press(Key.Right);
                Press(Key.Down);
                Assert.Equal(2, picker.PreviewRowCount);
                Assert.Equal(2, picker.PreviewColumnCount);
                Assert.Equal("2 × 2 Table", picker.SelectionText);

                // The preview does not touch the committed selection.
                Assert.Equal(0, picker.SelectedRowCount);

                Press(Key.End);
                Assert.Equal(8, picker.PreviewRowCount);
                Assert.Equal(8, picker.PreviewColumnCount);

                Press(Key.Home);
                Press(Key.Enter);

                Assert.NotNull(selected);
                Assert.Equal(1, selected!.RowCount);
                Assert.Equal(1, selected.ColumnCount);
                Assert.Equal(0, picker.PreviewRowCount);

                window.Close();
            });
        }

        [Fact]
        public void Escape_Clears_The_Preview_And_Requests_Close()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker();
                var window = new Window
                {
                    Width = 400,
                    Height = 400,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    ShowActivated = false,
                    Content = picker
                };

                window.Show();
                picker.Focus();

                int closeCount = 0;
                picker.RequestClose += (_, _) => closeCount++;

                picker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(picker), 0, Key.Down)
                {
                    RoutedEvent = Keyboard.KeyDownEvent
                });

                Assert.Equal(1, picker.PreviewRowCount);

                picker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(picker), 0, Key.Escape)
                {
                    RoutedEvent = Keyboard.KeyDownEvent
                });

                Assert.Equal(0, picker.PreviewRowCount);
                Assert.Equal(0, picker.PreviewColumnCount);
                Assert.Equal(1, closeCount);

                window.Close();
            });
        }

        [Fact]
        public void Selection_Text_Format_Is_Honored()
        {
            RunSta(() =>
            {
                var picker = new TableSizePicker
                {
                    SelectionTextFormat = "{0}r x {1}c",
                    EmptySelectionText = "Choose"
                };

                Assert.Equal("Choose", picker.SelectionText);

                picker.Select(2, 5);
                Assert.Equal("2r x 5c", picker.SelectionText);
            });
        }
    }
}
