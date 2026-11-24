/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Renders a Ribbon command icon from an <see cref="ImageSource"/> or, when no image is assigned,
    /// from a font glyph. A missing image and glyph render nothing but keep the reserved size so labels
    /// stay aligned.
    /// </summary>
    public class RibbonIconPresenter : FrameworkElement
    {
        /// <summary>
        /// The default glyph font: Segoe Fluent Icons on Windows 11 with a Segoe MDL2 Assets fallback.
        /// </summary>
        public static readonly FontFamily DefaultGlyphFontFamily = new("Segoe Fluent Icons, Segoe MDL2 Assets");

        private UIElement? _child;

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(ImageSource), typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Gets or sets the image to show. Takes precedence over <see cref="Glyph"/>.
        /// </summary>
        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FallbackSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FallbackSourceProperty = DependencyProperty.Register(
            nameof(FallbackSource), typeof(ImageSource), typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Gets or sets an image used when <see cref="Source"/> is unset (for example the small image scaled up
        /// when a large image is missing).
        /// </summary>
        public ImageSource? FallbackSource
        {
            get => (ImageSource?)GetValue(FallbackSourceProperty);
            set => SetValue(FallbackSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UseAdaptiveSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseAdaptiveSourceProperty = DependencyProperty.Register(
            nameof(UseAdaptiveSource), typeof(bool), typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(false, OnContentChanged));

        /// <summary>
        /// Gets or sets whether <see cref="Source"/> is rendered through <see cref="AdaptiveImage"/>.
        /// </summary>
        public bool UseAdaptiveSource
        {
            get => (bool)GetValue(UseAdaptiveSourceProperty);
            set => SetValue(UseAdaptiveSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UseAdaptiveFallbackSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseAdaptiveFallbackSourceProperty = DependencyProperty.Register(
            nameof(UseAdaptiveFallbackSource), typeof(bool), typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(false, OnContentChanged));

        /// <summary>
        /// Gets or sets whether <see cref="FallbackSource"/> is rendered through <see cref="AdaptiveImage"/>.
        /// </summary>
        public bool UseAdaptiveFallbackSource
        {
            get => (bool)GetValue(UseAdaptiveFallbackSourceProperty);
            set => SetValue(UseAdaptiveFallbackSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Glyph"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Gets or sets the font glyph shown when no image is available.
        /// </summary>
        public string? Glyph
        {
            get => (string?)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GlyphBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GlyphBrushProperty = DependencyProperty.Register(
            nameof(GlyphBrush), typeof(Brush), typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(null, OnContentChanged));

        /// <summary>
        /// Gets or sets an optional glyph color; the inherited foreground is used when unset.
        /// </summary>
        public Brush? GlyphBrush
        {
            get => (Brush?)GetValue(GlyphBrushProperty);
            set => SetValue(GlyphBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
            nameof(IconSize), typeof(double), typeof(RibbonIconPresenter),
            new FrameworkPropertyMetadata(RibbonMetrics.SmallIconSize, FrameworkPropertyMetadataOptions.AffectsMeasure, OnContentChanged));

        /// <summary>
        /// Gets or sets the square icon size in device-independent units.
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the presenter has an image or glyph to show.
        /// </summary>
        public bool HasIcon => Source != null || FallbackSource != null || !string.IsNullOrEmpty(Glyph);

        static RibbonIconPresenter()
        {
            FocusableProperty.OverrideMetadata(typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(false));
            IsHitTestVisibleProperty.OverrideMetadata(typeof(RibbonIconPresenter), new FrameworkPropertyMetadata(false));
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonIconPresenter)d).Rebuild();
        }

        private void Rebuild()
        {
            if (_child != null)
            {
                RemoveVisualChild(_child);
                _child = null;
            }

            var source = Source ?? FallbackSource;
            if (source != null)
            {
                var useAdaptiveImage = Source != null ? UseAdaptiveSource : UseAdaptiveFallbackSource;
                Image image = useAdaptiveImage ? new AdaptiveImage() : new Image();
                image.Source = source;
                image.Stretch = Stretch.Uniform;
                image.Width = IconSize;
                image.Height = IconSize;
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
                _child = image;
            }
            else if (!string.IsNullOrEmpty(Glyph))
            {
                var text = new TextBlock
                {
                    Text = Glyph,
                    FontFamily = DefaultGlyphFontFamily,
                    FontSize = Math.Round(IconSize * 0.9),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Normal,
                    FontStyle = FontStyles.Normal
                };

                if (GlyphBrush != null && IsEnabled)
                {
                    text.Foreground = GlyphBrush;
                }

                _child = text;
            }

            if (_child != null)
            {
                AddVisualChild(_child);
            }

            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // A colored glyph falls back to the (disabled) foreground so disabled commands read as disabled.
            if (e.Property == IsEnabledProperty && GlyphBrush != null)
            {
                Rebuild();
            }
        }

        /// <inheritdoc />
        protected override int VisualChildrenCount => _child == null ? 0 : 1;

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index)
        {
            if (_child == null || index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _child;
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            _child?.Measure(new Size(IconSize, IconSize));
            return HasIcon ? new Size(IconSize, IconSize) : new Size(0, 0);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            _child?.Arrange(new Rect(0, 0, IconSize, IconSize));
            return HasIcon ? new Size(IconSize, IconSize) : new Size(0, 0);
        }
    }

    /// <summary>
    /// A rich Ribbon tooltip with a title, explanation, optional shortcut text and an optional footer.
    /// Ribbon commands create one automatically from their label, <see cref="RibbonItem.DescriptionProperty"/>
    /// and <see cref="RibbonItem.KeyGestureTextProperty"/>; assign your own to <see cref="FrameworkElement.ToolTip"/>
    /// to replace it.
    /// </summary>
    public class RibbonScreenTip : ToolTip
    {
        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(RibbonScreenTip), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the bold heading.
        /// </summary>
        [Category("Common")]
        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(RibbonScreenTip), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the explanatory text.
        /// </summary>
        [Category("Common")]
        public string? Description
        {
            get => (string?)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="GestureText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GestureTextProperty = DependencyProperty.Register(
            nameof(GestureText), typeof(string), typeof(RibbonScreenTip), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the shortcut text shown beside the title, for example <c>(Ctrl+B)</c>.
        /// </summary>
        [Category("Common")]
        public string? GestureText
        {
            get => (string?)GetValue(GestureTextProperty);
            set => SetValue(GestureTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Footer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
            nameof(Footer), typeof(object), typeof(RibbonScreenTip), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets optional footer content shown under a separator (for example "Press F1 for more help").
        /// </summary>
        [Category("Common")]
        public object? Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FooterTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterTemplateProperty = DependencyProperty.Register(
            nameof(FooterTemplate), typeof(DataTemplate), typeof(RibbonScreenTip), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the template used to display <see cref="Footer"/>.
        /// </summary>
        public DataTemplate? FooterTemplate
        {
            get => (DataTemplate?)GetValue(FooterTemplateProperty);
            set => SetValue(FooterTemplateProperty, value);
        }

        static RibbonScreenTip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonScreenTip), new FrameworkPropertyMetadata(typeof(RibbonScreenTip)));
        }

        /// <inheritdoc />
        protected override void OnOpened(RoutedEventArgs e)
        {
            // A generated tip for a command with no label or description has nothing to say.
            if (string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Description) && Footer == null && Content == null)
            {
                IsOpen = false;
                return;
            }

            base.OnOpened(e);
        }

        /// <summary>
        /// Assigns an automatically generated screen tip bound to a Ribbon command's label, description and
        /// shortcut text. A tooltip set later in XAML replaces it.
        /// </summary>
        internal static void AttachDefault(FrameworkElement element)
        {
            var tip = new RibbonScreenTip();
            tip.SetBinding(TitleProperty, new Binding { Source = element, Path = new PropertyPath(RibbonItem.LabelProperty) });
            tip.SetBinding(DescriptionProperty, new Binding { Source = element, Path = new PropertyPath(RibbonItem.DescriptionProperty) });
            tip.SetBinding(GestureTextProperty, new Binding { Source = element, Path = new PropertyPath(RibbonItem.KeyGestureTextProperty) });
            element.ToolTip = tip;
            ToolTipService.SetShowOnDisabled(element, true);
            ToolTipService.SetInitialShowDelay(element, 600);
            ToolTipService.SetPlacement(element, PlacementMode.Bottom);
        }
    }
}
