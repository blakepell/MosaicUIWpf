/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls.WaveformVisualizer
{
    /// <summary>
    /// Displays a waveform captured from the default Windows audio render device.
    /// </summary>
    /// <remarks>
    /// The visualizer uses WASAPI loopback capture and follows changes to the default console render device.
    /// </remarks>
    /// <example>
    /// <code language="xml">
    /// &lt;CheckBox x:Name="ListeningToggle" IsChecked="True" /&gt;
    /// &lt;mosaic:LoopbackWaveformVisualizer
    ///     IsListening="{Binding IsChecked, ElementName=ListeningToggle}" /&gt;
    /// </code>
    /// </example>
    public sealed class LoopbackWaveformVisualizer : FrameworkElement
    {
        /// <summary>
        /// Identifies the <see cref="IsListening"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsListeningProperty = DependencyProperty.Register(
            nameof(IsListening),
            typeof(bool),
            typeof(LoopbackWaveformVisualizer),
            new FrameworkPropertyMetadata(true, OnIsListeningChanged));

        /// <summary>
        /// Gets or sets a value that indicates whether the visualizer captures system audio.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the visualizer captures system audio; otherwise, <see langword="false" />.
        /// The default is <see langword="true" />.
        /// </value>
        public bool IsListening
        {
            get => (bool)GetValue(IsListeningProperty);
            set => SetValue(IsListeningProperty, value);
        }

        private static readonly Brush Background = CreateFrozenBrush(Color.FromRgb(10, 14, 20));
        private static readonly Pen WaveformPen = CreateFrozenPen(Color.FromRgb(0, 191, 255));

        private readonly DispatcherTimer renderTimer;
        private AudioCapture? capture;
        private DeviceTracker? deviceTracker;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackWaveformVisualizer"/> class.
        /// </summary>
        public LoopbackWaveformVisualizer()
        {
            SnapsToDevicePixels = true;
            renderTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(60),
                DispatcherPriority.Render,
                (_, _) => InvalidateVisual(),
                Dispatcher)
            {
                IsEnabled = false
            };

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <inheritdoc/>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(Background, null, new Rect(RenderSize));

            double[] samples = capture?.GetRecentSamples() ?? [];
            if (!IsListening || samples.Length < 2 || RenderSize.Width <= 0 || RenderSize.Height <= 0)
            {
                return;
            }

            int pointCount = Math.Min(samples.Length, Math.Max(2, (int)Math.Ceiling(RenderSize.Width)));
            double centerY = RenderSize.Height / 2;
            double xScale = RenderSize.Width / (pointCount - 1);
            double sampleScale = (double)(samples.Length - 1) / (pointCount - 1);

            StreamGeometry geometry = new();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(new Point(0, centerY - samples[0] * centerY), false, false);
                for (int point = 1; point < pointCount; point++)
                {
                    int sampleIndex = Math.Min(samples.Length - 1, (int)Math.Round(point * sampleScale));
                    context.LineTo(
                        new Point(point * xScale, centerY - samples[sampleIndex] * centerY),
                        true,
                        false);
                }
            }

            geometry.Freeze();
            drawingContext.DrawGeometry(null, WaveformPen, geometry);
        }

        private static void OnIsListeningChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LoopbackWaveformVisualizer visualizer = (LoopbackWaveformVisualizer)dependencyObject;
            if (!visualizer.IsLoaded || DesignerProperties.GetIsInDesignMode(visualizer))
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                visualizer.StartListening();
            }
            else
            {
                visualizer.StopListening();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsListening && !DesignerProperties.GetIsInDesignMode(this))
            {
                StartListening();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopListening();
        }

        private void StartListening()
        {
            if (deviceTracker is not null)
            {
                return;
            }

            capture = new AudioCapture();
            deviceTracker = new DeviceTracker(capture);

            try
            {
                deviceTracker.Start();
                renderTimer.Start();
            }
            catch
            {
                deviceTracker.Dispose();
                deviceTracker = null;
                capture = null;
                SetCurrentValue(IsListeningProperty, false);
                throw;
            }
        }

        private void StopListening()
        {
            renderTimer.Stop();
            deviceTracker?.Dispose();
            deviceTracker = null;
            capture = null;
            InvalidateVisual();
        }

        private static Brush CreateFrozenBrush(Color color)
        {
            SolidColorBrush brush = new(color);
            brush.Freeze();
            return brush;
        }

        private static Pen CreateFrozenPen(Color color)
        {
            Pen pen = new(CreateFrozenBrush(color), 1);
            pen.Freeze();
            return pen;
        }
    }
}
