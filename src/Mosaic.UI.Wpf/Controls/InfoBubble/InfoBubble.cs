/*
 * Code Originates from: https://github.com/atc-net/atc-wpf (MIT)
 */

using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Specifies the position of an <see cref="InfoBubble"/> indicator relative to its content.
    /// </summary>
    public enum InfoBubblePlacementMode
    {
        /// <summary>
        /// Positions the indicator at the upper-left edge.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Positions the indicator at the center of the upper edge.
        /// </summary>
        Top,

        /// <summary>
        /// Positions the indicator at the upper-right edge.
        /// </summary>
        TopRight,

        /// <summary>
        /// Positions the indicator at the center of the right edge.
        /// </summary>
        Right,

        /// <summary>
        /// Positions the indicator at the lower-right edge.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Positions the indicator at the center of the lower edge.
        /// </summary>
        Bottom,

        /// <summary>
        /// Positions the indicator at the lower-left edge.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Positions the indicator at the center of the left edge.
        /// </summary>
        Left
    }

    /// <summary>
    /// Displays content with an overlaid count, status, or notification indicator.
    /// </summary>
    [ContentProperty(nameof(Content))]
    public sealed class InfoBubble : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="InfoBubbleContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleContentProperty = DependencyProperty.Register(
            nameof(InfoBubbleContent),
            typeof(object),
            typeof(InfoBubble),
            new PropertyMetadata(null, OnInfoBubbleDisplayPropertiesChanged));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleContentTemplateProperty = DependencyProperty.Register(
            nameof(InfoBubbleContentTemplate),
            typeof(DataTemplate),
            typeof(InfoBubble),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleBackgroundProperty = DependencyProperty.Register(
            nameof(InfoBubbleBackground),
            typeof(Brush),
            typeof(InfoBubble),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleForegroundProperty = DependencyProperty.Register(
            nameof(InfoBubbleForeground),
            typeof(Brush),
            typeof(InfoBubble),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleBorderBrushProperty = DependencyProperty.Register(
            nameof(InfoBubbleBorderBrush),
            typeof(Brush),
            typeof(InfoBubble),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleBorderThicknessProperty = DependencyProperty.Register(
            nameof(InfoBubbleBorderThickness),
            typeof(Thickness),
            typeof(InfoBubble),
            new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleCornerRadiusProperty = DependencyProperty.Register(
            nameof(InfoBubbleCornerRadius),
            typeof(CornerRadius),
            typeof(InfoBubble),
            new PropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Identifies the <see cref="InfoBubblePlacementMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubblePlacementModeProperty = DependencyProperty.Register(
            nameof(InfoBubblePlacementMode),
            typeof(global::Mosaic.UI.Wpf.Controls.InfoBubblePlacementMode),
            typeof(InfoBubble),
            new PropertyMetadata(global::Mosaic.UI.Wpf.Controls.InfoBubblePlacementMode.TopRight));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleMarginProperty = DependencyProperty.Register(
            nameof(InfoBubbleMargin),
            typeof(Thickness),
            typeof(InfoBubble),
            new PropertyMetadata(default(Thickness), OnInfoBubbleMarginChanged));

        private static readonly DependencyPropertyKey InfoBubbleOverhangMarginPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(InfoBubbleOverhangMargin),
                typeof(Thickness),
                typeof(InfoBubble),
                new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Identifies the read-only <see cref="InfoBubbleOverhangMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleOverhangMarginProperty =
            InfoBubbleOverhangMarginPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey InfoBubbleIndicatorMarginPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(InfoBubbleIndicatorMargin),
                typeof(Thickness),
                typeof(InfoBubble),
                new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Identifies the read-only <see cref="InfoBubbleIndicatorMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleIndicatorMarginProperty =
            InfoBubbleIndicatorMarginPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="InfoBubbleFontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleFontFamilyProperty = DependencyProperty.Register(
            nameof(InfoBubbleFontFamily),
            typeof(FontFamily),
            typeof(InfoBubble),
            new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleFontSizeProperty = DependencyProperty.Register(
            nameof(InfoBubbleFontSize),
            typeof(double),
            typeof(InfoBubble),
            new PropertyMetadata(10d));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleFontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleFontWeightProperty = DependencyProperty.Register(
            nameof(InfoBubbleFontWeight),
            typeof(FontWeight),
            typeof(InfoBubble),
            new PropertyMetadata(FontWeights.Normal));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleMinWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleMinWidthProperty = DependencyProperty.Register(
            nameof(InfoBubbleMinWidth),
            typeof(double),
            typeof(InfoBubble),
            new PropertyMetadata(16d));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleMinHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleMinHeightProperty = DependencyProperty.Register(
            nameof(InfoBubbleMinHeight),
            typeof(double),
            typeof(InfoBubble),
            new PropertyMetadata(16d));

        /// <summary>
        /// Identifies the <see cref="InfoBubblePadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubblePaddingProperty = DependencyProperty.Register(
            nameof(InfoBubblePadding),
            typeof(Thickness),
            typeof(InfoBubble),
            new PropertyMetadata(new Thickness(4, 2, 4, 2)));

        /// <summary>
        /// Identifies the <see cref="IsInfoBubbleVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsInfoBubbleVisibleProperty = DependencyProperty.Register(
            nameof(IsInfoBubbleVisible),
            typeof(bool),
            typeof(InfoBubble),
            new PropertyMetadata(true, OnInfoBubbleDisplayPropertiesChanged));

        /// <summary>
        /// Identifies the <see cref="IsDot"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDotProperty = DependencyProperty.Register(
            nameof(IsDot),
            typeof(bool),
            typeof(InfoBubble),
            new PropertyMetadata(false, OnInfoBubbleDisplayPropertiesChanged));

        /// <summary>
        /// Identifies the <see cref="InfoBubbleMaxValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBubbleMaxValueProperty = DependencyProperty.Register(
            nameof(InfoBubbleMaxValue),
            typeof(int),
            typeof(InfoBubble),
            new PropertyMetadata(0, OnInfoBubbleDisplayPropertiesChanged));

        /// <summary>
        /// Identifies the <see cref="HideWhenZero"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HideWhenZeroProperty = DependencyProperty.Register(
            nameof(HideWhenZero),
            typeof(bool),
            typeof(InfoBubble),
            new PropertyMetadata(false, OnInfoBubbleDisplayPropertiesChanged));

        private static readonly DependencyPropertyKey DisplayInfoBubbleContentPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(DisplayInfoBubbleContent),
                typeof(object),
                typeof(InfoBubble),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the read-only <see cref="DisplayInfoBubbleContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayInfoBubbleContentProperty =
            DisplayInfoBubbleContentPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ComputedInfoBubbleVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ComputedInfoBubbleVisibility),
                typeof(bool),
                typeof(InfoBubble),
                new PropertyMetadata(false));

        /// <summary>
        /// Identifies the read-only <see cref="ComputedInfoBubbleVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ComputedInfoBubbleVisibilityProperty =
            ComputedInfoBubbleVisibilityPropertyKey.DependencyProperty;

        static InfoBubble()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(InfoBubble),
                new FrameworkPropertyMetadata(typeof(InfoBubble)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfoBubble"/> class.
        /// </summary>
        public InfoBubble()
        {
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Gets or sets the content displayed in the indicator.
        /// </summary>
        /// <value>
        /// The indicator content. The default is <see langword="null"/>.
        /// </value>
        public object? InfoBubbleContent
        {
            get => GetValue(InfoBubbleContentProperty);
            set => SetValue(InfoBubbleContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used to display the indicator content.
        /// </summary>
        /// <value>
        /// The indicator content template. The default is <see langword="null"/>.
        /// </value>
        public DataTemplate? InfoBubbleContentTemplate
        {
            get => (DataTemplate?)GetValue(InfoBubbleContentTemplateProperty);
            set => SetValue(InfoBubbleContentTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush that paints the indicator background.
        /// </summary>
        /// <value>
        /// The indicator background brush. The default is <see langword="null"/>.
        /// </value>
        public Brush? InfoBubbleBackground
        {
            get => (Brush?)GetValue(InfoBubbleBackgroundProperty);
            set => SetValue(InfoBubbleBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush that paints the indicator content.
        /// </summary>
        /// <value>
        /// The indicator foreground brush. The default is <see langword="null"/>.
        /// </value>
        public Brush? InfoBubbleForeground
        {
            get => (Brush?)GetValue(InfoBubbleForegroundProperty);
            set => SetValue(InfoBubbleForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush that paints the indicator border.
        /// </summary>
        /// <value>
        /// The indicator border brush. The default is <see langword="null"/>.
        /// </value>
        public Brush? InfoBubbleBorderBrush
        {
            get => (Brush?)GetValue(InfoBubbleBorderBrushProperty);
            set => SetValue(InfoBubbleBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness of the indicator border.
        /// </summary>
        /// <value>
        /// The indicator border thickness. The default is an empty <see cref="Thickness"/>.
        /// </value>
        public Thickness InfoBubbleBorderThickness
        {
            get => (Thickness)GetValue(InfoBubbleBorderThicknessProperty);
            set => SetValue(InfoBubbleBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the radius of the indicator corners.
        /// </summary>
        /// <value>
        /// The indicator corner radius. The default is 8 device-independent units.
        /// </value>
        public CornerRadius InfoBubbleCornerRadius
        {
            get => (CornerRadius)GetValue(InfoBubbleCornerRadiusProperty);
            set => SetValue(InfoBubbleCornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the indicator position relative to the content.
        /// </summary>
        /// <value>
        /// The indicator position. The default is <see cref="Controls.InfoBubblePlacementMode.TopRight"/>.
        /// </value>
        public InfoBubblePlacementMode InfoBubblePlacementMode
        {
            get => (InfoBubblePlacementMode)GetValue(InfoBubblePlacementModeProperty);
            set => SetValue(InfoBubblePlacementModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the margin used to fine-tune the indicator position.
        /// </summary>
        /// <value>
        /// The positioning margin. Negative values move the indicator outside the content bounds.
        /// The default is an empty <see cref="Thickness"/>.
        /// </value>
        public Thickness InfoBubbleMargin
        {
            get => (Thickness)GetValue(InfoBubbleMarginProperty);
            set => SetValue(InfoBubbleMarginProperty, value);
        }

        /// <summary>
        /// Gets the margin reserved around content for indicator overhang.
        /// </summary>
        /// <value>
        /// The margin derived from the negative components of <see cref="InfoBubbleMargin"/>.
        /// </value>
        public Thickness InfoBubbleOverhangMargin =>
            (Thickness)GetValue(InfoBubbleOverhangMarginProperty);

        /// <summary>
        /// Gets the margin that moves the indicator inward.
        /// </summary>
        /// <value>
        /// The margin derived from the non-negative components of <see cref="InfoBubbleMargin"/>.
        /// </value>
        public Thickness InfoBubbleIndicatorMargin =>
            (Thickness)GetValue(InfoBubbleIndicatorMarginProperty);

        /// <summary>
        /// Gets or sets the font family used by the indicator content.
        /// </summary>
        /// <value>
        /// The indicator font family. The default is Segoe UI.
        /// </value>
        public FontFamily InfoBubbleFontFamily
        {
            get => (FontFamily)GetValue(InfoBubbleFontFamilyProperty);
            set => SetValue(InfoBubbleFontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the indicator content font size.
        /// </summary>
        /// <value>
        /// The indicator font size. The default is 10 device-independent units.
        /// </value>
        public double InfoBubbleFontSize
        {
            get => (double)GetValue(InfoBubbleFontSizeProperty);
            set => SetValue(InfoBubbleFontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight used by the indicator content.
        /// </summary>
        /// <value>
        /// The indicator font weight. The default is <see cref="FontWeights.Normal"/>.
        /// </value>
        public FontWeight InfoBubbleFontWeight
        {
            get => (FontWeight)GetValue(InfoBubbleFontWeightProperty);
            set => SetValue(InfoBubbleFontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum width of the indicator.
        /// </summary>
        /// <value>
        /// The minimum indicator width. The default is 16 device-independent units.
        /// </value>
        public double InfoBubbleMinWidth
        {
            get => (double)GetValue(InfoBubbleMinWidthProperty);
            set => SetValue(InfoBubbleMinWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum height of the indicator.
        /// </summary>
        /// <value>
        /// The minimum indicator height. The default is 16 device-independent units.
        /// </value>
        public double InfoBubbleMinHeight
        {
            get => (double)GetValue(InfoBubbleMinHeightProperty);
            set => SetValue(InfoBubbleMinHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the padding around the indicator content.
        /// </summary>
        /// <value>
        /// The indicator padding. The default is 4 units horizontally and 2 units vertically.
        /// </value>
        public Thickness InfoBubblePadding
        {
            get => (Thickness)GetValue(InfoBubblePaddingProperty);
            set => SetValue(InfoBubblePaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the indicator is visible.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the indicator is visible; otherwise, <see langword="false"/>.
        /// The default is <see langword="true"/>.
        /// </value>
        public bool IsInfoBubbleVisible
        {
            get => (bool)GetValue(IsInfoBubbleVisibleProperty);
            set => SetValue(IsInfoBubbleVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the indicator is displayed as a dot.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to display a dot without content; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool IsDot
        {
            get => (bool)GetValue(IsDotProperty);
            set => SetValue(IsDotProperty, value);
        }

        /// <summary>
        /// Gets or sets the largest numeric value displayed without a plus suffix.
        /// </summary>
        /// <value>
        /// The largest directly displayed value, or 0 to disable truncation. The default is 0.
        /// </value>
        public int InfoBubbleMaxValue
        {
            get => (int)GetValue(InfoBubbleMaxValueProperty);
            set => SetValue(InfoBubbleMaxValueProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a numeric zero hides the indicator.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to hide the indicator when its content is zero; otherwise,
        /// <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool HideWhenZero
        {
            get => (bool)GetValue(HideWhenZeroProperty);
            set => SetValue(HideWhenZeroProperty, value);
        }

        /// <summary>
        /// Gets the formatted content currently displayed in the indicator.
        /// </summary>
        /// <value>
        /// The content after applying dot and maximum-value display rules.
        /// </value>
        public object? DisplayInfoBubbleContent => GetValue(DisplayInfoBubbleContentProperty);

        /// <summary>
        /// Gets a value that indicates whether the indicator satisfies all visibility conditions.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the indicator should be visible; otherwise, <see langword="false"/>.
        /// </value>
        public bool ComputedInfoBubbleVisibility => (bool)GetValue(ComputedInfoBubbleVisibilityProperty);

        private static void OnInfoBubbleMarginChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is not InfoBubble infoBubble || eventArgs.NewValue is not Thickness margin)
            {
                return;
            }

            infoBubble.SetValue(
                InfoBubbleOverhangMarginPropertyKey,
                new Thickness(
                    Math.Max(0, -margin.Left),
                    Math.Max(0, -margin.Top),
                    Math.Max(0, -margin.Right),
                    Math.Max(0, -margin.Bottom)));

            infoBubble.SetValue(
                InfoBubbleIndicatorMarginPropertyKey,
                new Thickness(
                    Math.Max(0, margin.Left),
                    Math.Max(0, margin.Top),
                    Math.Max(0, margin.Right),
                    Math.Max(0, margin.Bottom)));
        }

        private static void OnInfoBubbleDisplayPropertiesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is InfoBubble infoBubble)
            {
                infoBubble.UpdateDisplayProperties();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            UpdateDisplayProperties();
        }

        private void UpdateDisplayProperties()
        {
            if (IsDot)
            {
                SetValue(DisplayInfoBubbleContentPropertyKey, null);
                SetValue(ComputedInfoBubbleVisibilityPropertyKey, IsInfoBubbleVisible);
                return;
            }

            var numericValue = GetNumericValue(InfoBubbleContent);

            if (HideWhenZero && numericValue == 0)
            {
                SetValue(ComputedInfoBubbleVisibilityPropertyKey, false);
                SetValue(DisplayInfoBubbleContentPropertyKey, InfoBubbleContent);
                return;
            }

            var displayContent = InfoBubbleMaxValue > 0 && numericValue > InfoBubbleMaxValue
                ? $"{InfoBubbleMaxValue}+"
                : InfoBubbleContent;

            SetValue(DisplayInfoBubbleContentPropertyKey, displayContent);
            SetValue(ComputedInfoBubbleVisibilityPropertyKey, IsInfoBubbleVisible && HasValidContent(InfoBubbleContent));
        }

        private static bool HasValidContent(object? content)
        {
            return content switch
            {
                null => false,
                string stringValue => !string.IsNullOrEmpty(stringValue),
                _ => true
            };
        }

        private static int? GetNumericValue(object? content)
        {
            return content switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                double doubleValue => (int)doubleValue,
                string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
                _ => null
            };
        }
    }
}
