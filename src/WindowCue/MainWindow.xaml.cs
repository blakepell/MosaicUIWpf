/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Argus.Memory;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using WindowCue.Common;
using WindowCue.Services;
using WindowCue.ViewModels;

namespace WindowCue
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _vm = null!;

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
            var settings  = AppServices.GetService<AppSettings>();
            var dockSvc   = AppServices.GetRequiredService<ScreenDockingService>();

            if (settings != null)
            {
                if (Enum.TryParse<DockEdge>(settings.DockEdge, out var edge))
                    _vm.DockEdge = edge;

                _vm.MonitorDeviceName = settings.MonitorDeviceName;
            }

            dockSvc.Dock(this, _vm.DockEdge, _vm.MonitorDeviceName);

            // Restore pinned items from settings
            if (settings?.PinnedItems?.Count > 0)
                await _vm.RestoreFromPersistedItemsAsync(settings.PinnedItems);
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
                Text        = vm.Label,
                ImageSource = vm.Icon,
                Tag         = vm,
                Command     = _vm.FocusItemCommand,
                CommandParameter = vm
            };

            // Keep the SideMenuItem label in sync with the VM label
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ToolbarItemViewModel.Label))
                    mi.Text = vm.Label;
                if (e.PropertyName == nameof(ToolbarItemViewModel.Icon))
                    mi.ImageSource = vm.Icon;
            };

            return mi;
        }

        // ── Drag-to-move ─────────────────────────────────────────────────────

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
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

            var rename = new MenuItem { Header = "Rename…" };
            rename.Click += (_, _) => _vm.RenameItemCommand.Execute(vm);

            var separator = new Separator();

            var remove = new MenuItem { Header = "Remove" };
            remove.Click += (_, _) => _vm.RemoveItemCommand.Execute(vm);

            menu.Items.Add(rename);
            menu.Items.Add(separator);
            menu.Items.Add(remove);

            menu.PlacementTarget = target;
            menu.IsOpen = true;
        }

        // ── ItemClicked: focus the target window ─────────────────────────────

        private void ToolbarMenu_ItemClicked(object sender, Mosaic.UI.Wpf.Controls.SideMenuItemClickEventArgs e)
        {
            if (e.Item.Tag is ToolbarItemViewModel vm)
                _vm.FocusItemCommand.Execute(vm);
        }

        // ── Add button ───────────────────────────────────────────────────────

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.AddWindowCommand.Execute(null);
        }

        // ── Settings button (opens context menu) ─────────────────────────────

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                btn.ContextMenu!.IsOpen = true;
        }

        private void DockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string edgeName)
                _vm.SetDockEdgeCommand.Execute(edgeName);
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            AppServices.GetRequiredService<ThemeManager>().ToggleTheme();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

