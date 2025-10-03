/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A ComboBox-based control that allows editing and selecting colors using
    /// hex strings (supports #RGB, #RRGGBB, #AARRGGBB) and named brushes.
    /// </summary>
    public class HexColorTextBox : ComboBox
    {
        private TextBox? _editableTextBox;
        private string _previousValue = "#FF000000";
        private bool _isUpdating = false;
        // When true, selection changes should not normalize/overwrite the editable text while user types.
        private bool _suppressSelectionChangeDuringTyping = false;

        // Ensure we detach handlers when unloaded to avoid memory leaks
        private void DetachEditableTextBoxEvents()
        {
            if (_editableTextBox != null)
            {
                _editableTextBox.TextChanged -= OnEditableTextChanged;
                _editableTextBox.LostFocus -= OnEditableLostFocus;
                _editableTextBox.PreviewKeyDown -= OnEditablePreviewKeyDown;
                _editableTextBox = null;
            }
        }

        /// <summary>
        /// Corner radius for the control border.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(HexColorTextBox), new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Gets or sets the corner radius for the control border.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Hex color text (e.g. #FFFFFFFF).
        /// </summary>
        public static readonly DependencyProperty HexColorProperty = DependencyProperty.Register(
            nameof(HexColor), typeof(string), typeof(HexColorTextBox),
            new FrameworkPropertyMetadata("#FF000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHexColorChanged));

        /// <summary>
        /// Gets or sets the hex color string (normalized to #AARRGGBB) displayed/edited by the control.
        /// </summary>
        public string HexColor
        {
            get => (string)GetValue(HexColorProperty);
            set => SetValue(HexColorProperty, value);
        }

        /// <summary>
        /// The selected color value.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color), typeof(Color), typeof(HexColorTextBox),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

        /// <summary>
        /// Gets or sets the currently-selected <see cref="Color"/> value for the control.
        /// </summary>
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// The collection of shades shown in the drop-down.
        /// NOTE: Do NOT create a mutable default instance in the dependency metadata.
        /// Each control must have its own collection instance.
        /// </summary>
        public static readonly DependencyProperty ColorShadesProperty = DependencyProperty.Register(
            nameof(ColorShades), typeof(ObservableCollection<Color>), typeof(HexColorTextBox),
            new PropertyMetadata(null));

        /// <summary>
        /// The collection of color shades displayed in the drop-down list.
        /// </summary>
        public ObservableCollection<Color> ColorShades
        {
            get => (ObservableCollection<Color>)GetValue(ColorShadesProperty)!;
            set => SetValue(ColorShadesProperty, value);
        }

        // Static constructor: default style and metadata overrides
        static HexColorTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HexColorTextBox),
                new FrameworkPropertyMetadata(typeof(HexColorTextBox)));

            // Make editable by default for ease-of-use in XAML
            IsEditableProperty.OverrideMetadata(typeof(HexColorTextBox),
                new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="HexColorTextBox"/>.
        /// </summary>
        public HexColorTextBox()
        {
            ColorShades = new ObservableCollection<Color>();
            _previousValue = ColorToHex(Colors.Black);
            UpdateColorShades(Colors.Black);

            // Detach handlers when control unloads to prevent event handler leaks
            Unloaded += OnControlUnloaded;
        }

        /// <summary>
        /// Handles changes to the <see cref="HexColorTextBox.HexColor"/> dependency property.
        /// </summary>
        /// <remarks>This method normalizes the new hex color value, updates the <see
        /// cref="HexColorTextBox.Color"/> property,  and synchronizes the text displayed in the editable text box, if
        /// applicable. If the new value is invalid,  no changes are applied. The method prevents recursive updates by
        /// using an internal flag.</remarks>
        /// <param name="d">The object on which the property value has changed. This is expected to be an instance of <see
        /// cref="HexColorTextBox"/>.</param>
        /// <param name="e">Provides data about the property change, including the old and new values.</param>
        private static void OnHexColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexColorTextBox control && e.NewValue is string hexValue)
            {
                if (control._isUpdating) return;

                if (control.TryParseHexColor(hexValue, out Color color, out string normalized))
                {
                    control._isUpdating = true;
                    control.Color = color;
                    control._isUpdating = false;
                    control._previousValue = normalized;

                    if (control._editableTextBox != null && control._editableTextBox.Text != normalized)
                    {
                        control._isUpdating = true;
                        control._editableTextBox.Text = normalized;
                        control._isUpdating = false;
                    }
                }
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="HexColorTextBox"/> control's color value when the associated dependency
        /// property changes.
        /// </summary>
        /// <remarks>This method updates the control's internal state and UI to reflect the new color
        /// value. It synchronizes the hex color representation, updates the color shades, and ensures the editable text
        /// box and selected item are consistent with the new color.</remarks>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property change occurred. This should be an instance of <see
        /// cref="HexColorTextBox"/>.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexColorTextBox control && e.NewValue is Color color)
            {
                if (control._isUpdating)
                {
                    control.UpdateColorShades(color);
                    return;
                }

                string hexValue = control.ColorToHex(color);

                control._isUpdating = true;
                control.HexColor = hexValue;
                control.UpdateColorShades(color);
                control._isUpdating = false;

                control._previousValue = hexValue;

                // Update editable text and selection
                if (control._editableTextBox != null && control._editableTextBox.Text != hexValue)
                {
                    control._isUpdating = true;
                    control._editableTextBox.Text = hexValue;
                    control._isUpdating = false;
                }

                // Try select matching shade (iterate internal ColorShades)
                foreach (var item in control.ColorShades)
                {
                    if (item == color)
                    {
                        control.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// ApplyTemplate wiring: finds the editable TextBox inside the ComboBox template
        /// and wires up events. Also ensures color shades reflect the current Color.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // detach if previously attached
            DetachEditableTextBoxEvents();

            _editableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;

            if (_editableTextBox != null)
            {
                // Don't set the TextBox.Text directly here - bindings on the template
                // can still be initializing and may overwrite our value. Wire up
                // handlers and defer the final synchronization to the Dispatcher
                // so it occurs after layout/binding initialization.
                _editableTextBox.TextChanged += OnEditableTextChanged;
                _editableTextBox.LostFocus += OnEditableLostFocus;
                _editableTextBox.PreviewKeyDown += OnEditablePreviewKeyDown;
            }

            // We manage Items manually (not via ItemsSource binding)
            // Make sure current Color is reflected in shades
            UpdateColorShades(Color);

            // After rebuilding the shades, defer selecting the matching shade and
            // syncing the HexColor/TextBox to the Dispatcher to ensure bindings
            // have completed. This avoids races where template bindings overwrite
            // our immediate assignments.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    _isUpdating = true;
                    string hex = ColorToHex(Color);
                    HexColor = hex;
                    _previousValue = hex;

                    // Select matching shade (iterate internal ColorShades)
                    foreach (var item in ColorShades)
                    {
                        if (item == Color)
                        {
                            SelectedItem = item;
                            break;
                        }
                    }

                    if (_editableTextBox != null && _editableTextBox.Text != hex)
                    {
                        _editableTextBox.Text = hex;
                    }
                }
                finally
                {
                    _isUpdating = false;
                }
            }), DispatcherPriority.Loaded);
        }

        private void OnControlUnloaded(object? sender, RoutedEventArgs e)
        {
            Unloaded -= OnControlUnloaded;
            DetachEditableTextBoxEvents();
        }

        /// <summary>
        /// Called when the selection changes in the combo box. Updates the control's
        /// Color and HexColor properties to match the newly selected color item.
        /// </summary>
        /// <param name="e">Selection changed event data.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // If we're suppressing selection updates while the user is typing, don't normalize or
            // overwrite the editable text. This allows the user to continue typing freely.
            if (_suppressSelectionChangeDuringTyping) return;

            if (!_isUpdating && SelectedItem is Color selectedColor)
            {
                _isUpdating = true;
                Color = selectedColor;
                HexColor = ColorToHex(selectedColor);

                // update text box
                if (_editableTextBox != null && _editableTextBox.Text != HexColor)
                {
                    _editableTextBox.Text = HexColor;
                }

                _isUpdating = false;
            }
        }

        /// <summary>
        /// Handles the <see cref="TextChangedEventArgs"/> event for the editable text box,  updating the color value
        /// and normalizing the hex color string.
        /// </summary>
        /// <remarks>This method validates and parses the text input as a hexadecimal color value.  If the
        /// input is valid, it updates the <see cref="Color"/> and <see cref="HexColor"/> properties  and ensures the
        /// text box displays the normalized hex color string.  The method prevents recursive updates by using an
        /// internal flag.</remarks>
        /// <param name="sender">The source of the event, typically the editable text box.</param>
        /// <param name="e">The event data containing information about the text change.</param>
        private void OnEditableTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_editableTextBox == null || _isUpdating) return;

            string text = _editableTextBox.Text;
            if (TryParseHexColor(text, out Color color, out string normalized))
            {
                // Update the Color for live preview but do NOT modify the editable text or HexColor while
                // the user is actively typing. This prevents the control from auto-completing the user's
                // input. The final normalization/commit will occur on lost focus or when Enter is pressed.
                _suppressSelectionChangeDuringTyping = true;
                _isUpdating = true;
                Color = color; // update preview only
                _isUpdating = false;

                // Intentionally do NOT set HexColor or overwrite _editableTextBox.Text here.
            }
        }

        /// <summary>
        /// Handles the loss of focus event for the editable text box, validating and normalizing the entered hex color
        /// value.
        /// </summary>
        /// <remarks>If the entered text represents a valid hex color, it is normalized and applied to the
        /// <c>HexColor</c> and <c>Color</c> properties. If the text is invalid, the text box reverts to the previous
        /// valid value.</remarks>
        /// <param name="sender">The source of the event, typically the editable text box.</param>
        /// <param name="e">The event data associated with the <see cref="RoutedEventArgs"/>.</param>
        private void OnEditableLostFocus(object sender, RoutedEventArgs e)
        {
            if (_editableTextBox == null) return;

            string text = _editableTextBox.Text;
            if (TryParseHexColor(text, out Color color, out string normalized))
            {
                _isUpdating = true;
                HexColor = normalized;
                Color = color;
                if (_editableTextBox.Text != normalized)
                {
                    _editableTextBox.Text = normalized;
                }
                _isUpdating = false;
                _previousValue = normalized;
                // user finished typing, allow selection/normalization again
                _suppressSelectionChangeDuringTyping = false;
            }
            else
            {
                // revert invalid
                _editableTextBox.Text = _previousValue;
                HexColor = _previousValue;
                TryParseHexColor(_previousValue, out Color prevColor, out _);
                Color = prevColor;
                _suppressSelectionChangeDuringTyping = false;
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewKeyDown"/> event for the editable text box.
        /// </summary>
        /// <remarks>This method processes specific key inputs when the editable text box is focused:
        /// <list type="bullet"> <item> <description>If the <see cref="Key.Escape"/> key is pressed, the text box
        /// reverts to its previous value, and the associated color properties are restored.</description> </item>
        /// <item> <description>If the <see cref="Key.Enter"/> key is pressed, the focus is moved to the next focusable
        /// element, simulating a "commit" action.</description> </item> </list> The event is marked as handled (<see
        /// cref="KeyEventArgs.Handled"/>) for these specific keys to prevent further processing.</remarks>
        /// <param name="sender">The source of the event, typically the editable text box.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void OnEditablePreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (_editableTextBox == null) return;

            if (e.Key == Key.Escape)
            {
                _editableTextBox.Text = _previousValue;
                HexColor = _previousValue;
                TryParseHexColor(_previousValue, out Color prevColor, out _);
                Color = prevColor;
                _suppressSelectionChangeDuringTyping = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                // commit: clear typing suppression then force lost-focus semantics
                _suppressSelectionChangeDuringTyping = false;
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Updates the collection of color shades based on the specified base color.
        /// </summary>
        /// <remarks>This method generates 10 lighter shades, the base color, and 10 darker shades,  and
        /// updates the <see cref="ColorShades"/> collection and the <see cref="Items"/> collection accordingly. The
        /// lighter shades are added in descending order of lightness, followed by the base color,  and then the darker
        /// shades in ascending order of darkness.</remarks>
        /// <param name="baseColor">The base color used to generate lighter and darker shades.</param>
        private void UpdateColorShades(Color baseColor)
        {
            // Ensure instance collection exists
            if (ColorShades == null)
            {
                ColorShades = new ObservableCollection<Color>();
            }

            ColorShades.Clear();

            // Manage ComboBox.Items manually
            Items.Clear();

            // Add 10 lighter shades (lightest first)
            for (int i = 10; i >= 1; i--)
            {
                double factor = i / 10.0;
                var shade = LightenColor(baseColor, factor);
                ColorShades.Add(shade);
                Items.Add(shade);
            }

            // Add base color
            ColorShades.Add(baseColor);
            Items.Add(baseColor);

            // Add 10 darker shades (closest darker first)
            for (int i = 1; i <= 10; i++)
            {
                double factor = i / 10.0;
                var shade = DarkenColor(baseColor, factor);
                ColorShades.Add(shade);
                Items.Add(shade);
            }
        }

        /// <summary>
        /// Adjusts the brightness of the specified color by increasing its RGB components proportionally.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> to be lightened.</param>
        /// <param name="factor">A value between 0 and 1 that determines the degree of lightening.  A value of 0 returns the original color,
        /// while a value of 1 results in white.</param>
        /// <returns>A new <see cref="Color"/> instance that is a lighter version of the input color.</returns>
        private Color LightenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Min(255, color.R + (255 - color.R) * factor),
                (byte)Math.Min(255, color.G + (255 - color.G) * factor),
                (byte)Math.Min(255, color.B + (255 - color.B) * factor));
        }

        /// <summary>
        /// Darkens the specified color by reducing its RGB components based on the given factor.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> to be darkened.</param>
        /// <param name="factor">A value between 0 and 1 representing the percentage by which to darken the color.  A factor of 0 leaves the
        /// color unchanged, while a factor of 1 results in black.</param>
        /// <returns>A new <see cref="Color"/> instance that is a darkened version of the input color.</returns>
        private Color DarkenColor(Color color, double factor)
        {
            return Color.FromArgb(
                color.A,
                (byte)Math.Max(0, color.R * (1 - factor)),
                (byte)Math.Max(0, color.G * (1 - factor)),
                (byte)Math.Max(0, color.B * (1 - factor)));
        }

        /// <summary>
        /// Attempts to parse a string representation of a color into a <see cref="Color"/> structure.
        /// </summary>
        private bool TryParseHexColor(string hex, out Color color, out string normalizedText)
        {
            color = Colors.Black;
            normalizedText = _previousValue;

            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            hex = hex.Trim();

            // Named brush?
            if (!hex.StartsWith("#", StringComparison.Ordinal))
            {
                var brushProp = typeof(Brushes).GetProperty(hex, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (brushProp != null)
                {
                    if (brushProp.GetValue(null) is SolidColorBrush scb)
                    {
                        color = scb.Color;
                        normalizedText = ColorToHex(color);
                        return true;
                    }
                }

                // try ColorConverter fallback (supports names and some formats)
                try
                {
                    var convObj = ColorConverter.ConvertFromString(hex);
                    if (convObj is Color c2)
                    {
                        color = c2;
                        normalizedText = ColorToHex(color);
                        return true;
                    }
                }
                catch { /* ignore */ }

                return false;
            }

            // Now hex starts with #
            string hexOnly = hex.Substring(1);

            if (hexOnly.Length == 3) // #RGB -> expand to RRGGBB, alpha FF
            {
                string r = new string(hexOnly[0], 2);
                string g = new string(hexOnly[1], 2);
                string b = new string(hexOnly[2], 2);
                hexOnly = "FF" + r + g + b;
            }
            else if (hexOnly.Length == 6) // RRGGBB -> prepend alpha FF
            {
                hexOnly = "FF" + hexOnly;
            }
            else if (hexOnly.Length == 8)
            {
                // AARRGGBB is fine already
            }
            else
            {
                return false;
            }

            // parse AARRGGBB
            if (uint.TryParse(hexOnly, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
            {
                byte a = (byte)((argb & 0xFF000000) >> 24);
                byte r = (byte)((argb & 0x00FF0000) >> 16);
                byte g = (byte)((argb & 0x0000FF00) >> 8);
                byte b = (byte)(argb & 0x000000FF);
                color = Color.FromArgb(a, r, g, b);
                normalizedText = ColorToHex(color);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a <see cref="Color"/> object to its hexadecimal string representation, including the alpha channel.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> to convert to a hexadecimal string.</param>
        /// <returns>A string representing the color in hexadecimal format, including the alpha channel, in the form "#AARRGGBB".</returns>
        private string ColorToHex(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
