using System.Windows.Shapes;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A <see cref="Shape"/> that draws a circular arc, pie, or ring of shapes between a start and end
    /// angle. It is the rendering primitive used by <see cref="RadialProgressBar"/>.
    /// </summary>
    public class Arc : Shape
    {
        /// <summary>
        /// Gets or sets additional shape rotation adjustment in degrees for Shape mode. Default value is 0d.
        /// </summary>
        public double ShapeRotationAdjustment
        {
            get => (double)GetValue(ShapeRotationAdjustmentProperty);
            set => SetValue(ShapeRotationAdjustmentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeRotationAdjustment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeRotationAdjustmentProperty =
            DependencyProperty.Register(nameof(ShapeRotationAdjustment), typeof(double), typeof(Arc),
                new UIPropertyMetadata(0d, UpdateArc));

        /// <summary>
        /// Gets or sets shape fading effect for Shape mode. Default value is True.
        /// </summary>
        public bool ShapeModeUseFade
        {
            get => (bool)GetValue(ShapeModeUseFadeProperty);
            set => SetValue(ShapeModeUseFadeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeUseFade"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeUseFadeProperty =
            DependencyProperty.Register(nameof(ShapeModeUseFade), typeof(bool), typeof(Arc),
                new UIPropertyMetadata(true, UpdateArc));

        /// <summary>
        /// Gets or sets default shape for Shape mode. Default value is Rectangle.
        /// </summary>
        public ArcShape ShapeModeShape
        {
            get => (ArcShape)GetValue(ShapeModeShapeProperty);
            set => SetValue(ShapeModeShapeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeShape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeShapeProperty =
            DependencyProperty.Register(nameof(ShapeModeShape), typeof(ArcShape), typeof(Arc),
                new UIPropertyMetadata(ArcShape.Rectangle, UpdateArc));

        /// <summary>
        /// Gets or sets shape width for Shape mode. Default value is 1d.
        /// </summary>
        public double ShapeModeWidth
        {
            get => (double)GetValue(ShapeModeWidthProperty);
            set => SetValue(ShapeModeWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeWidthProperty =
            DependencyProperty.Register(nameof(ShapeModeWidth), typeof(double), typeof(Arc),
                new UIPropertyMetadata(1d, UpdateArc));

        /// <summary>
        /// Gets or sets step in degrees for Shape mode. Degree range is 0 - 360. Default step is 3.
        /// </summary>
        public int ShapeModeStep
        {
            get => (int)GetValue(ShapeModeStepProperty);
            set => SetValue(ShapeModeStepProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShapeModeStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeModeStepProperty =
            DependencyProperty.Register(nameof(ShapeModeStep), typeof(int), typeof(Arc),
                new UIPropertyMetadata(3, UpdateArc));

        /// <summary>
        /// Identifies the <see cref="ArcMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArcModeProperty =
            DependencyProperty.Register(nameof(ArcMode), typeof(ArcMode), typeof(Arc),
                new UIPropertyMetadata(ArcMode.Fill, UpdateArc));

        /// <summary>
        /// Gets or sets the mode of the progress bar
        /// </summary>
        public ArcMode ArcMode
        {
            get => (ArcMode)GetValue(ArcModeProperty);
            set => SetValue(ArcModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressBorderThicknessProperty =
            DependencyProperty.Register(nameof(ProgressBorderThickness), typeof(Thickness), typeof(Arc),
                new UIPropertyMetadata(new Thickness(0), UpdatePen));

        /// <summary>
        /// Gets or sets the thickness of the fill border
        /// </summary>
        public Thickness ProgressBorderThickness
        {
            get => (Thickness)GetValue(ProgressBorderThicknessProperty);
            set => SetValue(ProgressBorderThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressFillBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressFillBrushProperty =
            DependencyProperty.Register(nameof(ProgressFillBrush), typeof(Brush), typeof(Arc),
                new UIPropertyMetadata(Brushes.White, UpdateFillBrush));

        /// <summary>
        /// Gets or sets the brush for the fill part
        /// </summary>
        public Brush ProgressFillBrush
        {
            get => (Brush)GetValue(ProgressFillBrushProperty);
            set => SetValue(ProgressFillBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressBackgroundBrushProperty =
            DependencyProperty.Register(nameof(ProgressBackgroundBrush), typeof(Brush), typeof(Arc),
                new UIPropertyMetadata(Brushes.Transparent, UpdateBgArc));

        /// <summary>
        /// Updates the background pen and invalidates the visual when the background brush changes.
        /// </summary>
        /// <param name="d">The <see cref="Arc"/> whose property changed.</param>
        /// <param name="e">The event data describing the property change.</param>
        private static void UpdateBgArc(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Arc)d).UpdateBackgroundPen();
            ((Arc)d).InvalidateVisual();
        }

        /// <summary>
        /// Gets or sets the brush used to paint the background track behind the fill.
        /// </summary>
        public Brush ProgressBackgroundBrush
        {
            get => (Brush)GetValue(ProgressBackgroundBrushProperty);
            set => SetValue(ProgressBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressBorderBrushProperty =
            DependencyProperty.Register(nameof(ProgressBorderBrush), typeof(Brush), typeof(Arc),
                new UIPropertyMetadata(Brushes.Transparent, UpdatePen));

        /// <summary>
        /// Rebuilds the pens and invalidates the visual when a pen-related property changes.
        /// </summary>
        /// <param name="d">The <see cref="Arc"/> whose property changed.</param>
        /// <param name="e">The event data describing the property change.</param>
        private static void UpdatePen(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Arc)d).UpdatePen();
            UpdateArc(d, e);
        }

        /// <summary>
        /// Gets or sets the fill border brush.
        /// </summary>
        public Brush ProgressBorderBrush
        {
            get => (Brush)GetValue(ProgressBorderBrushProperty);
            set => SetValue(ProgressBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the angle, in degrees, at which the arc fill begins.
        /// </summary>
        public double StartAngle
        {
            get => (double)GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StartAngle"/> dependency property. This enables animation, styling, binding, and so on.
        /// </summary>
        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register(nameof(StartAngle), typeof(double), typeof(Arc),
                new UIPropertyMetadata(0.0, UpdateArc));

        /// <summary>
        /// Gets or sets the angle, in degrees, at which the arc fill ends.
        /// </summary>
        public double EndAngle
        {
            get => (double)GetValue(EndAngleProperty);
            set => SetValue(EndAngleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EndAngle"/> dependency property. This enables animation, styling, binding, and so on.
        /// </summary>
        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register(nameof(EndAngle), typeof(double), typeof(Arc),
                new UIPropertyMetadata(90.0, UpdateArc));

        /// <summary>
        /// Gets or sets progress bar fill direction
        /// </summary>
        public SweepDirection Direction
        {
            get => (SweepDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Direction"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(nameof(Direction), typeof(SweepDirection), typeof(Arc),
                new UIPropertyMetadata(SweepDirection.Clockwise));

        /// <summary>
        /// Gets or sets the initial rotation of the arc a certain number of degree in the direction. 270 - clockwise, 90 - counterclockwise.
        /// </summary>
        public double OriginRotationDegrees
        {
            get => (double)GetValue(OriginRotationDegreesProperty);
            set => SetValue(OriginRotationDegreesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OriginRotationDegrees"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OriginRotationDegreesProperty =
            DependencyProperty.Register(nameof(OriginRotationDegrees), typeof(double), typeof(Arc),
                new UIPropertyMetadata(270d, UpdateArc));

        /// <summary>
        /// Invalidates the visual of the supplied <see cref="Arc"/> so it is redrawn.
        /// </summary>
        /// <param name="d">The <see cref="Arc"/> to invalidate.</param>
        /// <param name="e">The event data describing the property change.</param>
        protected static void UpdateArc(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Arc)d)?.InvalidateVisual();
        }

        /// <summary>
        /// Gets or sets the Indeterminate animation state
        /// </summary>
        public bool IsIndeterminate
        {
            get => (bool)GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsIndeterminate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(Arc),
                new UIPropertyMetadata(true, UpdateIndeterminate));

        /// <summary>
        /// Starts or stops the indeterminate animation and invalidates the visual when the state changes.
        /// </summary>
        /// <param name="d">The <see cref="Arc"/> whose property changed.</param>
        /// <param name="e">The event data describing the property change.</param>
        private static void UpdateIndeterminate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Arc)d).UpdateIndeterminate();

            UpdateArc(d, e);
        }

        private DispatcherTimer? _inTimer;
        private bool _inEnd;

        /// <summary>
        /// Starts or stops the indeterminate animation. A <see cref="DispatcherTimer"/> is used so the
        /// animation runs on the UI thread; this avoids the per-tick cross-thread marshaling that the
        /// old <see cref="System.Timers.Timer"/> required (its <c>Elapsed</c> handler fired on a
        /// thread-pool thread and had to hop back via <c>Dispatcher.InvokeAsync</c>).
        /// </summary>
        private void UpdateIndeterminate()
        {
            if (!IsIndeterminate)
            {
                if (_inTimer != null)
                {
                    _inTimer.Stop();
                    _inTimer = null;
                    SetCurrentValue(EndAngleProperty, StartAngle);
                }
            }
            else
            {
                _inTimer?.Stop();
                _inEnd = false;
                _inTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(100 * IndeterminateSpeedRatio)
                };
                _inTimer.Tick += (sender, args) =>
                {
                    var value = (double)GetValue(EndAngleProperty) + 9;
                    if (_inEnd)
                    {
                        _inEnd = false;
                        value = 0;
                        SetCurrentValue(EndAngleProperty, value);
                    }
                    else if (value >= 360)
                    {
                        value = 359.999;
                        SetCurrentValue(EndAngleProperty, value);
                        _inEnd = true;
                    }
                    else
                    {
                        SetCurrentValue(EndAngleProperty, value);
                    }
                };
                _inTimer.Start();
            }
        }

        /// <summary>
        /// Gets or sets speed ration for Indeterminate state animation. Default value is 1.
        /// </summary>
        public double IndeterminateSpeedRatio
        {
            get => (double)GetValue(IndeterminateSpeedRatioProperty);
            set => SetValue(IndeterminateSpeedRatioProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IndeterminateSpeedRatio"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IndeterminateSpeedRatioProperty =
            DependencyProperty.Register(nameof(IndeterminateSpeedRatio), typeof(double), typeof(Arc),
                new UIPropertyMetadata(1d, (o, args) => (o as Arc)?.UpdateIndeterminate()));

        /// <summary>
        /// Gets or sets progress bar background circle width. Default value is 0 - auto size.
        /// </summary>
        public double ArcBackgroundWidth
        {
            get => (double)GetValue(ArcBackgroundWidthProperty);
            set => SetValue(ArcBackgroundWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ArcBackgroundWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArcBackgroundWidthProperty =
            DependencyProperty.Register(nameof(ArcBackgroundWidth), typeof(double), typeof(Arc),
                new UIPropertyMetadata(0d, UpdateBgArc));

        /// <summary>
        /// Initializes a new instance of the <see cref="Arc"/> class.
        /// </summary>
        public Arc()
        {
            Loaded += (sender, args) =>
            {
                UpdateIndeterminate();
            };

            // Stop the indeterminate timer when detached so it does not keep the control alive.
            Unloaded += (sender, args) =>
            {
                _inTimer?.Stop();
                _inTimer = null;
            };
        }

        /// <summary>
        /// Gets the geometry that defines the arc to be drawn.
        /// </summary>
        protected override Geometry DefiningGeometry => GetArcGeometry();

        /// <summary>
        /// Draws the arc, pie, or shape ring into the supplied drawing context.
        /// </summary>
        /// <param name="drawingContext">The drawing context to render into.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            //update pens on first pass
            if (_pen == null)
            {
                UpdatePen();
                UpdateBackgroundPen();
            }

            //default and arc modes
            if (ArcMode == ArcMode.Fill || ArcMode == ArcMode.Pie)
            {
                //calculate radius
                var radiusX = RenderSize.Width / 2;
                var radiusY = RenderSize.Height / 2;
                var centerPoint = new Point(radiusX, radiusY);

                //if background is set
                if (ProgressBackgroundBrush != Brushes.Transparent)
                {
                    //get clip geometry
                    var clipb = GetArcGeometry(true).GetWidenedPathGeometry(
                        _bgClipPen);
                    if (clipb.CanFreeze)
                    {
                        clipb.Freeze();
                    }

                    //apply clip
                    drawingContext.PushClip(clipb);
                    //fill background
                    drawingContext.DrawEllipse(ProgressBackgroundBrush, null, centerPoint, radiusX, radiusY);
                    drawingContext.Pop();
                }

                //get clip of the progress arc
                var clip = GetArcGeometry().GetWidenedPathGeometry(_clipPen);
                if (clip.CanFreeze)
                {
                    clip.Freeze();
                }

                //apply clip
                drawingContext.PushClip(clip);
                //fill progress area
                drawingContext.DrawEllipse(ProgressFillBrush, null, centerPoint, radiusX, radiusY);
                //draw outline border if any
                if (ProgressBorderBrush != Brushes.Transparent && ProgressBorderThickness.Top > 0)
                {
                    drawingContext.DrawGeometry(null, _pen, clip);
                }

                drawingContext.Pop();
            }
            else
            {
                //SHAPE MODE
                //update brushes on first pass
                if (_semiBrush1 == null)
                {
                    UpdateInternalBrushes();
                }

                //precalculate all angles for performance reasons. This is intentionally kept
                //independent of the brush cache above: a theme change re-resolves the fill brush and
                //populates _semiBrush1 via UpdateInternalBrushes without ever building _data, so it
                //must be (re)built here whenever it is missing or it would be indexed while null.
                if (_data == null)
                {
                    _data = GetAngleData();
                }

                var maxAngle = Math.Max(StartAngle, EndAngle);
                //get number of draw passes to process, clamped to the precalculated data so we never
                //index past the end of the list (rounding can otherwise overshoot by one step).
                var max = (int)Math.Min(Math.Round(maxAngle / ShapeModeStep), _data.Count);

                for (int i = 0; i < max; i++)
                {
                    var data = _data[i];
                    var half = ShapeModeWidth * .5;
                    var rect = new Rect(data.StartPoint.X - half, data.StartPoint.Y, ShapeModeWidth, StrokeThickness);
                    //apply rotation to context
                    drawingContext.PushTransform(new RotateTransform(data.Angle, data.StartPoint.X, data.StartPoint.Y));
                    var brush = ProgressFillBrush;
                    //apply transparency
                    if (ShapeModeUseFade)
                    {
                        if (i == max - 2)
                        {
                            brush = _semiBrush1;
                        }

                        if (i == max - 1)
                        {
                            brush = _semiBrush2;
                        }
                        else if (i == max)
                        {
                            brush = _semiBrush3;
                        }
                    }
                    //draw shape
                    drawingContext.DrawRectangle(brush, _pen, rect);
                    drawingContext.Pop();
                }
            }
        }

        private List<AngleData> _data;
        private Pen _pen;
        private Pen _clipPen;
        private Pen _bgClipPen;
        private Brush _semiBrush1;
        private Brush _semiBrush2;
        private Brush _semiBrush3;

        /// <summary>
        /// Rebuilds the cached fade brushes and invalidates the visual when the fill brush changes.
        /// </summary>
        /// <param name="d">The <see cref="Arc"/> whose property changed.</param>
        /// <param name="e">The event data describing the property change.</param>
        private static void UpdateFillBrush(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var arc = d as Arc;
            arc?.UpdateInternalBrushes();
            UpdateArc(d, e);
        }

        /// <summary>
        /// Recreates the frozen, semi-transparent brushes used to fade the trailing shapes in Shape mode.
        /// </summary>
        internal void UpdateInternalBrushes()
        {
            if (ShapeModeUseFade && ProgressFillBrush != null)
            {
                _semiBrush1 = ProgressFillBrush.Clone();
                _semiBrush1.Opacity = .7;
                if (_semiBrush1.CanFreeze)
                {
                    _semiBrush1.Freeze();
                }

                _semiBrush2 = ProgressFillBrush.Clone();
                _semiBrush2.Opacity = .5;
                if (_semiBrush2.CanFreeze)
                {
                    _semiBrush2.Freeze();
                }

                _semiBrush3 = ProgressFillBrush.Clone();
                _semiBrush3.Opacity = .3;
                if (_semiBrush3.CanFreeze)
                {
                    _semiBrush3.Freeze();
                }
            }
        }

        /// <summary>
        /// Returns the inverse of the horizontal DPI scale for the current visual. Uses
        /// <see cref="VisualTreeHelper.GetDpi"/> which is safe to call even when the element is not
        /// connected to a presentation source (e.g. during a theme switch when the control is being
        /// re-templated). The previous implementation dereferenced
        /// <c>PresentationSource.FromVisual(this)</c>, which returns <c>null</c> for a detached
        /// visual and crashed when the theme changed.
        /// </summary>
        /// <returns>The reciprocal of the horizontal DPI scale factor for this element.</returns>
        private double GetDpiFactor()
        {
            return 1d / VisualTreeHelper.GetDpi(this).DpiScaleX;
        }

        /// <summary>
        /// Rebuilds the border pen and the progress clip pen, accounting for the current DPI.
        /// </summary>
        private void UpdatePen()
        {
            var width = ArcMode == ArcMode.Pie ? RenderSize.Width * .5 : StrokeThickness;

            var dpiFactor = GetDpiFactor();
            _pen = new Pen(ProgressBorderBrush, ProgressBorderThickness.Top * dpiFactor);
            if (_pen.CanFreeze)
            {
                _pen.Freeze();
            }

            _clipPen = new Pen(Brushes.White, width * dpiFactor);
            if (_clipPen.CanFreeze)
            {
                _clipPen.Freeze();
            }
        }

        /// <summary>
        /// Rebuilds the background clip pen, accounting for the current DPI.
        /// </summary>
        private void UpdateBackgroundPen()
        {
            var width = ArcBackgroundWidth == 0 ? RenderSize.Width * .5 : ArcBackgroundWidth;
            var dpiFactor = GetDpiFactor();

            _bgClipPen = new Pen(Brushes.White, width * dpiFactor);
            if (_bgClipPen.CanFreeze)
            {
                _bgClipPen.Freeze();
            }
        }

        /// <summary>
        /// Gets all the possible angles for shapes in shape mode
        /// </summary>
        /// <returns>A list of the position and rotation for every shape from the start angle around the full circle.</returns>
        private List<AngleData> GetAngleData()
        {
            var minAngle = Math.Min(StartAngle, EndAngle);
            var maxAngle = 359.999;

            var dic = new List<AngleData>();
            var startAngle = minAngle;
            var radiusX = RenderSize.Width / 2;
            var radiusY = RenderSize.Height / 2;
            var centerPoint = new Point(radiusX, radiusY);

            while (true)
            {
                if (startAngle > maxAngle)
                {
                    break;
                }

                var a = (Direction == SweepDirection.Clockwise ? -1 : 1) * (startAngle + OriginRotationDegrees) * (Math.PI / 180);

                var pt = new Point
                {
                    Y = centerPoint.Y - radiusY * Math.Sin(a),
                    X = centerPoint.X + radiusX * Math.Cos(a)
                };
                var a2 = GetAngleBetweenPoints(pt, centerPoint);
                dic.Add(new AngleData { StartPoint = pt, Angle = a2 });
                startAngle += ShapeModeStep;
            }

            return dic;
        }

        /// <summary>
        /// Holds the precalculated position and rotation of a single shape drawn in Shape mode.
        /// </summary>
        private class AngleData
        {
            /// <summary>
            /// The point at which the shape is drawn.
            /// </summary>
            public Point StartPoint;

            /// <summary>
            /// The rotation, in degrees, applied to the shape.
            /// </summary>
            public double Angle;
        }

        /// <summary>
        /// Returns the angle, in degrees, of the line from <paramref name="one"/> to <paramref name="two"/>,
        /// adjusted by <see cref="ShapeRotationAdjustment"/>.
        /// </summary>
        /// <param name="one">The first point.</param>
        /// <param name="two">The second point.</param>
        /// <returns>The adjusted angle, in degrees, between the two points.</returns>
        private double GetAngleBetweenPoints(Point one, Point two)
        {
            var xDiff = two.X - one.X;
            var yDiff = two.Y - one.Y;
            var angle = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
            return angle + ShapeRotationAdjustment - 90;
        }

        /// <summary>
        /// Builds the stream geometry for the arc between the current start and end angles.
        /// </summary>
        /// <param name="full"><see langword="true"/> to build the geometry for the full circle (used for the background track); otherwise, <see langword="false"/> to build it for the current progress span.</param>
        /// <returns>The geometry describing the arc.</returns>
        private Geometry GetArcGeometry(bool full = false)
        {
            var width = ArcMode == ArcMode.Pie ? RenderSize.Width * .5 : StrokeThickness;
            var startPoint = PointAtAngle(full ? 0d : Math.Min(StartAngle, EndAngle), Direction);
            var endPoint = PointAtAngle(full ? 359.999d : Math.Max(StartAngle, EndAngle), Direction);

            var arcSize = new Size(Math.Max(0, (RenderSize.Width - width) / 2),
                Math.Max(0, (RenderSize.Height - width) / 2));
            var isLargeArc = full || Math.Abs(EndAngle - StartAngle) > 180;

            var geom = new StreamGeometry();
            using (var context = geom.Open())
            {
                context.BeginFigure(startPoint, false, false);
                context.ArcTo(endPoint, arcSize, 0, isLargeArc, Direction, true, false);
            }

            geom.Transform = new TranslateTransform(width / 2, width / 2);
            return geom;
        }

        /// <summary>
        /// Returns point which corresponds to the angle of the circle
        /// </summary>
        /// <param name="angle">The angle, in degrees, measured from the origin rotation.</param>
        /// <param name="sweep">One of the enumeration values that specifies the sweep direction used to resolve the vertical position.</param>
        /// <returns>The point on the arc that corresponds to the supplied angle.</returns>
        private Point PointAtAngle(double angle, SweepDirection sweep)
        {
            var width = ArcMode == ArcMode.Pie ? RenderSize.Width * .5 : StrokeThickness;

            var translatedAngle = angle + OriginRotationDegrees;
            var radAngle = translatedAngle * (Math.PI / 180);
            var xr = (RenderSize.Width - width) / 2;
            var yr = (RenderSize.Height - width) / 2;

            var x = xr + xr * Math.Cos(radAngle);
            var y = yr * Math.Sin(radAngle);

            if (sweep == SweepDirection.Counterclockwise)
            {
                y = yr - y;
            }
            else
            {
                y = yr + y;
            }

            return new Point(x, y);
        }

        /// <summary>
        /// Recalculates the shapes angles data
        /// </summary>
        internal void RecalculateShapes()
        {
            _data = GetAngleData();
        }
    }
}
