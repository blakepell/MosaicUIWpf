/*
 * The RadialProgressBar control derives from:
 *
 * Alexander Smirnov
 * https://github.com/panthernet/XamlRadialProgressBar
 * Apache-2.0 license
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a <see cref="ProgressBar"/> that renders its value as a circular arc, pie, or a
    /// ring of discrete shapes.
    /// </summary>
    /// <remarks>
    /// The visual is produced by an internal <see cref="Arc"/> element named <c>PART_Arc</c> in the
    /// control template. The properties exposed here are forwarded to that element and control the
    /// arc geometry, fill, backgrounds, and the indeterminate animation.
    /// </remarks>
    public class RadialProgressBar : ProgressBar
    {
        private Arc? _arc;

        /// <summary>
        /// Gets or sets the brush used to fill the area inside the arc.
        /// </summary>
        /// <value>The brush painted behind the arc, inside its inner edge. The default is <see cref="Brushes.Transparent"/>.</value>
        public Brush InnerBackgroundBrush
        {
            get => (Brush)GetValue(InnerBackgroundBrushProperty);
            set => SetValue(InnerBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="InnerBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InnerBackgroundBrushProperty =
            DependencyProperty.Register(nameof(InnerBackgroundBrush), typeof(Brush), typeof(RadialProgressBar),
                new UIPropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// Gets or sets the brush used to paint the unfilled portion of the arc track.
        /// </summary>
        /// <value>The brush painted along the full sweep of the arc behind the progress fill. The default is <see cref="Brushes.Transparent"/>.</value>
        public Brush OuterBackgroundBrush
        {
            get => (Brush)GetValue(OuterBackgroundBrushProperty);
            set => SetValue(OuterBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OuterBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OuterBackgroundBrushProperty =
            DependencyProperty.Register(nameof(OuterBackgroundBrush), typeof(Brush), typeof(RadialProgressBar),
                new UIPropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// Gets or sets the thickness, in device-independent units, of the progress arc.
        /// </summary>
        /// <value>The width of the arc stroke. The default is <c>10</c>.</value>
        public double ArcWidth
        {
            get => (double)GetValue(ArcWidthProperty);
            set => SetValue(ArcWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ArcWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArcWidthProperty =
            DependencyProperty.Register(nameof(ArcWidth), typeof(double), typeof(RadialProgressBar),
                new UIPropertyMetadata(10d));

        /// <summary>
        /// Gets or sets the thickness, in device-independent units, of the background arc track.
        /// </summary>
        /// <value>The width of the background track. A value of <c>0</c> auto-sizes the track to the arc. The default is <c>0</c>.</value>
        public double ArcBackgroundWidth
        {
            get => (double)GetValue(ArcBackgroundWidthProperty);
            set => SetValue(ArcBackgroundWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ArcBackgroundWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArcBackgroundWidthProperty =
            DependencyProperty.Register(nameof(ArcBackgroundWidth), typeof(double), typeof(RadialProgressBar),
                new UIPropertyMetadata(0d));

        /// <summary>
        /// Gets or sets the angle, in degrees, at which the arc begins.
        /// </summary>
        /// <value>The starting rotation of the arc. <c>270</c> starts at the top for a clockwise arc; <c>90</c> starts at the top for a counterclockwise arc. The default is <c>270</c>.</value>
        public double ArcRotationDegree
        {
            get => (double)GetValue(ArcRotationDegreeProperty);
            set => SetValue(ArcRotationDegreeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ArcRotationDegree"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArcRotationDegreeProperty =
            DependencyProperty.Register(nameof(ArcRotationDegree), typeof(double), typeof(RadialProgressBar),
                new UIPropertyMetadata(270d));

        /// <summary>
        /// Identifies the <see cref="ArcMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArcModeProperty =
            DependencyProperty.Register(nameof(ArcMode), typeof(ArcMode), typeof(RadialProgressBar),
                new UIPropertyMetadata(ArcMode.Fill));

        /// <summary>
        /// Gets or sets a value that indicates how the progress is rendered.
        /// </summary>
        /// <value>One of the enumeration values that specifies whether the progress is drawn as a filled arc, a pie, or a ring of shapes. The default is <see cref="ArcMode.Fill"/>.</value>
        public ArcMode ArcMode
        {
            get => (ArcMode)GetValue(ArcModeProperty);
            set => SetValue(ArcModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the direction in which the arc sweeps from its starting angle.
        /// </summary>
        /// <value>One of the enumeration values that specifies the sweep direction. The default is <see cref="SweepDirection.Clockwise"/>.</value>
        public SweepDirection ArcDirection
        {
            get => (SweepDirection)GetValue(ArcDirectionProperty);
            set => SetValue(ArcDirectionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ArcDirection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArcDirectionProperty =
            DependencyProperty.Register(nameof(ArcDirection), typeof(SweepDirection), typeof(RadialProgressBar),
                new UIPropertyMetadata(SweepDirection.Clockwise));

        /// <summary>
        /// Gets or sets the width, in device-independent units, of each shape drawn in <see cref="ArcMode.Shape"/> mode.
        /// </summary>
        /// <value>The width of an individual shape. The default is <c>1</c>.</value>
        public double ShapeModeWidth
        {
            get => (double)GetValue(ShapeModeWidthProperty);
            set => SetValue(ShapeModeWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeWidthProperty =
            DependencyProperty.Register(nameof(ShapeModeWidth), typeof(double), typeof(RadialProgressBar),
                new UIPropertyMetadata(1d));

        /// <summary>
        /// Gets or sets the angular spacing, in degrees, between shapes drawn in <see cref="ArcMode.Shape"/> mode.
        /// </summary>
        /// <value>The step between shapes, in the range 0 to 360. The default is <c>3</c>.</value>
        public int ShapeModeStep
        {
            get => (int)GetValue(ShapeModeStepProperty);
            set => SetValue(ShapeModeStepProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeStepProperty =
            DependencyProperty.Register(nameof(ShapeModeStep), typeof(int), typeof(RadialProgressBar),
                new UIPropertyMetadata(3));

        /// <summary>
        /// Gets or sets the shape drawn at each step in <see cref="ArcMode.Shape"/> mode.
        /// </summary>
        /// <value>One of the enumeration values that specifies the shape. The default is <see cref="ArcShape.Rectangle"/>.</value>
        public ArcShape ShapeModeShape
        {
            get => (ArcShape)GetValue(ShapeModeShapeProperty);
            set => SetValue(ShapeModeShapeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeShape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeShapeProperty =
            DependencyProperty.Register(nameof(ShapeModeShape), typeof(ArcShape), typeof(RadialProgressBar),
                new UIPropertyMetadata(ArcShape.Rectangle));

        /// <summary>
        /// Gets or sets a value that indicates whether the trailing shapes fade out in <see cref="ArcMode.Shape"/> mode.
        /// </summary>
        /// <value><see langword="true"/> to fade the last few shapes toward transparency; otherwise, <see langword="false"/>. The default is <see langword="true"/>.</value>
        public bool ShapeModeUseFade
        {
            get => (bool)GetValue(ShapeModeUseFadeProperty);
            set => SetValue(ShapeModeUseFadeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeUseFade"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeUseFadeProperty =
            DependencyProperty.Register(nameof(ShapeModeUseFade), typeof(bool), typeof(RadialProgressBar),
                new UIPropertyMetadata(true));

        /// <summary>
        /// Gets or sets an additional rotation, in degrees, applied to each shape in <see cref="ArcMode.Shape"/> mode.
        /// </summary>
        /// <value>The extra rotation applied to every shape. The default is <c>0</c>.</value>
        public double ShapeRotationAdjustment
        {
            get => (double)GetValue(ShapeRotationAdjustmentProperty);
            set => SetValue(ShapeRotationAdjustmentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeRotationAdjustment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeRotationAdjustmentProperty =
            DependencyProperty.Register(nameof(ShapeRotationAdjustment), typeof(double), typeof(RadialProgressBar),
                new UIPropertyMetadata(0d));

        /// <summary>
        /// Gets or sets the speed multiplier for the animation shown while the control is indeterminate.
        /// </summary>
        /// <value>The multiplier applied to the animation timer interval; larger values animate faster. The default is <c>1</c>.</value>
        public double IndeterminateSpeedRatio
        {
            get => (double)GetValue(IndeterminateSpeedRatioProperty);
            set => SetValue(IndeterminateSpeedRatioProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IndeterminateSpeedRatio"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IndeterminateSpeedRatioProperty =
            DependencyProperty.Register(nameof(IndeterminateSpeedRatio), typeof(double), typeof(RadialProgressBar),
                new UIPropertyMetadata(1d));

        /// <summary>
        /// Initializes a new instance of the <see cref="RadialProgressBar"/> class.
        /// </summary>
        public RadialProgressBar()
        {
            DefaultStyleKey = typeof(RadialProgressBar);
            SizeChanged += RadialProgressBar_SizeChanged;
        }

        /// <summary>
        /// Recalculates the shape positions when the control is resized while in <see cref="ArcMode.Shape"/> mode.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data describing the size change.</param>
        private void RadialProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_arc == null)
            {
                return;
            }

            if (_arc.ArcMode == ArcMode.Shape)
            {
                _arc.RecalculateShapes();
            }
        }

        /// <summary>
        /// Builds the visual tree from the control template and caches the internal <see cref="Arc"/> element.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _arc = (Arc)Template.FindName("PART_Arc", this);
        }
    }
}
