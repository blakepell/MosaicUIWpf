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

using System.Globalization;
using System.Windows.Documents;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Arranges calendar-event presenters using continuous time coordinates and draws a 24-hour timeline.
    /// </summary>
    /// <remarks>
    /// This panel is normally supplied by the default <see cref="DayCalendarView"/> template. It remains public
    /// so replacement templates can preserve the built-in interval layout and timeline rendering.
    /// </remarks>
    public class DayTimelinePanel : Panel
    {
        private const double MinimumEventHeight = 2.0;
        private static readonly CultureInfo TimeCulture = CultureInfo.GetCultureInfo("en-US");
        private readonly List<CalendarEventLayoutItem> _layoutItems = [];
        private readonly List<DateTime> _columnEnds = [];

        /// <summary>
        /// Overlay visual used for the current time indicator and drag guides. It is reported as the last
        /// visual child so those guides paint above the arranged event presenters.
        /// </summary>
        private readonly GuideVisual _guideVisual = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DayTimelinePanel"/> class.
        /// </summary>
        public DayTimelinePanel()
        {
            this.AddVisualChild(_guideVisual);
        }

        /// <summary>
        /// Identifies the <see cref="HourHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourHeightProperty = DependencyProperty.Register(
            nameof(HourHeight), typeof(double), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(80.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="TimeColumnWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeColumnWidthProperty = DependencyProperty.Register(
            nameof(TimeColumnWidth), typeof(double), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(72.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="EventSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventSpacingProperty = DependencyProperty.Register(
            nameof(EventSpacing), typeof(double), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <see cref="ShowQuarterHourLines"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowQuarterHourLinesProperty = DependencyProperty.Register(
            nameof(ShowQuarterHourLines), typeof(bool), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="HourLineBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourLineBrushProperty = DependencyProperty.Register(
            nameof(HourLineBrush), typeof(Brush), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="QuarterHourLineBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty QuarterHourLineBrushProperty = DependencyProperty.Register(
            nameof(QuarterHourLineBrush), typeof(Brush), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="TimeForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeForegroundProperty = DependencyProperty.Register(
            nameof(TimeForeground), typeof(Brush), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="CurrentTimeBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentTimeBrushProperty = DependencyProperty.Register(
            nameof(CurrentTimeBrush), typeof(Brush), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="TimeColumnBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeColumnBackgroundProperty = DependencyProperty.Register(
            nameof(TimeColumnBackground), typeof(Brush), typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="FontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
            typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="FontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
            typeof(DayTimelinePanel),
            new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the vertical size allocated to each hour.
        /// </summary>
        public double HourHeight
        {
            get => (double)GetValue(HourHeightProperty);
            set => SetValue(HourHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the width reserved for time labels.
        /// </summary>
        public double TimeColumnWidth
        {
            get => (double)GetValue(TimeColumnWidthProperty);
            set => SetValue(TimeColumnWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the gap between adjacent event columns.
        /// </summary>
        public double EventSpacing
        {
            get => (double)GetValue(EventSpacingProperty);
            set => SetValue(EventSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether 15-minute subdivisions are drawn.
        /// </summary>
        public bool ShowQuarterHourLines
        {
            get => (bool)GetValue(ShowQuarterHourLinesProperty);
            set => SetValue(ShowQuarterHourLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for hour separators.
        /// </summary>
        public Brush HourLineBrush
        {
            get => (Brush)GetValue(HourLineBrushProperty);
            set => SetValue(HourLineBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for quarter-hour separators.
        /// </summary>
        public Brush QuarterHourLineBrush
        {
            get => (Brush)GetValue(QuarterHourLineBrushProperty);
            set => SetValue(QuarterHourLineBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for timeline labels.
        /// </summary>
        public Brush TimeForeground
        {
            get => (Brush)GetValue(TimeForegroundProperty);
            set => SetValue(TimeForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for current-time and drag-time guides.
        /// </summary>
        public Brush CurrentTimeBrush
        {
            get => (Brush)GetValue(CurrentTimeBrushProperty);
            set => SetValue(CurrentTimeBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush used by the time-label well.
        /// </summary>
        public Brush TimeColumnBackground
        {
            get => (Brush)GetValue(TimeColumnBackgroundProperty);
            set => SetValue(TimeColumnBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family used for time labels and drag indicators.
        /// </summary>
        public FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size used for time labels and drag indicators.
        /// </summary>
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        internal DayCalendarView? Owner { get; set; }

        internal void RefreshTimeline()
        {
            InvalidateMeasure();
            InvalidateVisual();
        }

        /// <inheritdoc />
        protected override int VisualChildrenCount => this.InternalChildren.Count + 1;

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index)
        {
            var childCount = this.InternalChildren.Count;

            if (index == childCount)
            {
                return _guideVisual;
            }

            if (index < 0 || index > childCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return this.InternalChildren[index];
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var width = double.IsInfinity(availableSize.Width)
                ? Math.Max(TimeColumnWidth + 320.0, MinWidth)
                : availableSize.Width;
            var height = Math.Max(0.0, HourHeight) * 24.0;

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(Math.Max(0.0, width - TimeColumnWidth), height));
            }

            return new Size(width, height);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            BuildLayoutItems();
            CalendarEventLayoutEngine.AssignColumns(_layoutItems, _columnEnds);

            var eventAreaLeft = Math.Max(0.0, TimeColumnWidth);
            var availableWidth = Math.Max(0.0, finalSize.Width - eventAreaLeft);
            var pixelsPerMinute = Math.Max(0.0, HourHeight) / 60.0;
            var day = Owner?.SelectedDate.Date ?? DateTime.Today;

            foreach (var item in _layoutItems)
            {
                var columnWidth = availableWidth / item.ColumnCount;
                var left = eventAreaLeft + (columnWidth * item.ColumnIndex) + EventSpacing;
                var width = Math.Max(0.0, columnWidth - EventSpacing);
                var top = Math.Max(0.0, (item.Start - day).TotalMinutes * pixelsPerMinute);
                var height = Math.Max(MinimumEventHeight, (item.End - item.Start).TotalMinutes * pixelsPerMinute);
                item.Presenter.Arrange(new Rect(left, top, width, height));
            }

            return finalSize;
        }

        /// <inheritdoc />
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var hourHeight = Math.Max(0.0, HourHeight);
            var timeColumnWidth = Math.Max(0.0, TimeColumnWidth);
            var hourPen = CreatePen(HourLineBrush, 1.0);
            var quarterPen = CreatePen(QuarterHourLineBrush, 0.5);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var fontFamily = TextElement.GetFontFamily(this);
            var fontSize = TextElement.GetFontSize(this);

            drawingContext.DrawRectangle(
                TimeColumnBackground,
                null,
                new Rect(0.0, 0.0, timeColumnWidth, RenderSize.Height));

            drawingContext.DrawLine(hourPen, new Point(timeColumnWidth, 0), new Point(timeColumnWidth, RenderSize.Height));

            for (var hour = 0; hour < 24; hour++)
            {
                var y = hour * hourHeight;
                drawingContext.DrawLine(hourPen, new Point(0.0, y), new Point(RenderSize.Width, y));

                if (ShowQuarterHourLines)
                {
                    for (var quarter = 1; quarter < 4; quarter++)
                    {
                        var quarterY = y + (hourHeight * quarter / 4.0);
                        drawingContext.DrawLine(quarterPen, new Point(timeColumnWidth, quarterY), new Point(RenderSize.Width, quarterY));
                    }
                }

                var label = DateTime.Today.AddHours(hour).ToString("h tt", TimeCulture);
                var text = new FormattedText(
                    label,
                    TimeCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    fontSize,
                    TimeForeground,
                    dpi)
                {
                    TextAlignment = TextAlignment.Right,
                    MaxTextWidth = Math.Max(0.0, timeColumnWidth - 12.0)
                };

                drawingContext.DrawText(text, new Point(0.0, Math.Max(0.0, y + 3.0)));
            }

            RenderTimeGuides();
        }

        /// <summary>
        /// Repaints the guide overlay. The overlay renders after the event presenters so the current time
        /// indicator stays visible when it overlaps an event.
        /// </summary>
        private void RenderTimeGuides()
        {
            using var guideContext = _guideVisual.RenderOpen();
            DrawTimeGuides(guideContext);
        }

        private static Pen CreatePen(Brush brush, double thickness)
        {
            var pen = new Pen(brush, thickness);
            if (pen.CanFreeze)
            {
                pen.Freeze();
            }

            return pen;
        }

        private void BuildLayoutItems()
        {
            _layoutItems.Clear();
            foreach (UIElement child in InternalChildren)
            {
                if (child is not CalendarEventPresenter presenter)
                {
                    continue;
                }

                var start = presenter.DisplayStart;
                var end = presenter.DisplayEnd;
                if (end <= start)
                {
                    presenter.Arrange(Rect.Empty);
                    continue;
                }

                _layoutItems.Add(new CalendarEventLayoutItem
                {
                    Presenter = presenter,
                    Start = start,
                    End = end
                });
            }
        }

        private void DrawTimeGuides(DrawingContext drawingContext)
        {
            var owner = Owner;
            if (owner == null)
            {
                return;
            }

            var pixelsPerMinute = Math.Max(0.0, HourHeight) / 60.0;
            var left = Math.Max(0.0, TimeColumnWidth);
            var guidePen = CreatePen(CurrentTimeBrush, 2.0);

            if (owner.ShowCurrentTimeIndicator && owner.SelectedDate.Date == DateTime.Today)
            {
                var nowY = DateTime.Now.TimeOfDay.TotalMinutes * pixelsPerMinute;
                drawingContext.DrawEllipse(CurrentTimeBrush, null, new Point(left, nowY), 4.0, 4.0);
                drawingContext.DrawLine(guidePen, new Point(left, nowY), new Point(RenderSize.Width, nowY));
            }

            if (!owner.IsDragActive)
            {
                return;
            }

            var dragY = (owner.DragPreviewStart - owner.SelectedDate.Date).TotalMinutes * pixelsPerMinute;
            drawingContext.DrawLine(guidePen, new Point(left, dragY), new Point(RenderSize.Width, dragY));

            var timeText = owner.DragPreviewStart.ToString("h:mm tt", TimeCulture);
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var formattedText = new FormattedText(
                timeText,
                TimeCulture,
                FlowDirection.LeftToRight,
                new Typeface(TextElement.GetFontFamily(this), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                TextElement.GetFontSize(this),
                owner.Foreground,
                dpi);
            var padding = 5.0;
            var labelRect = new Rect(
                left + 8.0,
                Math.Max(0.0, dragY - formattedText.Height - (padding * 2.0)),
                formattedText.Width + (padding * 2.0),
                formattedText.Height + (padding * 2.0));

            drawingContext.DrawRoundedRectangle(owner.Background, guidePen, labelRect, 3.0, 3.0);
            drawingContext.DrawText(formattedText, new Point(labelRect.Left + padding, labelRect.Top + padding));
        }

        /// <summary>
        /// A <see cref="DrawingVisual"/> that never participates in hit testing so the guides drawn on top of
        /// the events do not intercept mouse input intended for an event underneath them.
        /// </summary>
        private sealed class GuideVisual : DrawingVisual
        {
            protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) => null;

            protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters) => null;
        }
    }
}
