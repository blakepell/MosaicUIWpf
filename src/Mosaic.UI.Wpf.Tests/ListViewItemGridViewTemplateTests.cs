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
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Tests that the native <see cref="ListViewItem"/> template picks the <see cref="GridViewRowPresenter"/>
    /// or the plain <see cref="ContentPresenter"/> from the container's own state, so recycled containers in a
    /// virtualizing panel never fall back to drawing <c>ToString()</c> while a <see cref="GridView"/> is active.
    /// </summary>
    public class ListViewItemGridViewTemplateTests
    {
        private const string GridViewRowPart = "GridViewRow";
        private const string ItemContentPart = "ItemContent";

        private sealed record Row(int Id, string Name, string Value)
        {
            public override string ToString() => $"ROW-{Id}";
        }

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
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
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

        /// <summary>
        /// Runs the dispatcher until everything queued at <see cref="DispatcherPriority.Background"/> or higher
        /// (layout, bindings, generator work) has been processed.
        /// </summary>
        private static void Flush()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
        }

        private static GridView CreateGridView()
        {
            var view = new GridView();
            view.Columns.Add(new GridViewColumn { Header = "Id", DisplayMemberBinding = new System.Windows.Data.Binding(nameof(Row.Id)), Width = 60 });
            view.Columns.Add(new GridViewColumn { Header = "Name", DisplayMemberBinding = new System.Windows.Data.Binding(nameof(Row.Name)), Width = 120 });
            view.Columns.Add(new GridViewColumn { Header = "Value", DisplayMemberBinding = new System.Windows.Data.Binding(nameof(Row.Value)), Width = 120 });
            return view;
        }

        private static (Window Window, ListView ListView, ThemeManager Manager) CreateHost(ViewBase? view, IEnumerable<Row> rows, MosaicThemeMode theme = MosaicThemeMode.Dark)
        {
            var manager = CreateManager(theme);

            var listView = new ListView { View = view, ItemsSource = rows };
            VirtualizingPanel.SetIsVirtualizing(listView, true);
            VirtualizingPanel.SetVirtualizationMode(listView, VirtualizationMode.Recycling);
            VirtualizingPanel.SetScrollUnit(listView, ScrollUnit.Pixel);
            ScrollViewer.SetCanContentScroll(listView, true);

            var window = new Window
            {
                Width = 500,
                Height = 400,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowStyle = WindowStyle.None,
                Content = listView,
            };

            window.Resources.MergedDictionaries.Add(manager);
            window.Show();
            Flush();

            return (window, listView, manager);
        }

        private static List<ListViewItem> RealizedContainers(ListView listView)
        {
            var result = new List<ListViewItem>();
            CollectContainers(listView, result);
            return result;
        }

        private static void CollectContainers(DependencyObject parent, List<ListViewItem> result)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ListViewItem item)
                {
                    result.Add(item);
                    continue;
                }

                CollectContainers(child, result);
            }
        }

        /// <summary>
        /// Asserts that every realized, visible container shows the expected presenter.
        /// </summary>
        private static void AssertMode(ListView listView, bool expectGridRow, string context)
        {
            var containers = RealizedContainers(listView);
            Assert.NotEmpty(containers);

            foreach (var container in containers)
            {
                // Recycled containers parked off-screen by the panel are collapsed; skip those.
                if (container.Visibility != Visibility.Visible)
                {
                    continue;
                }

                container.ApplyTemplate();
                var gridRow = Assert.IsType<GridViewRowPresenter>(container.Template.FindName(GridViewRowPart, container));
                var content = Assert.IsType<ContentPresenter>(container.Template.FindName(ItemContentPart, container));

                var expectedGrid = expectGridRow ? Visibility.Visible : Visibility.Collapsed;
                var expectedContent = expectGridRow ? Visibility.Collapsed : Visibility.Visible;

                Assert.True(gridRow.Visibility == expectedGrid && content.Visibility == expectedContent,
                    $"{context}: container for '{container.Content}' has GridViewRow={gridRow.Visibility}, ItemContent={content.Visibility}.");
            }
        }

        private static ObservableCollection<Row> CreateRows(int count)
        {
            var rows = new ObservableCollection<Row>();

            for (int i = 0; i < count; i++)
            {
                rows.Add(new Row(i, $"Name {i}", $"Value {i}"));
            }

            return rows;
        }

        [Fact]
        public void GridView_Prepares_ColumnCollection_As_A_Local_Value_On_The_Container()
        {
            RunSta(() =>
            {
                var (window, listView, _) = CreateHost(CreateGridView(), CreateRows(10));

                try
                {
                    var container = (ListViewItem)listView.ItemContainerGenerator.ContainerFromIndex(0);
                    Assert.NotNull(GridView.GetColumnCollection(container));
                    Assert.Same(((GridView)listView.View).Columns, GridView.GetColumnCollection(container));
                    Assert.Equal(BaseValueSource.Local, DependencyPropertyHelper.GetValueSource(container, GridView.ColumnCollectionProperty).BaseValueSource);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void ColumnCollection_Does_Not_Inherit_Into_Nested_Items()
        {
            // The template triggers on GridView.ColumnCollection; if it inherited, a View-less ListView hosted
            // inside a GridView cell would pick up the outer columns and draw its items as grid rows.
            var metadata = GridView.ColumnCollectionProperty.GetMetadata(typeof(ListViewItem)) as FrameworkPropertyMetadata;
            Assert.False(metadata?.Inherits ?? false);
        }

        [Fact]
        public void GridView_Mode_Shows_Row_Presenter_For_Every_Realized_Container()
        {
            RunSta(() =>
            {
                var (window, listView, _) = CreateHost(CreateGridView(), CreateRows(200));

                try
                {
                    AssertMode(listView, expectGridRow: true, "initial");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void No_View_Mode_Shows_Content_Presenter()
        {
            RunSta(() =>
            {
                var (window, listView, _) = CreateHost(null, CreateRows(50));

                try
                {
                    AssertMode(listView, expectGridRow: false, "no view");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Switching_View_At_Runtime_Updates_Every_Container()
        {
            RunSta(() =>
            {
                var (window, listView, _) = CreateHost(null, CreateRows(50));

                try
                {
                    AssertMode(listView, expectGridRow: false, "start without view");

                    listView.View = CreateGridView();
                    Flush();
                    AssertMode(listView, expectGridRow: true, "after setting GridView");

                    listView.View = null;
                    Flush();
                    AssertMode(listView, expectGridRow: false, "after clearing view");

                    listView.View = CreateGridView();
                    Flush();
                    AssertMode(listView, expectGridRow: true, "after setting GridView again");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Recycling_Under_Churn_Never_Drops_The_Row_Presenter()
        {
            RunSta(() =>
            {
                var rows = CreateRows(2000);
                var (window, listView, manager) = CreateHost(CreateGridView(), rows);
                var random = new Random(1234);
                int nextId = rows.Count;
                var themes = new[] { MosaicThemeMode.Light, MosaicThemeMode.Dark };

                try
                {
                    var scrollViewer = FindScrollViewer(listView);
                    Assert.NotNull(scrollViewer);

                    for (int iteration = 0; iteration < 150; iteration++)
                    {
                        // Operations are deliberately batched without flushing in between so that
                        // containers are recycled, detached and re-templated within a single tick.
                        int operations = random.Next(3, 12);

                        for (int op = 0; op < operations; op++)
                        {
                            switch (random.Next(7))
                            {
                                case 0:
                                case 1:
                                    var added = new Row(nextId, $"Name {nextId}", $"Value {nextId}");
                                    nextId++;
                                    rows.Insert(random.Next(rows.Count + 1), added);
                                    listView.ScrollIntoView(added);
                                    break;
                                case 2:
                                    rows.RemoveAt(random.Next(rows.Count));
                                    break;
                                case 3:
                                    listView.ScrollIntoView(rows[^1]);
                                    break;
                                case 4:
                                    scrollViewer!.ScrollToVerticalOffset(random.NextDouble() * scrollViewer.ScrollableHeight);
                                    break;
                                case 5:
                                    listView.Items.Refresh();
                                    break;
                                case 6:
                                    manager.Theme = themes[random.Next(themes.Length)];
                                    break;
                            }

                            if (random.Next(3) == 0)
                            {
                                listView.UpdateLayout();
                            }
                        }

                        Flush();
                        AssertMode(listView, expectGridRow: true, $"iteration {iteration}");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ScrollViewer viewer)
                {
                    return viewer;
                }

                var nested = FindScrollViewer(child);

                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
