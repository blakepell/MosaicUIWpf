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
    /// Provides shared rendering and capture lifecycle behavior for waveform visualizers.
    /// </summary>
    public abstract class WaveformVisualizerBase : FrameworkElement
    {
        /// <summary>
        /// Identifies the <see cref="IsListening"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsListeningProperty = DependencyProperty.Register(
            nameof(IsListening),
            typeof(bool),
            typeof(WaveformVisualizerBase),
            new FrameworkPropertyMetadata(true, OnIsListeningChanged));

        private static readonly Brush BackgroundBrush = CreateFrozenBrush(Color.FromRgb(10, 14, 20));
        private static readonly Pen WaveformPen = CreateFrozenPen(Color.FromRgb(0, 191, 255));

        private readonly object captureLock = new();
        private readonly SemaphoreSlim lifecycleLock = new(1, 1);
        private readonly DispatcherTimer renderTimer;
        private CancellationTokenSource? lifecycleCancellation;
        private AudioCapture? capture;
        private IDisposable? captureSession;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveformVisualizerBase"/> class.
        /// </summary>
        protected WaveformVisualizerBase()
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

        /// <summary>
        /// Gets or sets a value that indicates whether the visualizer captures audio.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the visualizer captures audio; otherwise, <see langword="false" />.
        /// The default is <see langword="true" />.
        /// </value>
        public bool IsListening
        {
            get => (bool)GetValue(IsListeningProperty);
            set => SetValue(IsListeningProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));

            AudioCapture? currentCapture;
            lock (captureLock)
            {
                currentCapture = capture;
            }

            double[] samples = currentCapture?.GetRecentSamples() ?? [];
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

        /// <summary>
        /// Creates and starts a capture session that writes samples to the specified capture buffer.
        /// </summary>
        /// <param name="capture">The capture buffer that receives endpoint samples.</param>
        /// <returns>A capture session that releases its audio resources when disposed.</returns>
        private protected abstract IDisposable CreateCaptureSession(AudioCapture capture);

        /// <summary>
        /// Restarts capture when the control is loaded and listening.
        /// </summary>
        protected void RestartListening()
        {
            if (!IsLoaded || !IsListening || DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            QueueListeningChange(true);
        }

        private static void OnIsListeningChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            WaveformVisualizerBase visualizer = (WaveformVisualizerBase)dependencyObject;
            if (!visualizer.IsLoaded || DesignerProperties.GetIsInDesignMode(visualizer))
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                visualizer.QueueListeningChange(true);
            }
            else
            {
                visualizer.QueueListeningChange(false);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsListening && !DesignerProperties.GetIsInDesignMode(this))
            {
                QueueListeningChange(true);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            QueueListeningChange(false);
        }

        private void QueueListeningChange(bool shouldListen)
        {
            CancellationTokenSource cancellation = new();
            CancellationTokenSource? previousCancellation = Interlocked.Exchange(
                ref lifecycleCancellation,
                cancellation);
            previousCancellation?.Cancel();
            _ = ChangeListeningAsync(shouldListen, cancellation);
        }

        private async Task ChangeListeningAsync(
            bool shouldListen,
            CancellationTokenSource cancellation)
        {
            CancellationToken cancellationToken = cancellation.Token;
            try
            {
                await lifecycleLock.WaitAsync(cancellationToken);
                try
                {
                    (AudioCapture? oldCapture, IDisposable? oldSession) = DetachCaptureSession();
                    renderTimer.Stop();
                    InvalidateVisual();

                    if (oldSession is not null)
                    {
                        await Task.Run(oldSession.Dispose);
                    }
                    else
                    {
                        oldCapture?.Dispose();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!shouldListen)
                    {
                        return;
                    }

                    AudioCapture newCapture = new();
                    IDisposable? newSession = null;
                    try
                    {
                        newSession = await Task.Run(() => CreateCaptureSession(newCapture), cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!IsLoaded || !IsListening)
                        {
                            await Task.Run(newSession.Dispose);
                            return;
                        }

                        lock (captureLock)
                        {
                            capture = newCapture;
                            captureSession = newSession;
                        }

                        renderTimer.Start();
                    }
                    catch
                    {
                        if (newSession is not null)
                        {
                            await Task.Run(newSession.Dispose);
                        }
                        else
                        {
                            newCapture.Dispose();
                        }

                        throw;
                    }
                }
                finally
                {
                    lifecycleLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                if (IsLoaded)
                {
                    SetCurrentValue(IsListeningProperty, false);
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref lifecycleCancellation, null, cancellation);
                cancellation.Dispose();
            }
        }

        private (AudioCapture? Capture, IDisposable? Session) DetachCaptureSession()
        {
            lock (captureLock)
            {
                AudioCapture? detachedCapture = capture;
                IDisposable? detachedSession = captureSession;
                capture = null;
                captureSession = null;
                return (detachedCapture, detachedSession);
            }
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
