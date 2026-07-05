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
using Mosaic.UI.Wpf.Cache;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A compact color field that shows a swatch and an editable hex text box, with a drop-down
    /// button that opens a <see cref="RadialColorPicker"/> for full HSV selection. The hex value can
    /// be typed directly to set the color, or picked from the wheel in the drop-down. Mosaic theme aware.
    /// </summary>
    [TemplatePart(Name = PartHexTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartDropDownToggle, Type = typeof(ToggleButton))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartPicker, Type = typeof(RadialColorPicker))]
    [DefaultProperty(nameof(SelectedColor))]
    [DefaultEvent(nameof(ColorChanged))]
    public class RadialColorPickerBox : Control
    {
        #region Template Part Names

        private const string PartHexTextBox = "PART_HexTextBox";
        private const string PartDropDownToggle = "PART_DropDownToggle";
        private const string PartPopup = "PART_Popup";
        private const string PartPicker = "PART_Picker";

        #endregion

        #region Private Fields

        private TextBox? _hexTextBox;
        private ToggleButton? _dropDownToggle;
        private Popup? _popup;
        private RadialColorPicker? _picker;
        private bool _isUpdating;
        private string _previousValidHex = "#FF000000";

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SelectedColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            nameof(SelectedColor), typeof(Color), typeof(RadialColorPickerBox),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

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
            nameof(SelectedBrush), typeof(Brush), typeof(RadialColorPickerBox),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedBrushChanged));

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
            nameof(HexValue), typeof(string), typeof(RadialColorPickerBox),
            new FrameworkPropertyMetadata("#FF000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHexValueChanged));

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
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            nameof(IsDropDownOpen), typeof(bool), typeof(RadialColorPickerBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Gets or sets a value indicating whether the color picker drop-down is open.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the color picker drop-down is open.")]
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowAlpha"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
            nameof(ShowAlpha), typeof(bool), typeof(RadialColorPickerBox),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the alpha (transparency) editor is shown in the drop-down picker.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the alpha (transparency) editor is shown in the drop-down picker.")]
        public bool ShowAlpha
        {
            get => (bool)GetValue(ShowAlphaProperty);
            set => SetValue(ShowAlphaProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="WheelDiameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WheelDiameterProperty = DependencyProperty.Register(
            nameof(WheelDiameter), typeof(double), typeof(RadialColorPickerBox),
            new FrameworkPropertyMetadata(200.0));

        /// <summary>
        /// Gets or sets the diameter, in pixels, of the radial hue wheel shown in the drop-down.
        /// </summary>
        [Category("Mosaic")]
        [Description("The diameter, in pixels, of the radial hue wheel shown in the drop-down.")]
        public double WheelDiameter
        {
            get => (double)GetValue(WheelDiameterProperty);
            set => SetValue(WheelDiameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(RadialColorPickerBox),
            new FrameworkPropertyMetadata(new CornerRadius(2)));

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

        static RadialColorPickerBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RadialColorPickerBox),
                new FrameworkPropertyMetadata(typeof(RadialColorPickerBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RadialColorPickerBox"/> class.
        /// </summary>
        public RadialColorPickerBox()
        {
            _previousValidHex = HexValue;
        }

        #region Property Change Handlers

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RadialColorPickerBox box || e.NewValue is not Color color)
            {
                return;
            }

            box.SelectedBrush = ColorPaletteCache.GetBrush(color);

            if (!box._isUpdating)
            {
                box._isUpdating = true;
                box.HexValue = ColorToHex(color);
                box._isUpdating = false;
            }

            box._previousValidHex = ColorToHex(color);
            box.UpdateHexTextBox();
            box.OnColorChanged();
        }

        private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadialColorPickerBox box && !box._isUpdating && e.NewValue is SolidColorBrush brush)
            {
                box.SelectedColor = brush.Color;
            }
        }

        private static void OnHexValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RadialColorPickerBox box || box._isUpdating)
            {
                return;
            }

            if (e.NewValue is string hex && TryParseHexColor(hex, out var color))
            {
                box._isUpdating = true;
                box.SelectedColor = color;
                box._isUpdating = false;

                box.SelectedBrush = ColorPaletteCache.GetBrush(color);
                box._previousValidHex = ColorToHex(color);
                box.UpdateHexTextBox();
                box.OnColorChanged();
            }
        }

        #endregion

        #region Template

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            DetachHandlers();

            base.OnApplyTemplate();

            _hexTextBox = GetTemplateChild(PartHexTextBox) as TextBox;
            _dropDownToggle = GetTemplateChild(PartDropDownToggle) as ToggleButton;
            _popup = GetTemplateChild(PartPopup) as Popup;
            _picker = GetTemplateChild(PartPicker) as RadialColorPicker;

            if (_hexTextBox != null)
            {
                _hexTextBox.LostFocus += HexTextBox_LostFocus;
                _hexTextBox.KeyDown += HexTextBox_KeyDown;
                _hexTextBox.GotFocus += HexTextBox_GotFocus;
            }

            UpdateHexTextBox();
        }

        private void DetachHandlers()
        {
            if (_hexTextBox != null)
            {
                _hexTextBox.LostFocus -= HexTextBox_LostFocus;
                _hexTextBox.KeyDown -= HexTextBox_KeyDown;
                _hexTextBox.GotFocus -= HexTextBox_GotFocus;
            }
        }

        private void UpdateHexTextBox()
        {
            if (_hexTextBox != null && !_hexTextBox.IsKeyboardFocused)
            {
                string hex = ColorToHex(SelectedColor);
                if (_hexTextBox.Text != hex)
                {
                    _hexTextBox.Text = hex;
                }
            }
        }

        #endregion

        #region Hex Text Editing

        private void HexTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _previousValidHex = textBox.Text;
            }
        }

        private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                CommitHexText(textBox);
            }
        }

        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (e.Key == Key.Enter)
            {
                CommitHexText(textBox);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                textBox.Text = _previousValidHex;
                e.Handled = true;
            }
        }

        private void CommitHexText(TextBox textBox)
        {
            if (TryParseHexColor(textBox.Text, out var color))
            {
                SelectedColor = color;
                textBox.Text = ColorToHex(color);
            }
            else
            {
                textBox.Text = _previousValidHex;
            }
        }

        #endregion

        #region Helpers

        private void OnColorChanged()
        {
            ColorChanged?.Invoke(this, new ColorChangedEventArgs(SelectedColor, SelectedBrush, ColorToHex(SelectedColor)));
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

        #endregion
    }
}
