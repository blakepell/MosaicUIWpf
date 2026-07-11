/*
 * ChromaSwap
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using ChromaSwap.Common;
using ChromaSwap.Dialogs;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ChromaSwap
{
    /// <summary>
    /// The ChromaSwap workspace: load an image, inspect pixels with the loupe, pick a color and
    /// swap it (optionally preserving shading) with tolerance, browse shades/tints and export them.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MaxHistoryEntries = 21;

        private WriteableBitmap? _bitmap;
        private readonly List<byte[]> _history = new();
        private int _historyIndex = -1;
        private Color? _selectedColor;
        private string _fileName = "chromaswap-image";
        private ToastManager? _toasts;
        private readonly ObservableCollection<ShadeSwatch> _shades = new();
        private bool _shadesPanelOpen;
        private bool _isSwapping;

        public MainWindow()
        {
            InitializeComponent();

            this.UndoCommand = new RelayCommand(this.Undo);
            this.PasteCommand = new RelayCommand(this.PasteImage);

            ShadesItems.ItemsSource = _shades;
        }

        /// <summary>
        /// Bound to Ctrl+Z.
        /// </summary>
        public ICommand UndoCommand { get; }

        /// <summary>
        /// Bound to Ctrl+V.
        /// </summary>
        public ICommand PasteCommand { get; }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _toasts = new ToastManager(RootGrid);
        }

        private void ButtonToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            theme.ToggleTheme();

            var appSettings = AppServices.GetRequiredService<AppSettings>();
            appSettings.Theme = theme.Theme;
            App.SaveAppSettings();
        }

        /// <summary>
        /// Shows a short lived toast in the bottom right of the window.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="severity">The severity which drives the color scheme.</param>
        private void ShowToast(string message, ToastSeverity severity = ToastSeverity.Success)
        {
            try
            {
                _toasts ??= new ToastManager(RootGrid);
                _toasts.Show(string.Empty, message, severity, TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Toasts are informational only; never let one take the app down.
            }
        }

        /// <summary>
        /// Copies text to the clipboard with a confirmation toast.  Clipboard access can fail
        /// when another process has it locked.
        /// </summary>
        /// <param name="text">The text to copy.</param>
        private void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
                this.ShowToast($"Copied {text}");
            }
            catch (Exception)
            {
                this.ShowToast("Copy failed", ToastSeverity.Error);
            }
        }

        #region Image loading

        private void OpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Open Image",
                    Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp|All Files|*.*"
                };

                if (dialog.ShowDialog(this) == true)
                {
                    this.LoadImageFromFile(dialog.FileName);
                    this.ShowToast("Select a pixel and then choose the Swap Colors button.", ToastSeverity.Info);
                }
            }
            catch (Exception ex)
            {
                this.ShowToast($"Unable to open image: {ex.Message}", ToastSeverity.Error);
            }
        }

        private void WorkspaceGrid_OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void WorkspaceGrid_OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                {
                    this.LoadImageFromFile(files[0]);
                    this.ShowToast("Select a pixel and then choose the Swap Colors button.", ToastSeverity.Info);
                }
            }
            catch (Exception ex)
            {
                this.ShowToast($"Unable to load dropped file: {ex.Message}", ToastSeverity.Error);
            }
        }

        /// <summary>
        /// Loads an image from the clipboard (Ctrl+V).
        /// </summary>
        private void PasteImage()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                {
                    return;
                }

                var source = Clipboard.GetImage();

                if (source == null)
                {
                    return;
                }

                this.LoadBitmap(source, "chromaswap-image");
                this.ShowToast("Image pasted from clipboard");
            }
            catch (Exception ex)
            {
                this.ShowToast($"Unable to paste image: {ex.Message}", ToastSeverity.Error);
            }
        }

        /// <summary>
        /// Loads an image file from disk into the workspace.
        /// </summary>
        /// <param name="path">The full path of the image file.</param>
        private void LoadImageFromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    this.ShowToast("File not found.", ToastSeverity.Error);
                    return;
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();

                this.LoadBitmap(image, Path.GetFileNameWithoutExtension(path));
            }
            catch (Exception ex)
            {
                this.ShowToast($"Please choose a valid image file. ({ex.Message})", ToastSeverity.Error);
            }
        }

        /// <summary>
        /// Converts any bitmap source into the editable 96 DPI BGRA32 working bitmap and resets
        /// the selection, loupe and undo history.
        /// </summary>
        /// <param name="source">The decoded image.</param>
        /// <param name="fileName">The base file name used when saving.</param>
        private void LoadBitmap(BitmapSource source, string fileName)
        {
            var converted = source.Format == PixelFormats.Bgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;

            if (width <= 0 || height <= 0)
            {
                this.ShowToast("The image has no pixels to edit.", ToastSeverity.Error);
                return;
            }

            int stride = width * 4;
            var pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            // Normalize to 96 DPI so element coordinates map 1:1 to pixels at 100% zoom.
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

            _fileName = string.IsNullOrWhiteSpace(fileName) ? "chromaswap-image" : fileName;

            MainImage.Source = _bitmap;

            _history.Clear();
            _history.Add(pixels);
            _historyIndex = 0;

            _selectedColor = null;
            _shades.Clear();

            EmptyState.Visibility = Visibility.Collapsed;
            CanvasContainer.Visibility = Visibility.Visible;
            Loupe.Visibility = Visibility.Collapsed;
            SaveButton.IsEnabled = true;
            SwapButton.IsEnabled = false;
            UndoButton.IsEnabled = false;

            this.UpdateColorInfo(null);
        }

        #endregion

        #region Pixel picking / loupe

        /// <summary>
        /// Maps a mouse position over the (uniformly stretched) image element to pixel coordinates.
        /// </summary>
        /// <param name="position">The mouse position relative to the image element.</param>
        private (int X, int Y)? ToPixel(Point position)
        {
            if (_bitmap == null || MainImage.ActualWidth <= 0 || MainImage.ActualHeight <= 0)
            {
                return null;
            }

            int x = (int)Math.Floor(position.X * _bitmap.PixelWidth / MainImage.ActualWidth);
            int y = (int)Math.Floor(position.Y * _bitmap.PixelHeight / MainImage.ActualHeight);

            x = Math.Clamp(x, 0, _bitmap.PixelWidth - 1);
            y = Math.Clamp(y, 0, _bitmap.PixelHeight - 1);

            return (x, y);
        }

        /// <summary>
        /// Reads a single BGRA pixel from the working bitmap.
        /// </summary>
        /// <param name="x">The pixel column.</param>
        /// <param name="y">The pixel row.</param>
        private Color GetPixel(int x, int y)
        {
            var buffer = new byte[4];
            _bitmap!.CopyPixels(new Int32Rect(x, y, 1, 1), buffer, 4, 0);
            return Color.FromArgb(buffer[3], buffer[2], buffer[1], buffer[0]);
        }

        private void MainImage_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_bitmap == null)
            {
                return;
            }

            try
            {
                var pixel = this.ToPixel(e.GetPosition(MainImage));

                if (pixel == null)
                {
                    return;
                }

                // 25px of source magnified 4x fills the 100px loupe.
                int sourceSize = Math.Min(25, Math.Min(_bitmap.PixelWidth, _bitmap.PixelHeight));
                int cropX = Math.Clamp(pixel.Value.X - sourceSize / 2, 0, _bitmap.PixelWidth - sourceSize);
                int cropY = Math.Clamp(pixel.Value.Y - sourceSize / 2, 0, _bitmap.PixelHeight - sourceSize);

                LoupeImage.Source = new CroppedBitmap(_bitmap, new Int32Rect(cropX, cropY, sourceSize, sourceSize));

                var canvasPos = e.GetPosition(LoupeCanvas);
                Canvas.SetLeft(Loupe, canvasPos.X - 50);
                Canvas.SetTop(Loupe, canvasPos.Y - 50);
                Loupe.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                // The loupe is a nicety; hide it rather than fail on odd edge geometry.
                Loupe.Visibility = Visibility.Collapsed;
            }
        }

        private void MainImage_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Loupe.Visibility = Visibility.Collapsed;
        }

        private void MainImage_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_bitmap == null)
            {
                return;
            }

            try
            {
                var pixel = this.ToPixel(e.GetPosition(MainImage));

                if (pixel == null)
                {
                    return;
                }

                _selectedColor = this.GetPixel(pixel.Value.X, pixel.Value.Y);
                this.UpdateColorInfo(_selectedColor);
                SwapButton.IsEnabled = true;

                if (_shadesPanelOpen)
                {
                    this.GenerateShades();
                }
            }
            catch (Exception ex)
            {
                this.ShowToast($"Unable to read pixel: {ex.Message}", ToastSeverity.Error);
            }
        }

        /// <summary>
        /// Updates the footer preview and HEX/RGB/HSV readouts for the selected color.
        /// </summary>
        /// <param name="color">The selected color, or null to reset the readouts.</param>
        private void UpdateColorInfo(Color? color)
        {
            if (color == null)
            {
                ColorPreviewFill.Background = null;
                ColorNameText.Text = "Select a pixel";
                HexValueText.Text = "#------";
                RgbValueText.Text = "---, ---, ---";
                HsvValueText.Text = "---, ---, ---";
                return;
            }

            var c = color.Value;
            var hsv = ColorUtils.RgbToHsv(c.R, c.G, c.B);

            ColorPreviewFill.Background = new SolidColorBrush(c);
            ColorNameText.Text = c.A == 0 ? "Transparent" : $"R:{c.R} G:{c.G} B:{c.B}";
            HexValueText.Text = ColorUtils.ToHex(c);
            RgbValueText.Text = $"{c.R}, {c.G}, {c.B}";
            HsvValueText.Text = $"{Math.Round(hsv.H)}°, {Math.Round(hsv.S)}%, {Math.Round(hsv.V)}%";
        }

        private void CopyHex_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedColor != null)
            {
                this.CopyToClipboard(HexValueText.Text);
            }
        }

        private void CopyRgb_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedColor != null)
            {
                this.CopyToClipboard(RgbValueText.Text);
            }
        }

        private void CopyHsv_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedColor != null)
            {
                this.CopyToClipboard(HsvValueText.Text);
            }
        }

        #endregion

        #region Shades & tints panel

        private void ColorPreview_OnClick(object sender, RoutedEventArgs e)
        {
            if (_selectedColor == null)
            {
                this.ShowToast("Select a pixel first.", ToastSeverity.Info);
                return;
            }

            this.GenerateShades();
            this.SetShadesPanelOpen(true);
        }

        private void CloseShadesButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.SetShadesPanelOpen(false);
        }

        /// <summary>
        /// Slides the shades panel in or out.
        /// </summary>
        /// <param name="open">True to slide the panel in, false to slide it out.</param>
        private void SetShadesPanelOpen(bool open)
        {
            if (_shadesPanelOpen == open)
            {
                return;
            }

            _shadesPanelOpen = open;

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            if (open)
            {
                ShadesPanel.Visibility = Visibility.Visible;
                ShadesPanelTransform.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
            }
            else
            {
                var slideOut = new DoubleAnimation(-310, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease };
                slideOut.Completed += (_, _) =>
                {
                    if (!_shadesPanelOpen)
                    {
                        ShadesPanel.Visibility = Visibility.Collapsed;
                    }
                };
                ShadesPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            }
        }

        /// <summary>
        /// Builds the 21 entry scale for the selected color: ten tints toward white, the base
        /// color, then ten shades toward black (mirrors the original ChromaSwap scale).
        /// </summary>
        private void GenerateShades()
        {
            _shades.Clear();

            if (_selectedColor == null)
            {
                return;
            }

            var c = _selectedColor.Value;
            var normalForeground = (Brush)this.FindResource(MosaicTheme.ControlTextForegroundBrush);
            var accentForeground = (Brush)this.FindResource(MosaicTheme.AccentBrush);

            void Add(byte r, byte g, byte b, string cssName, string label, bool isBase)
            {
                var color = Color.FromRgb(r, g, b);
                var brush = new SolidColorBrush(color);
                brush.Freeze();

                _shades.Add(new ShadeSwatch
                {
                    Name = label,
                    Hex = ColorUtils.ToHex(color),
                    CssName = cssName,
                    Brush = brush,
                    IsBase = isBase,
                    HexForeground = isBase ? accentForeground : normalForeground
                });
            }

            // Tints: lightest (shade-50) at the top down to shade-500.
            for (int i = 10; i >= 1; i--)
            {
                double weight = i / 12.0;
                int scale = (11 - i) * 50;
                Add(ColorUtils.Mix(c.R, 255, weight), ColorUtils.Mix(c.G, 255, weight), ColorUtils.Mix(c.B, 255, weight),
                    $"shade-{scale}", $"shade-{scale}", false);
            }

            Add(c.R, c.G, c.B, "shade-550", "shade-550 (Base)", true);

            // Shades: shade-600 down to the darkest (shade-1050).
            for (int i = 1; i <= 10; i++)
            {
                double weight = i / 12.0;
                int scale = 550 + i * 50;
                Add(ColorUtils.Mix(c.R, 0, weight), ColorUtils.Mix(c.G, 0, weight), ColorUtils.Mix(c.B, 0, weight),
                    $"shade-{scale}", $"shade-{scale}", false);
            }
        }

        private void Swatch_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ShadeSwatch swatch })
            {
                this.CopyToClipboard(swatch.Hex);
            }
        }

        private void ExportCssButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_shades.Count == 0)
            {
                this.ShowToast("Nothing to export yet.", ToastSeverity.Info);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Export CSS Variables",
                    FileName = "chromaswap-palette.css",
                    Filter = "CSS Files|*.css|All Files|*.*"
                };

                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, this.BuildCssExport());

                this.ShowToast("CSS exported!");
            }
            catch (Exception ex)
            {
                this.ShowToast($"Export failed: {ex.Message}", ToastSeverity.Error);
            }
        }

        private async void ExportResourceDictionaryButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_shades.Count == 0)
            {
                this.ShowToast("Nothing to export yet.", ToastSeverity.Info);
                return;
            }

            try
            {
                string? prefix = await this.ShowResourcePrefixDialogAsync();

                if (prefix == null)
                {
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Export ResourceDictionary",
                    FileName = "chromaswap-palette.xaml",
                    Filter = "XAML Resource Dictionary|*.xaml|All Files|*.*"
                };

                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, this.BuildResourceDictionaryExport(prefix));

                this.ShowToast("ResourceDictionary exported!");
            }
            catch (Exception ex)
            {
                this.ShowToast($"Export failed: {ex.Message}", ToastSeverity.Error);
            }
        }

        private string BuildCssExport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("/* Generated by ChromaSwap */");
            sb.AppendLine(":root {");

            foreach (var shade in _shades)
            {
                sb.Append("  --").Append(shade.CssName).Append(": ").Append(shade.Hex).Append(';');
                sb.AppendLine(shade.IsBase ? " /* Base Color */" : string.Empty);
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private async Task<string?> ShowResourcePrefixDialogAsync()
        {
            var prefixBox = new TextBox
            {
                MinWidth = 280,
                Margin = new Thickness(0, 4, 0, 14)
            };

            var previewText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 14),
                FontSize = 12
            };
            previewText.SetResourceReference(ForegroundProperty, MosaicTheme.ControlTextSecondaryForegroundBrush);

            void UpdatePreview()
            {
                previewText.Text = $"Example: {BuildPrefixedResourceKey(prefixBox.Text, "shade-400")}";
            }

            prefixBox.TextChanged += (_, _) => UpdatePreview();

            var exportButton = new Button
            {
                Content = "Export",
                IsDefault = true,
                MinWidth = 80,
                MinHeight = 25,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                IsCancel = true,
                MinWidth = 80
            };

            var buttons = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Orientation = Orientation.Horizontal
            };
            buttons.Children.Add(exportButton);
            buttons.Children.Add(cancelButton);

            var content = new StackPanel
            {
                Margin = new Thickness(4)
            };
            content.Children.Add(new TextBlock { Text = "Prefix" });
            content.Children.Add(prefixBox);
            content.Children.Add(previewText);
            content.Children.Add(buttons);

            var dialog = new ModalDialog
            {
                Title = "ResourceDictionary Prefix",
                Description = "Enter an optional resource key prefix.",
                Content = content,
                CloseOnBackdropClick = false,
                CloseOnEscape = true
            };

            string result = string.Empty;
            exportButton.Click += (_, _) =>
            {
                result = prefixBox.Text.Trim();
                dialog.Close(true);
            };

            cancelButton.Click += (_, _) => dialog.Close(null);

            dialog.Opened += (_, _) =>
            {
                UpdatePreview();
                Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
                {
                    prefixBox.Focus();
                    prefixBox.SelectAll();
                });
            };

            return await dialog.ShowAsync(RootGrid) == true ? result : null;
        }

        private string BuildResourceDictionaryExport(string prefix)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<ResourceDictionary");
            sb.AppendLine("    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"");
            sb.AppendLine("    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
            sb.AppendLine("    <!-- Generated by ChromaSwap -->");

            foreach (var shade in _shades)
            {
                string key = EscapeXamlAttribute(BuildPrefixedResourceKey(prefix, shade.CssName));
                sb.Append("    <SolidColorBrush x:Key=\"").Append(key).Append("\" Color=\"").Append(shade.Hex).Append("\" />");
                sb.AppendLine(shade.IsBase ? " <!-- Base Color -->" : string.Empty);
            }

            sb.AppendLine("</ResourceDictionary>");
            return sb.ToString();
        }

        private static string BuildPrefixedResourceKey(string prefix, string resourceKey)
        {
            string normalizedPrefix = prefix.Trim().Trim('-');
            return string.IsNullOrWhiteSpace(normalizedPrefix) ? resourceKey : $"{normalizedPrefix}-{resourceKey}";
        }

        private static string EscapeXamlAttribute(string value)
        {
            return SecurityElement.Escape(value) ?? string.Empty;
        }

        #endregion

        #region Swap

        private async void SwapButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_bitmap == null || _selectedColor == null || _isSwapping)
            {
                return;
            }

            try
            {
                var view = new SwapColorsView(_selectedColor.Value, Color.FromRgb(0x3B, 0x82, 0xF6));

                var dialog = new ModalDialog
                {
                    Title = "Swap Colors",
                    Description = "Replace the selected color globally.",
                    Content = view,
                    CloseOnBackdropClick = true
                };

                bool? result = await dialog.ShowAsync(RootGrid);

                if (result == true)
                {
                    await this.ExecuteSwapAsync(view.TargetColor, view.Tolerance, view.MatchShading);
                }
            }
            catch (Exception ex)
            {
                this.ShowToast($"Swap failed: {ex.Message}", ToastSeverity.Error);
            }
        }

        /// <summary>
        /// Replaces every pixel within the tolerance distance of the selected color.  With match
        /// shading enabled the pixel's saturation/value offsets from the source are re-applied to
        /// the target color so gradients survive the swap.
        /// </summary>
        /// <param name="target">The replacement color.</param>
        /// <param name="tolerance">Tolerance percent (0-100) mapped onto RGB euclidean distance.</param>
        /// <param name="matchShading">Whether to preserve gradients/shading.</param>
        private async Task ExecuteSwapAsync(Color target, int tolerance, bool matchShading)
        {
            if (_bitmap == null || _selectedColor == null)
            {
                return;
            }

            _isSwapping = true;
            SwapButton.IsEnabled = false;
            UndoButton.IsEnabled = false;

            try
            {
                var source = _selectedColor.Value;
                int width = _bitmap.PixelWidth;
                int height = _bitmap.PixelHeight;
                int stride = width * 4;

                var pixels = new byte[height * stride];
                _bitmap.CopyPixels(pixels, stride, 0);

                int count = await Task.Run(() =>
                {
                    // Max RGB distance is ~442 (black to white); tolerance % maps onto it.
                    double threshold = tolerance / 100.0 * 442.0;
                    double thresholdSq = threshold * threshold;

                    var sourceHsv = ColorUtils.RgbToHsv(source.R, source.G, source.B);
                    var targetHsv = ColorUtils.RgbToHsv(target.R, target.G, target.B);

                    int swapped = 0;

                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        byte b = pixels[i];
                        byte g = pixels[i + 1];
                        byte r = pixels[i + 2];

                        double dr = r - source.R;
                        double dg = g - source.G;
                        double db = b - source.B;
                        double distSq = dr * dr + dg * dg + db * db;

                        if (distSq > thresholdSq)
                        {
                            continue;
                        }

                        if (matchShading)
                        {
                            // Keep the target hue but re-apply this pixel's shading offsets.
                            var pixelHsv = ColorUtils.RgbToHsv(r, g, b);
                            var (nr, ng, nb) = ColorUtils.HsvToRgb(
                                targetHsv.H,
                                targetHsv.S + (pixelHsv.S - sourceHsv.S),
                                targetHsv.V + (pixelHsv.V - sourceHsv.V));

                            pixels[i] = nb;
                            pixels[i + 1] = ng;
                            pixels[i + 2] = nr;
                        }
                        else
                        {
                            pixels[i] = target.B;
                            pixels[i + 1] = target.G;
                            pixels[i + 2] = target.R;
                        }

                        swapped++;
                    }

                    return swapped;
                });

                _bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                this.PushHistory(pixels);

                this.ShowToast($"Swapped {count:N0} pixel{(count == 1 ? "" : "s")}.");
            }
            catch (Exception ex)
            {
                this.ShowToast($"Swap failed: {ex.Message}", ToastSeverity.Error);
            }
            finally
            {
                _isSwapping = false;
                SwapButton.IsEnabled = _selectedColor != null;
                UndoButton.IsEnabled = _historyIndex > 0;
            }
        }

        #endregion

        #region History / save

        /// <summary>
        /// Pushes a pixel snapshot onto the undo history, trimming any forward history and
        /// capping memory use.
        /// </summary>
        /// <param name="pixels">The BGRA snapshot after an edit.</param>
        private void PushHistory(byte[] pixels)
        {
            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            }

            _history.Add(pixels);
            _historyIndex = _history.Count - 1;

            while (_history.Count > MaxHistoryEntries)
            {
                _history.RemoveAt(0);
                _historyIndex--;
            }

            UndoButton.IsEnabled = _historyIndex > 0;
        }

        private void UndoButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Undo();
        }

        /// <summary>
        /// Steps back one entry in the edit history.
        /// </summary>
        private void Undo()
        {
            if (_bitmap == null || _historyIndex <= 0)
            {
                return;
            }

            try
            {
                _historyIndex--;
                var pixels = _history[_historyIndex];
                _bitmap.WritePixels(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight), pixels, _bitmap.PixelWidth * 4, 0);

                UndoButton.IsEnabled = _historyIndex > 0;
                this.ShowToast("Undo successful");
            }
            catch (Exception ex)
            {
                this.ShowToast($"Undo failed: {ex.Message}", ToastSeverity.Error);
            }
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_bitmap == null)
            {
                return;
            }

            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Image",
                    FileName = $"{_fileName}-edited.png",
                    Filter = "PNG Image|*.png"
                };

                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_bitmap));

                using var stream = File.Create(dialog.FileName);
                encoder.Save(stream);

                this.ShowToast("Image saved.");
            }
            catch (Exception ex)
            {
                this.ShowToast($"Save failed: {ex.Message}", ToastSeverity.Error);
            }
        }

        #endregion
    }
}
