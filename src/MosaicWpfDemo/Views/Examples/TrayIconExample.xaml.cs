/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class TrayIconExample
    {
        private readonly TrayIconExampleViewModel _viewModel = new();
        private TrayIcon? _tray;

        public TrayIconExample()
        {
            InitializeComponent();
            this.DataContext = _viewModel;
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_tray != null)
            {
                return;
            }

            // Resources are created lazily; looking it up instantiates the tray icon. The icon itself is
            // registered with the shell only while IsVisible (bound to IsTrayVisible) is true, and it removes
            // itself automatically when the application's dispatcher shuts down.
            _tray = (TrayIcon)this.FindResource("DemoTray");
            _viewModel.Tray = _tray;

            _tray.LeftClick += (_, args) => _viewModel.Write($"LeftClick ({args.InputSource}) at {args.ScreenPosition}");
            _tray.DoubleClick += (_, args) => _viewModel.Write($"DoubleClick ({args.InputSource}) at {args.ScreenPosition}");
            _tray.RightClick += (_, args) => _viewModel.Write($"RightClick ({args.InputSource}) at {args.ScreenPosition}");
            _tray.MouseEnter += (_, _) => _viewModel.WriteHover("MouseEnter");
            _tray.MouseLeave += (_, _) => _viewModel.WriteHover("MouseLeave");
        }
    }

    /// <summary>
    /// View model the tray icon's context menu binds to.
    /// </summary>
    public partial class TrayIconExampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isTrayVisible;

        [ObservableProperty]
        private string _toolTipText = "Mosaic UI for WPF Demo";

        [ObservableProperty]
        private bool _logHover;

        public ObservableCollection<string> Log { get; } = new();

        internal TrayIcon? Tray { get; set; }

        partial void OnIsTrayVisibleChanged(bool value)
        {
            this.Write(value ? "Tray icon shown" : "Tray icon hidden");
        }

        internal void Write(string message)
        {
            this.Log.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {message}");
        }

        internal void WriteHover(string message)
        {
            if (this.LogHover)
            {
                this.Write(message);
            }
        }

        [RelayCommand]
        private void RestoreWindow()
        {
            var window = Application.Current.MainWindow;

            if (window == null)
            {
                return;
            }

            window.Show();

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
            this.Write("Restored main window");
        }

        [RelayCommand]
        private void MinimizeWindow()
        {
            if (Application.Current.MainWindow is { } window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        [RelayCommand]
        private void SetIcon(string? name)
        {
            if (this.Tray == null)
            {
                return;
            }

            this.Tray.Icon = name switch
            {
                "shield" => new BitmapImage(new Uri("pack://application:,,,/Assets/shield-48.png")),
                "vector" => CreateVectorIcon(),
                _ => new BitmapImage(new Uri("pack://application:,,,/Assets/collage-48.png"))
            };

            this.Write($"Icon changed to {name}");
        }

        [RelayCommand]
        private void HideTray()
        {
            this.IsTrayVisible = false;
        }

        [RelayCommand]
        private void ClearLog()
        {
            this.Log.Clear();
        }

        private static ImageSource CreateVectorIcon()
        {
            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(Brushes.DodgerBlue, null, new EllipseGeometry(new Point(8, 8), 8, 8)));
            group.Children.Add(new GeometryDrawing(Brushes.White, null, new EllipseGeometry(new Point(8, 8), 3, 3)));

            var image = new DrawingImage(group);
            image.Freeze();
            return image;
        }
    }
}
