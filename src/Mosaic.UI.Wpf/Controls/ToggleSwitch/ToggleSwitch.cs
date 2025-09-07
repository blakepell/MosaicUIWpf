using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    [TemplatePart(Name = PartThumb, Type = typeof(Border))]
    [TemplatePart(Name = PartOnTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PartOffTextBlock, Type = typeof(TextBlock))]
    public class ToggleSwitch : Control
    {
        #region Private Fields

        private const string PartThumb = "PART_Thumb";
        private const string PartOnTextBlock = "PART_OnTextBlock";
        private const string PartOffTextBlock = "PART_OffTextBlock";

        private Border? _thumb;
        private Canvas? _canvasParent;
        private TextBlock? _onTextBlock;
        private TextBlock? _offTextBlock;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
            nameof(IsOn), typeof(bool), typeof(ToggleSwitch), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOnChanged));

        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ts = (ToggleSwitch)d;
            ts?.UpdateThumbPosition(true);
        }

        public static readonly DependencyProperty OnTextProperty = DependencyProperty.Register(
            nameof(OnText), typeof(string), typeof(ToggleSwitch), new PropertyMetadata("ON"));

        public string OnText
        {
            get => (string)GetValue(OnTextProperty);
            set => SetValue(OnTextProperty, value);
        }

        public static readonly DependencyProperty OffTextProperty = DependencyProperty.Register(
            nameof(OffText), typeof(string), typeof(ToggleSwitch), new PropertyMetadata("OFF"));

        public string OffText
        {
            get => (string)GetValue(OffTextProperty);
            set => SetValue(OffTextProperty, value);
        }

        public static readonly DependencyProperty OnBackgroundBrushProperty = DependencyProperty.Register(nameof(OnBackgroundBrush), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.Green));

        public Brush OnBackgroundBrush
        {
            get => (Brush)GetValue(OnBackgroundBrushProperty);
            set => SetValue(OnBackgroundBrushProperty, value);
        }

        public static readonly DependencyProperty OffBackgroundBrushProperty = DependencyProperty.Register(nameof(OffBackgroundBrush), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.DarkGray));

        public Brush OffBackgroundBrush
        {
            get => (Brush)GetValue(OffBackgroundBrushProperty);
            set => SetValue(OffBackgroundBrushProperty, value);
        }

        #endregion

        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
            FocusableProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(true));
            WidthProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(60.0));
            HeightProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(28.0));
        }

        public ToggleSwitch()
        {
            Cursor = Cursors.Hand;
        }

        private void RefreshUI()
        {
            this.Background = IsOn ? OnBackgroundBrush : OffBackgroundBrush;

            if (_onTextBlock != null)
            {
                _onTextBlock.Visibility = IsOn ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_offTextBlock != null)
            {
                _offTextBlock.Visibility = IsOn ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Unsubscribe previous handlers if any
            if (_thumb != null)
            {
                _thumb.Loaded -= OnThumbLoaded;
            }

            _thumb = GetTemplateChild(PartThumb) as Border;
            _canvasParent = _thumb?.Parent as Canvas;

            if (_thumb != null)
            {
                _thumb.Loaded += OnThumbLoaded;
            }

            if (_canvasParent != null)
            {
                _canvasParent.SizeChanged -= OnCanvasSizeChanged;
                _canvasParent.SizeChanged += OnCanvasSizeChanged;
            }

            _onTextBlock = GetTemplateChild(PartOnTextBlock) as TextBlock;
            _offTextBlock = GetTemplateChild(PartOffTextBlock) as TextBlock;

            // Handle clicks anywhere on the control
            this.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;

            // Set initial position without animation
            this.UpdateThumbPosition(false);

            // Refresh colors, etc.
            this.RefreshUI();
        }

        private void OnThumbLoaded(object? sender, RoutedEventArgs e)
        {
            UpdateThumbPosition(false);
        }

        private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Recalculate target positions when container resizes
            UpdateThumbPosition(false);
        }

        private void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // Toggle state on click anywhere within the control
            IsOn = !IsOn;
            e.Handled = true;

            this.RefreshUI();
        }

        private void UpdateThumbPosition(bool animate)
        {
            if (_thumb == null || _canvasParent == null)
            {
                return;
            }

            // Ensure we have measured sizes
            if (_canvasParent.ActualWidth <= 0 || _thumb.ActualWidth <= 0)
            {
                return;
            }

            // The template sets Margin="4" on the thumb; when using Canvas.Left the actual left edge is Canvas.Left + Margin.Left.
            // We want the thumb's left edge to be 4 when off, and (parentWidth - thumbWidth - 4) when on.
            // Therefore Canvas.Left (value we set) should be desiredLeft - marginLeft.

            double marginLeft = _thumb.Margin.Left;

            double offLeft = 0; // desired left edge 4 => Canvas.Left = 0 because Margin.Left == 4
            double desiredOnLeftEdge = _canvasParent.ActualWidth - _thumb.ActualWidth - 4;
            double onLeft = Math.Max(0, desiredOnLeftEdge - marginLeft);

            double target = IsOn ? onLeft : offLeft;

            if (animate)
            {
                var animation = new DoubleAnimation
                {
                    To = target,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                _thumb.BeginAnimation(Canvas.LeftProperty, animation);
            }
            else
            {
                _thumb.BeginAnimation(Canvas.LeftProperty, null); // stop any animation
                Canvas.SetLeft(_thumb, target);
            }
        }
    }
}
