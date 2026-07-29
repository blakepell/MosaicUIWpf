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
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class ToastManagerTests
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
        /// Drains the dispatcher queue so layout, rendering and the adorner layer catch up.
        /// </summary>
        private static void Pump()
        {
            for (int i = 0; i < 3; i++)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
        }

        /// <summary>
        /// Shows an off screen window whose content is a grid, which is the surface toasts and
        /// dialogs are hosted over, and runs the test body against it.
        /// </summary>
        private static void WithWindow(Action<Window, Grid> body)
        {
            RunSta(() =>
            {
                var window = new Window
                {
                    Width = 800,
                    Height = 600,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Left = -4000,
                    Top = -4000
                };

                var grid = new Grid();
                window.Content = grid;
                window.Show();
                Pump();

                try
                {
                    body(window, grid);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Enumerates a visual and everything beneath it.
        /// </summary>
        private static IEnumerable<DependencyObject> SelfAndDescendants(DependencyObject root)
        {
            yield return root;

            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                foreach (var descendant in SelfAndDescendants(VisualTreeHelper.GetChild(root, i)))
                {
                    yield return descendant;
                }
            }
        }

        [Fact]
        public void FindHost_ResolvesTheWindowContent()
        {
            WithWindow((window, grid) =>
            {
                var child = new Button();
                grid.Children.Add(child);
                Pump();

                Assert.Same(grid, ToastManager.FindHost(child));
            });
        }

        [Fact]
        public void FindHost_ResolvesTheWindowContentForElementsInsideAnAdorner()
        {
            WithWindow((window, grid) =>
            {
                var content = new Button();
                var dialog = new ModalDialog { Content = content };
                _ = dialog.ShowAsync(grid);
                Pump();

                // Window.GetWindow cannot see out of an adorner because adorners do not inherit the
                // window service property; FindHost walks the visual tree instead.
                Assert.Null(Window.GetWindow(content));
                Assert.Same(grid, ToastManager.FindHost(content));

                dialog.Close();
            });
        }

        [Fact]
        public void ForElement_SharesOneManagerPerSurface()
        {
            WithWindow((window, grid) =>
            {
                var first = new Button();
                var second = new Button();
                grid.Children.Add(first);
                grid.Children.Add(second);
                Pump();

                var manager = ToastManager.ForElement(first);

                Assert.NotNull(manager);
                Assert.Same(grid, manager!.AdornedElement);
                Assert.Same(manager, ToastManager.ForElement(second));
            });
        }

        [Fact]
        public void Show_PlacesTheToastAboveAnOpenModalDialog()
        {
            WithWindow((window, grid) =>
            {
                var box = new CopyTextBox { ShowToast = true, Text = "copied" };
                var dialog = new ModalDialog { Content = box };
                _ = dialog.ShowAsync(grid);
                Pump();

                box.Copy();
                Pump();

                var adorners = AdornerLayer.GetAdornerLayer(grid)!.GetAdorners(grid);

                Assert.NotNull(adorners);
                Assert.Equal(2, adorners!.Length);

                // Adorners render in the order they were added, so the toast host must be last.
                Assert.Single(SelfAndDescendants(adorners[0]).OfType<ModalDialog>());
                Assert.Single(SelfAndDescendants(adorners[1]).OfType<ToastMessage>());

                dialog.Close();
            });
        }

        [Fact]
        public void Show_MovesTheToastHostBackToTheTopWhenAnotherAdornerWasAddedAfterIt()
        {
            WithWindow((window, grid) =>
            {
                var manager = new ToastManager(grid);
                manager.Show("First", "Shown before the dialog opened.", ToastSeverity.Info, null);
                Pump();

                var dialog = new ModalDialog { Content = new Button() };
                _ = dialog.ShowAsync(grid);
                Pump();

                var layer = AdornerLayer.GetAdornerLayer(grid)!;

                // The dialog was added last and therefore currently covers the toast host.
                Assert.Single(SelfAndDescendants(layer.GetAdorners(grid)![1]).OfType<ModalDialog>());

                manager.Show("Second", "Shown while the dialog is open.", ToastSeverity.Info, null);
                Pump();

                var adorners = layer.GetAdorners(grid);

                Assert.Equal(2, adorners!.Length);
                Assert.Equal(2, SelfAndDescendants(adorners[1]).OfType<ToastMessage>().Count());
                Assert.Empty(SelfAndDescendants(adorners[1]).OfType<ModalDialog>());

                dialog.Close();
            });
        }
    }
}
