/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowCue.Common;
using WindowCue.Services;
using WindowCue.ViewModels;

namespace WindowCue
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm = null!;
        private AppBarService _appBar = null!;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _vm = AppServices.GetRequiredService<MainWindowViewModel>();
            DataContext = _vm;

            // Sync toolbar items → SideMenu on collection changes
            _vm.Items.CollectionChanged += Items_CollectionChanged;
            RebuildSideMenuItems();

            // Restore dock position from settings
            var settings = AppServices.GetService<AppSettings>();
            var dockSvc = AppServices.GetRequiredService<ScreenDockingService>();

            if (settings != null)
            {
                if (Enum.TryParse<DockEdge>(settings.DockEdge, out var edge))
                {
                    _vm.DockEdge = edge;
                }

                _vm.MonitorDeviceName = settings.MonitorDeviceName;
            }

            dockSvc.Dock(this, _vm.DockEdge, _vm.MonitorDeviceName);

            // Register the window as a shell AppBar so Windows reserves the docked
            // strip as work area.  Other windows will snap and maximize around it.
            _appBar = AppServices.GetRequiredService<AppBarService>();
            _appBar.Register(this, _vm.DockEdge, _vm.MonitorDeviceName);

            // Keep the AppBar reservation in sync when the user changes the dock edge.
            _vm.PropertyChanged += OnVmPropertyChanged;

            // Remove the reservation as soon as the window closes.
            Closed += (_, _) =>
            {
                _vm.PropertyChanged -= OnVmPropertyChanged;
                _appBar.Unregister();
            };

            // Restore pinned items from settings
            if (settings?.PinnedItems?.Count > 0)
            {
                await _vm.RestoreFromPersistedItemsAsync(settings.PinnedItems);
            }
        }

        // ── SideMenu synchronization ─────────────────────────────────────────

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => RebuildSideMenuItems();

        private void RebuildSideMenuItems()
        {
            ToolbarMenu.MenuItems.Clear();
            foreach (var item in _vm.Items)
                ToolbarMenu.MenuItems.Add(CreateSideMenuItem(item));
        }

        private SideMenuItem CreateSideMenuItem(ToolbarItemViewModel vm)
        {
            var mi = new SideMenuItem
            {
                Text = vm.Label,
                ImageSource = vm.IsAvailable ? vm.Icon : ToGrayscale(vm.Icon),
                Tag = vm,
                Command = _vm.FocusItemCommand,
                CommandParameter = vm
            };

            // Keep the SideMenuItem label and icon in sync with the VM.
            // When the item becomes unavailable its icon is shown in greyscale;
            // the item stays clickable so the focus command can attempt re-bind.
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ToolbarItemViewModel.Label))
                {
                    mi.Text = vm.Label;
                }

                if (e.PropertyName == nameof(ToolbarItemViewModel.Icon) ||
                    e.PropertyName == nameof(ToolbarItemViewModel.IsAvailable))
                {
                    mi.ImageSource = vm.IsAvailable ? vm.Icon : ToGrayscale(vm.Icon);
                }
            };

            return mi;
        }

        /// <summary>
        /// Converts a <see cref="BitmapSource"/> to grayscale while preserving the alpha channel.
        /// Returns the original source unchanged if conversion fails or the source is not a BitmapSource.
        /// </summary>
        private static ImageSource? ToGrayscale(ImageSource? source)
        {
            if (source is not BitmapSource bitmap)
                return source;

            try
            {
                // Convert to Bgra32 so we can read/write all four channels uniformly.
                var bgra = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                int width = bgra.PixelWidth;
                int height = bgra.PixelHeight;
                int stride = width * 4;
                var pixels = new byte[stride * height];
                bgra.CopyPixels(pixels, stride, 0);

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte b = pixels[i];
                    byte g = pixels[i + 1];
                    byte r = pixels[i + 2];
                    // Rec. 709 luminance
                    byte lum = (byte)(0.2126 * r + 0.7152 * g + 0.0722 * b);
                    pixels[i] = lum;
                    pixels[i + 1] = lum;
                    pixels[i + 2] = lum;
                    // pixels[i + 3] = alpha — preserved as-is
                }

                var result = BitmapSource.Create(
                    width, height, bitmap.DpiX, bitmap.DpiY,
                    PixelFormats.Bgra32, null, pixels, stride);
                result.Freeze();
                return result;
            }
            catch
            {
                return source;
            }
        }

        // ── AppBar sync ───────────────────────────────────────────────────────

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MainWindowViewModel.DockEdge)
                               or nameof(MainWindowViewModel.MonitorDeviceName))
            {
                // Post asynchronously so the ViewModel's synchronous Dock() call
                // completes first; the AppBar-confirmed position then wins.
                Dispatcher.InvokeAsync(
                    () => _appBar.UpdateEdge(this, _vm.DockEdge, _vm.MonitorDeviceName));
            }
        }

        // ── Drag-to-move ─────────────────────────────────────────────────────

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // ── Right-click context menu on toolbar items ─────────────────────────

        private void ToolbarMenu_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Walk the visual tree from the click origin to find the SideMenuItemControl
            var element = e.OriginalSource as DependencyObject;
            while (element != null)
            {
                if (element is SideMenuItemControl ctrl &&
                    ctrl.MenuItem?.Tag is ToolbarItemViewModel vm)
                {
                    ShowItemContextMenu(vm, ctrl);
                    e.Handled = true;
                    return;
                }
                element = VisualTreeHelper.GetParent(element);
            }
        }

        private void ShowItemContextMenu(ToolbarItemViewModel vm, FrameworkElement target)
        {
            var menu = new ContextMenu();

            var rename = new MenuItem { Header = "Rename\u2026" };
            rename.Click += (_, _) => _vm.RenameItemCommand.Execute(vm);

            var remove = new MenuItem { Header = "Remove" };
            remove.Click += (_, _) => _vm.RemoveItemCommand.Execute(vm);

            var properties = new MenuItem { Header = "Properties\u2026" };
            properties.Click += (_, _) => _vm.ShowItemPropertiesCommand.Execute(vm);

            menu.Items.Add(rename);
            menu.Items.Add(new Separator());
            menu.Items.Add(remove);
            menu.Items.Add(new Separator());
            menu.Items.Add(properties);

            menu.PlacementTarget = target;
            menu.IsOpen = true;
        }

        // ── ItemClicked: focus the target window ─────────────────────────────

        private void ToolbarMenu_ItemClicked(object sender, Mosaic.UI.Wpf.Controls.SideMenuItemClickEventArgs e)
        {
            if (e.Item.Tag is ToolbarItemViewModel vm)
            {
                _vm.FocusItemCommand.Execute(vm);
            }
        }

        // ── Add button ───────────────────────────────────────────────────────

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddWindowCommand.Execute(null);
        }

        // ── Settings button (opens context menu) ─────────────────────────────

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu is ContextMenu menu)
            {
                menu.PlacementTarget = btn;
                menu.IsOpen = true;
            }
        }

        private void DockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string edgeName)
            {
                _vm.SetDockEdgeCommand.Execute(edgeName);
            }
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            AppServices.GetRequiredService<ThemeManager>().ToggleTheme();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Remove all pinned items?",
                "Remove All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _vm.Items.Clear();
            }
        }
    }
}

