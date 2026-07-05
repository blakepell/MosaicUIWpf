/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable InconsistentNaming

using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Mosaic.UI.Wpf.Cache;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A radial (HSV wheel) color picker. Colors are selected by clicking the hue ring that runs
    /// the full gamut around the perimeter; the inner square shows every shade of the selected hue
    /// for fine tuning. A side panel exposes the hex value (with a copy button) plus editable
    /// red, green, blue, and alpha (transparency) channels. The control is Mosaic theme aware.
    /// </summary>
    [TemplatePart(Name = PartHueImage, Type = typeof(Image))]
    [TemplatePart(Name = PartSVImage, Type = typeof(Image))]
    [TemplatePart(Name = PartSVBorder, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartWheelCanvas, Type = typeof(Canvas))]
    [TemplatePart(Name = PartHueThumb, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartSVThumb, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartHexTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartCopyButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartRedTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartGreenTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartBlueTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartAlphaTextBox, Type = typeof(TextBox))]
    [DefaultProperty(nameof(SelectedColor))]
    [DefaultEvent(nameof(ColorChanged))]
    public class RadialColorPicker : Control
    {
        #region Template Part Names

        private const string PartHueImage = "PART_HueImage";
        private const string PartSVImage = "PART_SVImage";
        private const string PartSVBorder = "PART_SVBorder";
        private const string PartWheelCanvas = "PART_WheelCanvas";
        private const string PartHueThumb = "PART_HueThumb";
        private const string PartSVThumb = "PART_SVThumb";
        private const string PartHexTextBox = "PART_HexTextBox";
        private const string PartCopyButton = "PART_CopyButton";
        private const string PartRedTextBox = "PART_RedTextBox";
        private const string PartGreenTextBox = "PART_GreenTextBox";
        private const string PartBlueTextBox = "PART_BlueTextBox";
        private const string PartAlphaTextBox = "PART_AlphaTextBox";

        #endregion

        #region Private Fields

        private Image? _hueImage;
        private Image? _svImage;
        private FrameworkElement? _svBorder;
        private Canvas? _wheelCanvas;
        private FrameworkElement? _hueThumb;
        private FrameworkElement? _svThumb;
        private TextBox? _hexTextBox;
        private ButtonBase? _copyButton;
        private TextBox? _redTextBox;
        private TextBox? _greenTextBox;
        private TextBox? _blueTextBox;
        private TextBox? _alphaTextBox;

        // HSV state is the authoritative source of truth for the wheel so that hue is preserved even
        // when saturation or value collapse to a gray (which would otherwise lose the hue).
        private double _hue;          // 0..360
        private double _saturation;   // 0..1
        private double _value;        // 0..1
        private byte _alpha = 255;    // 0..255

        private WriteableBitmap? _hueBitmap;
        private WriteableBitmap? _svBitmap;

        private bool _draggingHue;
        private bool _draggingSV;
        private bool _isUpdating;
        private bool _templateApplied;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SelectedColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            nameof(SelectedColor), typeof(Color), typeof(RadialColorPicker),
            new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        /// <summary>
        /// Gets or sets the currently selected color (including its alpha channel).
        /// </summary>
        [Category("Mosaic")]
        [Description("The currently selected color, including the alpha (transparency) channel.")]
        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(
            nameof(SelectedBrush), typeof(Brush), typeof(RadialColorPicker),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedBrushChanged));

        /// <summary>
        /// Gets or sets a <see cref="SolidColorBrush"/> representation of the selected color.
        /// </summary>
        [Category("Mosaic")]
        [Description("A SolidColorBrush representation of the selected color.")]
        public Brush SelectedBrush
        {
            get => (Brush)GetValue(SelectedBrushProperty);
            set => SetValue(SelectedBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HexValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HexValueProperty = DependencyProperty.Register(
            nameof(HexValue), typeof(string), typeof(RadialColorPicker),
            new FrameworkPropertyMetadata("#FFFF0000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHexValueChanged));

        /// <summary>
        /// Gets or sets the hex string (normalized to #AARRGGBB) of the selected color.
        /// </summary>
        [Category("Mosaic")]
        [Description("The hex string (#AARRGGBB) of the selected color.")]
        public string HexValue
        {
            get => (string)GetValue(HexValueProperty);
            set => SetValue(HexValueProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="WheelDiameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WheelDiameterProperty = DependencyProperty.Register(
            nameof(WheelDiameter), typeof(double), typeof(RadialColorPicker),
            new FrameworkPropertyMetadata(220.0, OnWheelDiameterChanged));

        /// <summary>
        /// Gets or sets the diameter, in device-independent pixels, of the radial hue wheel.
        /// </summary>
        [Category("Mosaic")]
        [Description("The diameter, in pixels, of the radial hue wheel.")]
        public double WheelDiameter
        {
            get => (double)GetValue(WheelDiameterProperty);
            set => SetValue(WheelDiameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RingThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RingThicknessProperty = DependencyProperty.Register(
            nameof(RingThickness), typeof(double), typeof(RadialColorPicker),
            new FrameworkPropertyMetadata(24.0, OnWheelDiameterChanged));

        /// <summary>
        /// Gets or sets the thickness, in device-independent pixels, of the hue ring.
        /// </summary>
        [Category("Mosaic")]
        [Description("The thickness, in pixels, of the hue ring.")]
        public double RingThickness
        {
            get => (double)GetValue(RingThicknessProperty);
            set => SetValue(RingThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowAlpha"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
            nameof(ShowAlpha), typeof(bool), typeof(RadialColorPicker),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the alpha (transparency) editor is shown.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the alpha (transparency) editor is shown.")]
        public bool ShowAlpha
        {
            get => (bool)GetValue(ShowAlphaProperty);
            set => SetValue(ShowAlphaProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(RadialColorPicker),
            new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the control's outer border.
        /// </summary>
        [Category("Mosaic")]
        [Description("The corner radius of the control's outer border.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the selected color changes.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        #endregion

        static RadialColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RadialColorPicker),
                new FrameworkPropertyMetadata(typeof(RadialColorPicker)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RadialColorPicker"/> class.
        /// </summary>
        public RadialColorPicker()
        {
            var (h, s, v) = RgbToHsv(SelectedColor);
            _hue = h;
            _saturation = s;
            _value = v;
            _alpha = SelectedColor.A;
        }

        #region Property Change Handlers

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RadialColorPicker picker || picker._isUpdating)
            {
                return;
            }

            if (e.NewValue is Color color)
            {
                picker._isUpdating = true;

                var (h, s, v) = RgbToHsv(color);

                // Preserve the current hue when the incoming color is a pure gray (saturation of 0),
                // otherwise the hue thumb would snap back to red every time.
                if (s > 0)
                {
                    picker._hue = h;
                }

                picker._saturation = s;
                picker._value = v;
                picker._alpha = color.A;

                picker.SelectedBrush = ColorPaletteCache.GetBrush(color);
                picker.HexValue = ColorToHex(color);

                picker._isUpdating = false;

                picker.RefreshVisuals(rebuildSV: true);
                picker.OnColorChanged();
            }
        }

        private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadialColorPicker picker && !picker._isUpdating && e.NewValue is SolidColorBrush brush)
            {
                picker.SelectedColor = brush.Color;
            }
        }

        private static void OnHexValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RadialColorPicker picker || picker._isUpdating)
            {
                return;
            }

            if (e.NewValue is string hex && TryParseHexColor(hex, out var color))
            {
                picker.SelectedColor = color;
            }
        }

        private static void OnWheelDiameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadialColorPicker picker && picker._templateApplied)
            {
                picker.BuildBitmaps();
                picker.RefreshVisuals(rebuildSV: true);
            }
        }

        #endregion

        #region Template

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Detach any previously wired handlers before re-acquiring the template parts.
            DetachHandlers();

            base.OnApplyTemplate();

            _hueImage = GetTemplateChild(PartHueImage) as Image;
            _svImage = GetTemplateChild(PartSVImage) as Image;
            _svBorder = GetTemplateChild(PartSVBorder) as FrameworkElement;
            _wheelCanvas = GetTemplateChild(PartWheelCanvas) as Canvas;
            _hueThumb = GetTemplateChild(PartHueThumb) as FrameworkElement;
            _svThumb = GetTemplateChild(PartSVThumb) as FrameworkElement;
            _hexTextBox = GetTemplateChild(PartHexTextBox) as TextBox;
            _copyButton = GetTemplateChild(PartCopyButton) as ButtonBase;
            _redTextBox = GetTemplateChild(PartRedTextBox) as TextBox;
            _greenTextBox = GetTemplateChild(PartGreenTextBox) as TextBox;
            _blueTextBox = GetTemplateChild(PartBlueTextBox) as TextBox;
            _alphaTextBox = GetTemplateChild(PartAlphaTextBox) as TextBox;

            if (_hueImage != null)
            {
                _hueImage.MouseLeftButtonDown += HueImage_MouseLeftButtonDown;
                _hueImage.MouseMove += HueImage_MouseMove;
                _hueImage.MouseLeftButtonUp += Wheel_MouseLeftButtonUp;
            }

            if (_svBorder != null)
            {
                _svBorder.MouseLeftButtonDown += SVBorder_MouseLeftButtonDown;
                _svBorder.MouseMove += SVBorder_MouseMove;
                _svBorder.MouseLeftButtonUp += Wheel_MouseLeftButtonUp;
            }

            if (_copyButton != null)
            {
                _copyButton.Click += CopyButton_Click;
            }

            WireChannelTextBox(_hexTextBox, isHex: true);
            WireChannelTextBox(_redTextBox, isHex: false);
            WireChannelTextBox(_greenTextBox, isHex: false);
            WireChannelTextBox(_blueTextBox, isHex: false);
            WireChannelTextBox(_alphaTextBox, isHex: false);

            _templateApplied = true;

            BuildBitmaps();
            RefreshVisuals(rebuildSV: true);
        }

        private void WireChannelTextBox(TextBox? textBox, bool isHex)
        {
            if (textBox == null)
            {
                return;
            }

            textBox.LostFocus += ChannelTextBox_LostFocus;
            textBox.KeyDown += ChannelTextBox_KeyDown;
        }

        private void DetachHandlers()
        {
            if (_hueImage != null)
            {
                _hueImage.MouseLeftButtonDown -= HueImage_MouseLeftButtonDown;
                _hueImage.MouseMove -= HueImage_MouseMove;
                _hueImage.MouseLeftButtonUp -= Wheel_MouseLeftButtonUp;
            }

            if (_svBorder != null)
            {
                _svBorder.MouseLeftButtonDown -= SVBorder_MouseLeftButtonDown;
                _svBorder.MouseMove -= SVBorder_MouseMove;
                _svBorder.MouseLeftButtonUp -= Wheel_MouseLeftButtonUp;
            }

            if (_copyButton != null)
            {
                _copyButton.Click -= CopyButton_Click;
            }

            foreach (var textBox in new[] { _hexTextBox, _redTextBox, _greenTextBox, _blueTextBox, _alphaTextBox })
            {
                if (textBox != null)
                {
                    textBox.LostFocus -= ChannelTextBox_LostFocus;
                    textBox.KeyDown -= ChannelTextBox_KeyDown;
                }
            }
        }

        #endregion

        #region Geometry Helpers

        private double Diameter => Math.Max(40, WheelDiameter);

        private double OuterRadius => Diameter / 2.0;

        private double InnerRadius => Math.Max(10, OuterRadius - Math.Max(6, RingThickness));

        // Side length of the saturation/value square inscribed within the inner ring circle.
        private double SquareSide => InnerRadius * 2.0 / Math.Sqrt(2.0) * 0.94;

        #endregion

        #region Bitmap Rendering

        private void BuildBitmaps()
        {
            int size = (int)Math.Round(Diameter);

            if (size < 8)
            {
                return;
            }

            double cx = size / 2.0;
            double cy = size / 2.0;
            double outer = OuterRadius;
            double inner = InnerRadius;

            // Hue ring bitmap (only depends on geometry, so it is safe to cache across hue changes).
            _hueBitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
            var hueBuffer = new int[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x + 0.5 - cx;
                    double dy = y + 0.5 - cy;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    int index = y * size + x;

                    if (dist >= inner && dist <= outer)
                    {
                        double angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;
                        if (angle < 0)
                        {
                            angle += 360;
                        }

                        var color = HsvToRgb(angle, 1.0, 1.0, 255);

                        // Simple 1px anti-aliasing on the inner and outer edges.
                        double edge = Math.Min(dist - inner, outer - dist);
                        byte a = edge < 1.0 ? (byte)(Math.Clamp(edge, 0, 1) * 255) : (byte)255;
                        hueBuffer[index] = (a << 24) | (color.R << 16) | (color.G << 8) | color.B;
                    }
                    else
                    {
                        hueBuffer[index] = 0;
                    }
                }
            }

            _hueBitmap.WritePixels(new Int32Rect(0, 0, size, size), hueBuffer, size * 4, 0);

            if (_hueImage != null)
            {
                _hueImage.Source = _hueBitmap;
                _hueImage.Width = size;
                _hueImage.Height = size;
            }

            if (_wheelCanvas != null)
            {
                _wheelCanvas.Width = size;
                _wheelCanvas.Height = size;
            }

            BuildSVBitmap();
        }

        private void BuildSVBitmap()
        {
            int side = (int)Math.Round(SquareSide);

            if (side < 4)
            {
                return;
            }

            _svBitmap = new WriteableBitmap(side, side, 96, 96, PixelFormats.Bgra32, null);
            var buffer = new int[side * side];

            for (int y = 0; y < side; y++)
            {
                double val = 1.0 - (double)y / (side - 1);

                for (int x = 0; x < side; x++)
                {
                    double sat = (double)x / (side - 1);
                    var color = HsvToRgb(_hue, sat, val, 255);
                    buffer[y * side + x] = unchecked((int)0xFF000000) | (color.R << 16) | (color.G << 8) | color.B;
                }
            }

            _svBitmap.WritePixels(new Int32Rect(0, 0, side, side), buffer, side * 4, 0);

            if (_svBorder != null)
            {
                _svBorder.Width = side;
                _svBorder.Height = side;

                double cx = Diameter / 2.0;
                double cy = Diameter / 2.0;
                Canvas.SetLeft(_svBorder, cx - side / 2.0);
                Canvas.SetTop(_svBorder, cy - side / 2.0);
            }

            if (_svImage != null)
            {
                _svImage.Source = _svBitmap;
            }
        }

        #endregion

        #region Visual Refresh

        private void RefreshVisuals(bool rebuildSV)
        {
            if (!_templateApplied)
            {
                return;
            }

            if (rebuildSV)
            {
                BuildSVBitmap();
            }

            UpdateThumbs();
            UpdateTextBoxes();
        }

        private void UpdateThumbs()
        {
            double cx = Diameter / 2.0;
            double cy = Diameter / 2.0;

            // Hue thumb sits on the mid-line of the ring.
            if (_hueThumb != null)
            {
                double midR = (InnerRadius + OuterRadius) / 2.0;
                double rad = _hue * Math.PI / 180.0;
                double hx = cx + midR * Math.Cos(rad);
                double hy = cy - midR * Math.Sin(rad);
                Canvas.SetLeft(_hueThumb, hx - _hueThumb.Width / 2.0);
                Canvas.SetTop(_hueThumb, hy - _hueThumb.Height / 2.0);
            }

            // Saturation/value thumb sits inside the inscribed square.
            if (_svThumb != null)
            {
                double side = SquareSide;
                double sqX = cx - side / 2.0;
                double sqY = cy - side / 2.0;
                double sx = sqX + _saturation * side;
                double sy = sqY + (1.0 - _value) * side;
                Canvas.SetLeft(_svThumb, sx - _svThumb.Width / 2.0);
                Canvas.SetTop(_svThumb, sy - _svThumb.Height / 2.0);
            }
        }

        private void UpdateTextBoxes()
        {
            var color = SelectedColor;

            SetTextBoxText(_hexTextBox, ColorToHex(color));
            SetTextBoxText(_redTextBox, color.R.ToString(CultureInfo.InvariantCulture));
            SetTextBoxText(_greenTextBox, color.G.ToString(CultureInfo.InvariantCulture));
            SetTextBoxText(_blueTextBox, color.B.ToString(CultureInfo.InvariantCulture));
            SetTextBoxText(_alphaTextBox, color.A.ToString(CultureInfo.InvariantCulture));
        }

        private static void SetTextBoxText(TextBox? textBox, string text)
        {
            if (textBox != null && !textBox.IsKeyboardFocused && textBox.Text != text)
            {
                textBox.Text = text;
            }
        }

        #endregion

        #region Mouse Interaction

        private void HueImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggingHue = true;
            _hueImage?.CaptureMouse();
            UpdateHueFromPoint(e.GetPosition((IInputElement?)_wheelCanvas ?? _hueImage));
            e.Handled = true;
        }

        private void HueImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingHue && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateHueFromPoint(e.GetPosition((IInputElement?)_wheelCanvas ?? _hueImage));
            }
        }

        private void SVBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggingSV = true;
            _svBorder?.CaptureMouse();
            UpdateSVFromPoint(e.GetPosition(_svBorder));
            e.Handled = true;
        }

        private void SVBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggingSV && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSVFromPoint(e.GetPosition(_svBorder));
            }
        }

        private void Wheel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingHue)
            {
                _draggingHue = false;
                _hueImage?.ReleaseMouseCapture();
            }

            if (_draggingSV)
            {
                _draggingSV = false;
                _svBorder?.ReleaseMouseCapture();
            }
        }

        private void UpdateHueFromPoint(Point p)
        {
            double cx = Diameter / 2.0;
            double cy = Diameter / 2.0;
            double dx = p.X - cx;
            double dy = p.Y - cy;

            double angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;
            if (angle < 0)
            {
                angle += 360;
            }

            _hue = angle;
            CommitHsv(rebuildSV: true);
        }

        private void UpdateSVFromPoint(Point p)
        {
            double side = SquareSide;
            if (side <= 0)
            {
                return;
            }

            _saturation = Math.Clamp(p.X / side, 0.0, 1.0);
            _value = Math.Clamp(1.0 - p.Y / side, 0.0, 1.0);
            CommitHsv(rebuildSV: false);
        }

        #endregion

        #region Channel Editing

        private void ChannelTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                CommitChannelTextBox(textBox);
                e.Handled = true;
            }
        }

        private void ChannelTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                CommitChannelTextBox(textBox);
            }
        }

        private void CommitChannelTextBox(TextBox textBox)
        {
            if (ReferenceEquals(textBox, _hexTextBox))
            {
                if (TryParseHexColor(textBox.Text, out var color))
                {
                    SelectedColor = color;
                }
                else
                {
                    UpdateTextBoxes();
                }

                return;
            }

            var current = SelectedColor;
            byte r = current.R, g = current.G, b = current.B, a = current.A;

            if (byte.TryParse(textBox.Text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte channel))
            {
                if (ReferenceEquals(textBox, _redTextBox)) r = channel;
                else if (ReferenceEquals(textBox, _greenTextBox)) g = channel;
                else if (ReferenceEquals(textBox, _blueTextBox)) b = channel;
                else if (ReferenceEquals(textBox, _alphaTextBox)) a = channel;

                SelectedColor = Color.FromArgb(a, r, g, b);
            }
            else
            {
                // Invalid input: revert to the current authoritative value.
                UpdateTextBoxes();
            }
        }

        #endregion

        #region Copy

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ColorToHex(SelectedColor));
            }
            catch
            {
                // The clipboard can occasionally be locked by another process; ignore.
            }
        }

        #endregion

        #region Commit / Conversions

        /// <summary>
        /// Pushes the authoritative HSV/alpha state out to <see cref="SelectedColor"/> and refreshes the UI.
        /// </summary>
        private void CommitHsv(bool rebuildSV)
        {
            var color = HsvToRgb(_hue, _saturation, _value, _alpha);

            _isUpdating = true;
            SelectedColor = color;
            SelectedBrush = ColorPaletteCache.GetBrush(color);
            HexValue = ColorToHex(color);
            _isUpdating = false;

            RefreshVisuals(rebuildSV);
            OnColorChanged();
        }

        private void OnColorChanged()
        {
            ColorChanged?.Invoke(this, new ColorChangedEventArgs(SelectedColor, SelectedBrush, HexValue));
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static bool TryParseHexColor(string? hex, out Color color)
        {
            color = Colors.Black;

            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            hex = hex.Trim();

            if (!hex.StartsWith('#'))
            {
                hex = "#" + hex;
            }

            string body = hex.Substring(1);

            if (body.Length == 3)
            {
                body = "FF" + new string(body[0], 2) + new string(body[1], 2) + new string(body[2], 2);
            }
            else if (body.Length == 6)
            {
                body = "FF" + body;
            }
            else if (body.Length != 8)
            {
                return false;
            }

            if (uint.TryParse(body, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
            {
                byte a = (byte)((argb & 0xFF000000) >> 24);
                byte r = (byte)((argb & 0x00FF0000) >> 16);
                byte g = (byte)((argb & 0x0000FF00) >> 8);
                byte b = (byte)(argb & 0x000000FF);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }

            return false;
        }

        private static (double h, double s, double v) RgbToHsv(Color c)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0)
            {
                if (max == r)
                {
                    h = 60 * (((g - b) / delta) % 6);
                }
                else if (max == g)
                {
                    h = 60 * (((b - r) / delta) + 2);
                }
                else
                {
                    h = 60 * (((r - g) / delta) + 4);
                }
            }

            if (h < 0)
            {
                h += 360;
            }

            double s = max <= 0 ? 0 : delta / max;
            double v = max;

            return (h, s, v);
        }

        private static Color HsvToRgb(double h, double s, double v, byte a)
        {
            h = ((h % 360) + 360) % 360;
            s = Math.Clamp(s, 0, 1);
            v = Math.Clamp(v, 0, 1);

            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60.0 % 2) - 1));
            double m = v - c;

            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromArgb(
                a,
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        #endregion
    }
}
