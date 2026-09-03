/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Themes;
using System.Windows.Media.Media3D;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A <see cref="TextBlock"/> that selects a light or dark foreground from an explicit
    /// <see cref="BackgroundBrush"/> or the nearest supported visual ancestor background.
    /// </summary>
    /// <remarks>
    /// Automatic ancestor detection supports <see cref="Border"/>, <see cref="Panel"/>,
    /// <see cref="Control"/>, and <see cref="TextBlock"/> backgrounds. Set <see cref="BackgroundBrush"/>
    /// when the painted background is not exposed by one of those elements.
    /// </remarks>
    public class ThemedTextBlock : TextBlock
    {
        private DependencyObject? _observedBackgroundOwner;
        private DependencyProperty? _observedBackgroundProperty;
        private DependencyPropertyDescriptor? _observedBackgroundDescriptor;
        private Brush? _observedBrush;

        /// <summary>
        /// Identifies the <see cref="BackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundBrushProperty = DependencyProperty.Register(
            nameof(BackgroundBrush),
            typeof(Brush),
            typeof(ThemedTextBlock),
            new FrameworkPropertyMetadata(null, OnAppearancePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="LuminanceThreshold"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LuminanceThresholdProperty = DependencyProperty.Register(
            nameof(LuminanceThreshold),
            typeof(double),
            typeof(ThemedTextBlock),
            new FrameworkPropertyMetadata(
                ContrastBrushHelper.DefaultLuminanceThreshold,
                OnAppearancePropertyChanged,
                CoerceLuminanceThreshold));

        /// <summary>
        /// Identifies the <see cref="LightForegroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LightForegroundBrushProperty = DependencyProperty.Register(
            nameof(LightForegroundBrush),
            typeof(Brush),
            typeof(ThemedTextBlock),
            new FrameworkPropertyMetadata(Brushes.White, OnAppearancePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DarkForegroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DarkForegroundBrushProperty = DependencyProperty.Register(
            nameof(DarkForegroundBrush),
            typeof(Brush),
            typeof(ThemedTextBlock),
            new FrameworkPropertyMetadata(Brushes.Black, OnAppearancePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="PaletteFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteFamilyProperty = DependencyProperty.Register(
            nameof(PaletteFamily),
            typeof(string),
            typeof(ThemedTextBlock),
            new FrameworkPropertyMetadata(null, OnAppearancePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="PaletteShade"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaletteShadeProperty = DependencyProperty.Register(
            nameof(PaletteShade),
            typeof(int),
            typeof(ThemedTextBlock),
            new FrameworkPropertyMetadata(0, OnAppearancePropertyChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemedTextBlock"/> class.
        /// </summary>
        public ThemedTextBlock()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Gets or sets an explicit background brush used to calculate the foreground. When null, the
        /// nearest supported ancestor background is used.
        /// </summary>
        [Category("Appearance")]
        [Description("The optional background brush used to calculate the automatic foreground.")]
        public Brush? BackgroundBrush
        {
            get => (Brush?)GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the relative-luminance value at or below which the light foreground is selected.
        /// Raising the value favors the light foreground.
        /// </summary>
        [Category("Appearance")]
        [Description("The relative-luminance cutoff used to switch between the light and dark foreground brushes.")]
        [DefaultValue(ContrastBrushHelper.DefaultLuminanceThreshold)]
        public double LuminanceThreshold
        {
            get => (double)GetValue(LuminanceThresholdProperty);
            set => SetValue(LuminanceThresholdProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground used on backgrounds at or below <see cref="LuminanceThreshold"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("The foreground brush used on darker backgrounds.")]
        public Brush LightForegroundBrush
        {
            get => (Brush)GetValue(LightForegroundBrushProperty);
            set => SetValue(LightForegroundBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground used on backgrounds above <see cref="LuminanceThreshold"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("The foreground brush used on lighter backgrounds.")]
        public Brush DarkForegroundBrush
        {
            get => (Brush)GetValue(DarkForegroundBrushProperty);
            set => SetValue(DarkForegroundBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the optional Mosaic palette family used for a family-specific foreground switch point.
        /// Set this together with <see cref="PaletteShade"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("The optional Mosaic palette family used to select the foreground switch point.")]
        public string? PaletteFamily
        {
            get => (string?)GetValue(PaletteFamilyProperty);
            set => SetValue(PaletteFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the optional Mosaic palette shade used with <see cref="PaletteFamily"/>.
        /// A value of zero disables palette-specific selection.
        /// </summary>
        [Category("Appearance")]
        [Description("The optional Mosaic palette shade used to select the foreground switch point.")]
        [DefaultValue(0)]
        public int PaletteShade
        {
            get => (int)GetValue(PaletteShadeProperty);
            set => SetValue(PaletteShadeProperty, value);
        }

        /// <inheritdoc />
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            if (IsLoaded && BackgroundBrush is null)
            {
                ObserveAncestorBackground();
            }
        }

        private static object CoerceLuminanceThreshold(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;
            return double.IsNaN(value) ? ContrastBrushHelper.DefaultLuminanceThreshold : Math.Clamp(value, 0, 1);
        }

        private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = (ThemedTextBlock)d;

            if (e.Property == BackgroundBrushProperty)
            {
                textBlock.ObserveAncestorBackground();
                return;
            }

            textBlock.UpdateForeground();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ObserveAncestorBackground();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopObservingAncestorBackground();
        }

        private void ObserveAncestorBackground()
        {
            StopObservingAncestorBackground();

            if (BackgroundBrush is not null)
            {
                ObserveBrush(BackgroundBrush);
                UpdateForeground();
                return;
            }

            DependencyObject? ancestor = GetParent(this);

            while (ancestor is not null)
            {
                if (TryGetBackgroundProperty(ancestor, out DependencyProperty? property) &&
                    ancestor.GetValue(property) is Brush)
                {
                    _observedBackgroundOwner = ancestor;
                    _observedBackgroundProperty = property;
                    _observedBackgroundDescriptor = DependencyPropertyDescriptor.FromProperty(property, ancestor.GetType());
                    _observedBackgroundDescriptor?.AddValueChanged(ancestor, OnAncestorBackgroundChanged);
                    ObserveBrush((Brush)ancestor.GetValue(property));
                    break;
                }

                ancestor = GetParent(ancestor);
            }

            UpdateForeground();
        }

        private void StopObservingAncestorBackground()
        {
            if (_observedBackgroundOwner is not null && _observedBackgroundDescriptor is not null)
            {
                _observedBackgroundDescriptor.RemoveValueChanged(_observedBackgroundOwner, OnAncestorBackgroundChanged);
            }

            if (_observedBrush is not null)
            {
                _observedBrush.Changed -= OnObservedBrushChanged;
            }

            _observedBackgroundOwner = null;
            _observedBackgroundProperty = null;
            _observedBackgroundDescriptor = null;
            _observedBrush = null;
        }

        private void OnAncestorBackgroundChanged(object? sender, EventArgs e)
        {
            Brush? brush = _observedBackgroundOwner?.GetValue(_observedBackgroundProperty!) as Brush;
            ObserveBrush(brush);
            UpdateForeground();
        }

        private void ObserveBrush(Brush? brush)
        {
            if (ReferenceEquals(_observedBrush, brush))
            {
                return;
            }

            if (_observedBrush is not null)
            {
                _observedBrush.Changed -= OnObservedBrushChanged;
            }

            _observedBrush = brush;

            if (_observedBrush is not null && !_observedBrush.IsFrozen)
            {
                _observedBrush.Changed += OnObservedBrushChanged;
            }
        }

        private void OnObservedBrushChanged(object? sender, EventArgs e)
        {
            UpdateForeground();
        }

        private void UpdateForeground()
        {
            Brush? background = BackgroundBrush;

            if (background is null && _observedBackgroundOwner is not null && _observedBackgroundProperty is not null)
            {
                background = _observedBackgroundOwner.GetValue(_observedBackgroundProperty) as Brush;
            }

            if (!TryGetRepresentativeColor(background, out Color color))
            {
                return;
            }

            bool useLightForeground = ContrastBrushHelper.ShouldUseLightForeground(
                PaletteFamily,
                PaletteShade,
                color,
                LuminanceThreshold);

            Brush foreground = useLightForeground ? LightForegroundBrush : DarkForegroundBrush;

            SetCurrentValue(ForegroundProperty, foreground);
        }

        private static bool TryGetBackgroundProperty(DependencyObject element, out DependencyProperty? property)
        {
            property = element switch
            {
                Border => Border.BackgroundProperty,
                Panel => Panel.BackgroundProperty,
                Control => Control.BackgroundProperty,
                TextBlock => TextBlock.BackgroundProperty,
                _ => null
            };

            return property is not null;
        }

        private static bool TryGetRepresentativeColor(Brush? brush, out Color color)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                color = solidColorBrush.Color;
                return true;
            }

            if (brush is GradientBrush { GradientStops.Count: > 0 } gradientBrush)
            {
                double red = 0;
                double green = 0;
                double blue = 0;

                foreach (GradientStop stop in gradientBrush.GradientStops)
                {
                    red += stop.Color.R;
                    green += stop.Color.G;
                    blue += stop.Color.B;
                }

                double count = gradientBrush.GradientStops.Count;
                color = Color.FromRgb((byte)(red / count), (byte)(green / count), (byte)(blue / count));
                return true;
            }

            color = default;
            return false;
        }

        private static DependencyObject? GetParent(DependencyObject element)
        {
            return element is Visual or Visual3D
                ? VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }
    }
}
