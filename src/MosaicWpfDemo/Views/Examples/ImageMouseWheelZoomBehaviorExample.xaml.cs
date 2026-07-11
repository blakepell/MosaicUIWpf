/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ImageMouseWheelZoomBehaviorExample : INotifyPropertyChanged
    {
        private ImageSource? _imageSource;
        private string? _imagePath;
        private bool _requireCtrl;

        public ImageMouseWheelZoomBehaviorExample()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ImageSource? ImageSource
        {
            get => _imageSource;
            private set
            {
                if (!Equals(_imageSource, value))
                {
                    _imageSource = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasImage));
                }
            }
        }

        public string? ImagePath
        {
            get => _imagePath;
            private set
            {
                if (_imagePath != value)
                {
                    _imagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasImage => ImageSource != null;

        public bool RequireCtrl
        {
            get => _requireCtrl;
            set
            {
                if (_requireCtrl != value)
                {
                    _requireCtrl = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OpenImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp|All files|*.*",
                Title = "Open Image"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadImage(dialog.FileName);
            }
        }

        private void ImageDropSurface_OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = TryGetDroppedImagePath(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void ImageDropSurface_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetDroppedImagePath(e, out string? path))
            {
                LoadImage(path);
            }

            e.Handled = true;
        }

        private static bool TryGetDroppedImagePath(DragEventArgs e, out string? path)
        {
            path = null;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return false;
            }

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return false;
            }

            string candidate = files[0];
            string extension = System.IO.Path.GetExtension(candidate);
            if (!string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            path = candidate;
            return true;
        }

        private void LoadImage(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            ImageSource = bitmap;
            ImagePath = path;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
