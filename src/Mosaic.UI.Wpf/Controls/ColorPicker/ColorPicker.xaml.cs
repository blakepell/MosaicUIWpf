/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable InconsistentNaming

using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using Mosaic.UI.Wpf.Cache;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A color picker UserControl that allows users to select colors from presets or enter hex values.
    /// </summary>
    public partial class ColorPicker : INotifyPropertyChanged
    {

        #region Private Fields

        private TextBox? _hexTextBox;
        private Border? _colorPreview;
        private Button? _dropDownButton;
        private Popup? _colorPopup;
        private ItemsControl? _colorItemsControl;
        private string? _previousValidHexValue;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(ColorPicker), new FrameworkPropertyMetadata(new CornerRadius(2), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Gets or sets the CornerRadius applied to PART_MainBorder.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(
            nameof(SelectedBrush), typeof(Brush), typeof(ColorPicker), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedBrushChanged));

        /// <summary>
        /// Gets or sets the selected brush.
        /// </summary>
        public Brush SelectedBrush
        {
            get => (Brush)GetValue(SelectedBrushProperty);
            set => SetValue(SelectedBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            nameof(SelectedColor), typeof(Color), typeof(ColorPicker), new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HexValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HexValueProperty = DependencyProperty.Register(
            nameof(HexValue), typeof(string), typeof(ColorPicker), new FrameworkPropertyMetadata("#000000", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHexValueChanged));

        /// <summary>
        /// Gets or sets the hex value of the selected color.
        /// </summary>
        public string HexValue
        {
            get => (string)GetValue(HexValueProperty);
            set => SetValue(HexValueProperty, value);
        }

        #endregion

        #region Events Declarations

        /// <summary>
        /// Event that occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Event that occurs when the selected color changes.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        /// <summary>
        /// Collection of preset color items for the dropdown.
        /// </summary>
        public ObservableCollection<ColorPickerItem> PresetColors { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPicker"/> class.
        /// </summary>
        public ColorPicker()
        {
            InitializeComponent();
            InitializePresetColors();
            DataContext = this;
            Loaded += ColorPicker_Loaded;
            
            // Store the initial value as the previous valid value
            _previousValidHexValue = HexValue;
        }

        /// <summary>
        /// Handles the Loaded event to initialize the control parts.
        /// </summary>
        private void ColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeControlParts();
        }

        /// <summary>
        /// Initializes the control parts after the control is loaded.
        /// </summary>
        private void InitializeControlParts()
        {
            // Find the named elements in the visual tree
            _hexTextBox = FindName("PART_HexTextBox") as TextBox;
            _colorPreview = FindName("PART_ColorPreview") as Border;
            _dropDownButton = FindName("PART_DropDownButton") as Button;
            _colorPopup = FindName("PART_ColorPopup") as Popup;
            _colorItemsControl = FindName("PART_ColorItemsControl") as ItemsControl;

            // Hook up event handlers
            if (_hexTextBox != null)
            {
                _hexTextBox.LostFocus += HexTextBox_LostFocus;
                _hexTextBox.KeyDown += HexTextBox_KeyDown;
                _hexTextBox.GotFocus += HexTextBox_GotFocus;
            }

            if (_dropDownButton != null)
            {
                _dropDownButton.Click += DropDownButton_Click;
            }

            if (_colorItemsControl != null)
            {
                _colorItemsControl.ItemsSource = PresetColors;
            }

            if (_colorPopup != null)
            {
                _colorPopup.Closed += ColorPopup_Closed;
            }

            UpdateControls();
        }

        /// <summary>
        /// Initializes the preset colors with Fluent Design inspired colors.
        /// </summary>
        private void InitializePresetColors()
        {
            PresetColors =
            [
                new("Red", "#FF5252", ColorPaletteCache.GetBrush("#FF5252")),
                new("Pink", "#E91E63", ColorPaletteCache.GetBrush("#E91E63")),
                new("Purple", "#9C27B0", ColorPaletteCache.GetBrush("#9C27B0")),
                new("Deep Purple", "#673AB7", ColorPaletteCache.GetBrush("#673AB7")),
                new("Indigo", "#3F51B5", ColorPaletteCache.GetBrush("#3F51B5")),
                new("Blue", "#2196F3", ColorPaletteCache.GetBrush("#2196F3")),
                new("Light Blue", "#03A9F4", ColorPaletteCache.GetBrush("#03A9F4")),
                new("Cyan", "#00BCD4", ColorPaletteCache.GetBrush("#00BCD4")),
                new("Teal", "#009688", ColorPaletteCache.GetBrush("#009688")),
                new("Green", "#4CAF50", ColorPaletteCache.GetBrush("#4CAF50")),
                new("Light Green", "#8BC34A", ColorPaletteCache.GetBrush("#8BC34A")),
                new("Lime", "#CDDC39", ColorPaletteCache.GetBrush("#CDDC39")),
                new("Yellow", "#FFEB3B", ColorPaletteCache.GetBrush("#FFEB3B")),
                new("Amber", "#FFC107", ColorPaletteCache.GetBrush("#FFC107")),
                new("Orange", "#FF9800", ColorPaletteCache.GetBrush("#FF9800")),
                new("Deep Orange", "#FF5722", ColorPaletteCache.GetBrush("#FF5722")),
                new("Brown", "#795548", ColorPaletteCache.GetBrush("#795548")),
                new("Grey", "#9E9E9E", ColorPaletteCache.GetBrush("#9E9E9E")),
                new("Blue Grey", "#607D8B", ColorPaletteCache.GetBrush("#607D8B")),
                new("Black", "#000000", ColorPaletteCache.GetBrush("#000000")),
                new("White", "#FFFFFF", ColorPaletteCache.GetBrush("#FFFFFF")),

                // Dark theme variants
                new("Dark Red", "#D32F2F", ColorPaletteCache.GetBrush("#D32F2F")),
                new("Dark Pink", "#C2185B", ColorPaletteCache.GetBrush("#C2185B")),
                new("Dark Purple", "#7B1FA2", ColorPaletteCache.GetBrush("#7B1FA2")),
                new("Dark Blue", "#1976D2", ColorPaletteCache.GetBrush("#1976D2")),
                new("Dark Green", "#388E3C", ColorPaletteCache.GetBrush("#388E3C")),
                new("Dark Orange", "#F57C00", ColorPaletteCache.GetBrush("#F57C00")),
                new("Dark Teal", "#00796B", ColorPaletteCache.GetBrush("#00796B")),
                new("Dark Cyan", "#006064", ColorPaletteCache.GetBrush("#006064")),
                new("Dark Indigo", "#283593", ColorPaletteCache.GetBrush("#283593")),
                new("Dark Blue Grey", "#455A64", ColorPaletteCache.GetBrush("#455A64")),
                new("Dark Lime", "#AFB42B", ColorPaletteCache.GetBrush("#AFB42B")),
                new("Dark Yellow", "#FBC02D", ColorPaletteCache.GetBrush("#FBC02D")),
                new("Dark Amber", "#FF8F00", ColorPaletteCache.GetBrush("#FF8F00")),
                new("Dark Deep Orange", "#E64A19", ColorPaletteCache.GetBrush("#E64A19")),
                new("Dark Brown", "#5D4037", ColorPaletteCache.GetBrush("#5D4037")),
                new("Dark Grey", "#616161", ColorPaletteCache.GetBrush("#616161"))
            ];
        }

        /// <summary>
        /// Called when the SelectedBrush property changes.
        /// </summary>
        private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPicker colorPicker && e.NewValue is SolidColorBrush brush)
            {
                colorPicker.SelectedColor = brush.Color;
                colorPicker.HexValue = brush.Color.ToString();
                colorPicker.UpdateControls();
                colorPicker.OnColorChanged();
            }
        }

        /// <summary>
        /// Called when the SelectedColor property changes.
        /// </summary>
        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPicker colorPicker && e.NewValue is Color color)
            {
                colorPicker.SelectedBrush = ColorPaletteCache.GetBrush(color);
                colorPicker.HexValue = color.ToString();
                colorPicker.UpdateControls();
                colorPicker.OnColorChanged();
            }
        }

        /// <summary>
        /// Called when the HexValue property changes.
        /// </summary>
        private static void OnHexValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPicker colorPicker && e.NewValue is string hexValue)
            {
                if (IsValidHexColor(hexValue))
                {
                    try
                    {
                        var color = ColorPaletteCache.GetColor(hexValue);
                        colorPicker.SelectedColor = color;
                        colorPicker.SelectedBrush = ColorPaletteCache.GetBrush(color);

                        colorPicker.UpdateControls();
                        colorPicker.OnColorChanged();

                        // Update the previous valid value when the property is set programmatically
                        colorPicker._previousValidHexValue = hexValue;
                    }
                    catch
                    {
                        // Invalid hex value, ignore
                    }
                }
            }
        }

        /// <summary>
        /// Updates all controls to reflect the current selected color.
        /// </summary>
        private void UpdateControls()
        {
            if (_colorPreview != null)
            {
                _colorPreview.Background = SelectedBrush;
            }

            OnPropertyChanged(nameof(SelectedBrush));
            OnPropertyChanged(nameof(SelectedColor));
            OnPropertyChanged(nameof(HexValue));
        }

        /// <summary>
        /// Handles the TextBox got focus event to store the current valid value.
        /// </summary>
        private void HexTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Store the current value as the previous valid value when starting to edit
                _previousValidHexValue = textBox.Text;
            }
        }

        /// <summary>
        /// Handles the TextBox lost focus event to validate and commit or revert the value.
        /// </summary>
        private void HexTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                ValidateAndCommitHexValue(textBox);
            }
        }

        /// <summary>
        /// Handles the TextBox key down event to validate on Enter key.
        /// </summary>
        private void HexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (e.Key == Key.Enter)
                {
                    ValidateAndCommitHexValue(textBox);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    // Revert to previous valid value on Escape
                    if (_previousValidHexValue != null)
                    {
                        textBox.Text = _previousValidHexValue;
                        HexValue = _previousValidHexValue;
                    }
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Validates the hex input and either commits the new value or reverts to the previous valid value.
        /// </summary>
        private void ValidateAndCommitHexValue(TextBox textBox)
        {
            string input = textBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                // Revert to previous valid value if empty
                if (_previousValidHexValue != null)
                {
                    textBox.Text = _previousValidHexValue;
                    return;
                }
            }

            string cleanedInput = input;
            bool addedHashPrefix = false;

            if (!cleanedInput.StartsWith('#'))
            {
                cleanedInput = $"#{cleanedInput}";
                addedHashPrefix = true;
            }

            if (IsValidHexColor(cleanedInput))
            {
                // Valid color - update the property and store as new previous valid value
                string normalizedHex = cleanedInput.ToUpper();
                HexValue = normalizedHex;
                _previousValidHexValue = normalizedHex;

                // Update the textbox to show the normalized format
                if (textBox.Text != normalizedHex)
                {
                    textBox.Text = normalizedHex;

                    // If we added a hash prefix, select it to provide visual feedback
                    if (addedHashPrefix)
                    {
                        // Use Dispatcher to ensure the text is updated before setting selection
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            textBox.SelectionStart = 0;
                            textBox.SelectionLength = 1; // Select just the "#" character
                        }));
                    }
                }
            }
            else
            {
                // Invalid color - revert to previous valid value
                if (_previousValidHexValue != null)
                {
                    textBox.Text = _previousValidHexValue;
                    HexValue = _previousValidHexValue;
                }
            }
        }

        /// <summary>
        /// Validates if a string is a valid hex color.
        /// </summary>
        private static bool IsValidHexColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return false;

            string pattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$";
            return Regex.IsMatch(hex, pattern);
        }

        /// <summary>
        /// Raises the ColorChanged event.
        /// </summary>
        private void OnColorChanged()
        {
            ColorChanged?.Invoke(this, new ColorChangedEventArgs(SelectedColor, SelectedBrush, HexValue));
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Handles the dropdown button click event.
        /// </summary>
        private void DropDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_colorPopup != null)
            {
                _colorPopup.IsOpen = !_colorPopup.IsOpen;
            }
        }

        /// <summary>
        /// Handles the color popup closed event.
        /// </summary>
        private void ColorPopup_Closed(object? sender, EventArgs e)
        {
            // Focus back to the control
            Focus();
        }

        /// <summary>
        /// Handles clicking on a color item in the popup.
        /// </summary>
        private void ColorItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: ColorPickerItem colorItem })
            {
                HexValue = colorItem.HexValue;
                _previousValidHexValue = colorItem.HexValue;

                if (_colorPopup != null)
                {
                    _colorPopup.IsOpen = false;
                }
            }
        }
    }
}