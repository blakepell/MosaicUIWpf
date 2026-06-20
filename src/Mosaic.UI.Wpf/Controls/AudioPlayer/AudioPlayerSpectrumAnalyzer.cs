/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Threading.Tasks;
using Mosaic.UI.Wpf.Controls.WaveformVisualizer;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A real-time spectrum analyzer designed to pair with the <see cref="AudioPlayer"/> control. It renders
    /// frequency bands across the horizontal axis and signal strength (amplitude) on the vertical axis, with
    /// optional peak-hold indicators and amplitude-driven color intensity that makes strong frequencies easy
    /// to identify at a glance.
    /// </summary>
    /// <remarks>
    /// The analyzer captures the audio rendered by the default Windows output device using WASAPI loopback —
    /// the same capture infrastructure used by <see cref="LoopbackWaveformVisualizer"/>. Because the
    /// <see cref="AudioPlayer"/> plays through that device, simply placing this control near an
    /// <see cref="AudioPlayer"/> visualizes whatever it is currently playing (along with any other system
    /// audio). A short-time Fourier transform converts the captured samples into a magnitude spectrum which is
    /// then grouped into logarithmically spaced bands so that the bass, mid, and treble ranges are all legible.
    /// </remarks>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:AudioPlayerSpectrumAnalyzer Height="140" IsActive="True" BandCount="32" /&gt;
    /// </code>
    /// </example>
    public class AudioPlayerSpectrumAnalyzer : FrameworkElement
    {
        #region Constants

        /// <summary>The number of samples fed into each FFT. Must be a power of two.</summary>
        private const int FftSize = 1024;

        /// <summary>The lowest signal level (in decibels relative to full scale) mapped to the bottom of a bar.</summary>
        private const double DecibelFloor = -70.0;

        /// <summary>The interval between visual updates, expressed in milliseconds (~30 fps).</summary>
        private const double FrameIntervalMilliseconds = 33.0;

        /// <summary>How quickly a bar rises toward a higher target value (0 = frozen, 1 = instant).</summary>
        private const double AttackRate = 0.65;

        /// <summary>How quickly a bar falls toward a lower target value (0 = frozen, 1 = instant).</summary>
        private const double DecayRate = 0.18;

        /// <summary>How far a peak-hold marker drops each frame once it is no longer being pushed up.</summary>
        private const double PeakFallPerFrame = 0.012;

        #endregion

        #region Private Fields

        private static readonly Brush DefaultBackgroundBrush = CreateFrozenBrush(Color.FromRgb(10, 14, 20));
        private static readonly Brush DefaultBarBrush = CreateFrozenBrush(Color.FromRgb(0, 191, 255));
        private static readonly Brush DefaultIntensityBrush = CreateIntensityGradient();
        private static readonly Brush DefaultPeakBrush = CreateFrozenBrush(Color.FromRgb(245, 245, 245));

        private readonly Lock _captureLock = new();
        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private readonly DispatcherTimer _renderTimer;

        private readonly double[] _window = new double[FftSize];
        private readonly double[] _fftRe = new double[FftSize];
        private readonly double[] _fftIm = new double[FftSize];
        private double _windowSum;

        /// <summary>Per-band smoothed magnitudes in the range 0..1, used to draw the bars.</summary>
        private double[] _bandValues = [];

        /// <summary>Per-band peak-hold markers in the range 0..1.</summary>
        private double[] _peakValues = [];

        /// <summary>The first FFT bin (inclusive) contributing to each band.</summary>
        private int[] _bandBinStart = [];

        /// <summary>The last FFT bin (exclusive) contributing to each band.</summary>
        private int[] _bandBinEnd = [];

        private CancellationTokenSource? _lifecycleCancellation;
        private AudioCapture? _capture;
        private IDisposable? _captureSession;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="IsActive"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(true, OnIsActiveChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the analyzer is capturing audio and animating. The default is <c>true</c>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the analyzer is capturing audio and animating.")]
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BandCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BandCountProperty = DependencyProperty.Register(
            nameof(BandCount),
            typeof(int),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(32, FrameworkPropertyMetadataOptions.AffectsRender, OnBandCountChanged),
            ValidateBandCount);

        /// <summary>
        /// Gets or sets the number of frequency bands (bars) rendered across the horizontal axis. The default is <c>32</c>.
        /// </summary>
        [Category("Appearance")]
        [Description("The number of frequency bands (bars) rendered across the horizontal axis.")]
        public int BandCount
        {
            get => (int)GetValue(BandCountProperty);
            set => SetValue(BandCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Background"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(DefaultBackgroundBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the brush used to fill the area behind the bars.
        /// </summary>
        [Category("Appearance")]
        [Description("The brush used to fill the area behind the bars.")]
        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BarBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
            nameof(BarBrush),
            typeof(Brush),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(DefaultBarBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the solid brush used to fill bars when <see cref="UseColorIntensity"/> is disabled.
        /// </summary>
        [Category("Appearance")]
        [Description("The solid brush used to fill bars when color intensity is disabled.")]
        public Brush BarBrush
        {
            get => (Brush)GetValue(BarBrushProperty);
            set => SetValue(BarBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IntensityBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IntensityBrushProperty = DependencyProperty.Register(
            nameof(IntensityBrush),
            typeof(Brush),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(DefaultIntensityBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the vertical gradient used to color bars when <see cref="UseColorIntensity"/> is enabled.
        /// The gradient is mapped to the full height of the control so each bar reveals the portion of the gradient
        /// up to its amplitude — the default is the classic green (quiet) through yellow and orange to red (loudest).
        /// </summary>
        [Category("Appearance")]
        [Description("The vertical gradient used to color bars when color intensity is enabled (green to yellow to orange to red).")]
        public Brush IntensityBrush
        {
            get => (Brush)GetValue(IntensityBrushProperty);
            set => SetValue(IntensityBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UseColorIntensity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UseColorIntensityProperty = DependencyProperty.Register(
            nameof(UseColorIntensity),
            typeof(bool),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets a value indicating whether bars are painted with the vertical <see cref="IntensityBrush"/>
        /// gradient (making strong frequencies easy to identify) instead of the solid <see cref="BarBrush"/>. The
        /// default is <c>true</c>.
        /// </summary>
        [Category("Appearance")]
        [Description("Whether bars are painted with the vertical intensity gradient instead of the solid bar brush.")]
        public bool UseColorIntensity
        {
            get => (bool)GetValue(UseColorIntensityProperty);
            set => SetValue(UseColorIntensityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowPeaks"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPeaksProperty = DependencyProperty.Register(
            nameof(ShowPeaks),
            typeof(bool),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets a value indicating whether per-band peak-hold indicators are drawn. The default is <c>true</c>.
        /// </summary>
        [Category("Appearance")]
        [Description("Whether per-band peak-hold indicators are drawn above each bar.")]
        public bool ShowPeaks
        {
            get => (bool)GetValue(ShowPeaksProperty);
            set => SetValue(ShowPeaksProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PeakBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PeakBrushProperty = DependencyProperty.Register(
            nameof(PeakBrush),
            typeof(Brush),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(DefaultPeakBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the brush used to draw the peak-hold indicators.
        /// </summary>
        [Category("Appearance")]
        [Description("The brush used to draw the peak-hold indicators above each bar.")]
        public Brush PeakBrush
        {
            get => (Brush)GetValue(PeakBrushProperty);
            set => SetValue(PeakBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BarSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BarSpacingProperty = DependencyProperty.Register(
            nameof(BarSpacing),
            typeof(double),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the gap, in device-independent pixels, rendered between adjacent bars. The default is <c>2</c>.
        /// </summary>
        [Category("Appearance")]
        [Description("The gap, in device-independent pixels, rendered between adjacent bars.")]
        public double BarSpacing
        {
            get => (double)GetValue(BarSpacingProperty);
            set => SetValue(BarSpacingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Sensitivity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SensitivityProperty = DependencyProperty.Register(
            nameof(Sensitivity),
            typeof(double),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(1.0));

        /// <summary>
        /// Gets or sets a linear gain applied to the captured signal before it is mapped to bar heights. Values
        /// above <c>1.0</c> make quiet passages more visible; values below <c>1.0</c> tame loud material. The default is <c>1.0</c>.
        /// </summary>
        [Category("Behavior")]
        [Description("A linear gain applied to the captured signal before it is mapped to bar heights.")]
        public double Sensitivity
        {
            get => (double)GetValue(SensitivityProperty);
            set => SetValue(SensitivityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(double),
            typeof(AudioPlayerSpectrumAnalyzer),
            new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the radius applied to the top corners of each bar. The default is <c>2</c>.
        /// </summary>
        [Category("Appearance")]
        [Description("The radius applied to the top corners of each bar.")]
        public double CornerRadius
        {
            get => (double)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when audio capture or rendering fails.
        /// </summary>
        public event EventHandler<Exception>? OnError;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayerSpectrumAnalyzer"/> class.
        /// </summary>
        public AudioPlayerSpectrumAnalyzer()
        {
            SnapsToDevicePixels = true;

            // Precompute a Hann window and its sum so the magnitude spectrum can be normalized to full scale.
            for (int i = 0; i < FftSize; i++)
            {
                _window[i] = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (FftSize - 1));
                _windowSum += _window[i];
            }

            BuildBands();

            _renderTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(FrameIntervalMilliseconds),
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
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? 320.0 : availableSize.Width;
            double height = double.IsInfinity(availableSize.Height) ? 120.0 : availableSize.Height;
            return new Size(width, height);
        }

        #region Rendering

        /// <inheritdoc/>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            try
            {
                RenderSpectrum(drawingContext);
            }
            catch (Exception exception)
            {
                ReportError(exception);
            }
        }

        private void RenderSpectrum(DrawingContext drawingContext)
        {
            var size = RenderSize;
            drawingContext.DrawRectangle(Background, null, new Rect(size));

            if (size.Width <= 0 || size.Height <= 0 || _bandValues.Length == 0)
            {
                return;
            }

            UpdateBandValues();

            int bandCount = _bandValues.Length;
            double spacing = Math.Max(0, BarSpacing);
            double slotWidth = size.Width / bandCount;
            double barWidth = Math.Max(1.0, slotWidth - spacing);
            double radius = Math.Min(CornerRadius, barWidth / 2.0);
            bool useColor = UseColorIntensity;
            Brush barBrush = BarBrush;
            Brush intensityBrush = IntensityBrush;
            Brush peakBrush = PeakBrush;
            bool showPeaks = ShowPeaks && peakBrush != null;
            double peakThickness = 2.0;

            // The intensity gradient is mapped to the full control height (not each bar's rect) so a bar's color
            // reflects how high it reaches — green near the floor rising to red at the top, like a classic analyzer.
            var fullColumnHeight = new Rect(0, 0, size.Width, size.Height);

            for (int band = 0; band < bandCount; band++)
            {
                double value = _bandValues[band];
                double x = band * slotWidth + (slotWidth - barWidth) / 2.0;

                if (value > 0.0001)
                {
                    double barHeight = value * size.Height;
                    double y = size.Height - barHeight;
                    var rect = new Rect(x, y, barWidth, barHeight);

                    if (useColor && intensityBrush != null)
                    {
                        Geometry clip = radius > 0 ? new RectangleGeometry(rect, radius, radius) : new RectangleGeometry(rect);
                        clip.Freeze();
                        drawingContext.PushClip(clip);
                        drawingContext.DrawRectangle(intensityBrush, null, fullColumnHeight);
                        drawingContext.Pop();
                    }
                    else if (radius > 0)
                    {
                        drawingContext.DrawRoundedRectangle(barBrush, null, rect, radius, radius);
                    }
                    else
                    {
                        drawingContext.DrawRectangle(barBrush, null, rect);
                    }
                }

                if (showPeaks)
                {
                    double peak = _peakValues[band];
                    if (peak > 0.0001)
                    {
                        double peakY = size.Height - peak * size.Height;
                        peakY = Math.Min(size.Height - peakThickness, Math.Max(0, peakY - peakThickness));
                        drawingContext.DrawRectangle(peakBrush, null, new Rect(x, peakY, barWidth, peakThickness));
                    }
                }
            }
        }

        /// <summary>
        /// Pulls the most recent captured samples, runs the FFT, and folds the magnitude spectrum into the
        /// smoothed per-band values and peak-hold markers that drive the next frame.
        /// </summary>
        private void UpdateBandValues()
        {
            double[] targets = ComputeBandTargets();

            for (int band = 0; band < _bandValues.Length; band++)
            {
                double target = band < targets.Length ? targets[band] : 0.0;
                double current = _bandValues[band];
                double rate = target > current ? AttackRate : DecayRate;
                current += (target - current) * rate;

                if (current < 0.0001)
                {
                    current = 0.0;
                }

                _bandValues[band] = current;

                double peak = _peakValues[band];
                if (current >= peak)
                {
                    peak = current;
                }
                else
                {
                    peak = Math.Max(current, peak - PeakFallPerFrame);
                }

                _peakValues[band] = peak;
            }
        }

        /// <summary>
        /// Computes the normalized 0..1 magnitude for each band from the latest captured audio, or an empty
        /// span when the analyzer is idle so that the bars decay naturally to zero.
        /// </summary>
        private double[] ComputeBandTargets()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return CreateDesignTimeTargets();
            }

            if (!IsActive)
            {
                return [];
            }

            AudioCapture? currentCapture;
            lock (_captureLock)
            {
                currentCapture = _capture;
            }

            double[]? samples = currentCapture?.GetRecentSamples();
            if (samples == null || samples.Length < FftSize)
            {
                return [];
            }

            // Window the most recent FftSize samples into the FFT input buffers.
            int offset = samples.Length - FftSize;
            for (int i = 0; i < FftSize; i++)
            {
                _fftRe[i] = samples[offset + i] * _window[i];
                _fftIm[i] = 0.0;
            }

            Fft(_fftRe, _fftIm);

            var targets = new double[_bandValues.Length];
            double sensitivity = Math.Max(0.0001, Sensitivity);
            double range = -DecibelFloor;

            for (int band = 0; band < targets.Length; band++)
            {
                int start = _bandBinStart[band];
                int end = _bandBinEnd[band];
                double maxMagnitude = 0.0;

                for (int bin = start; bin < end; bin++)
                {
                    double magnitude = Math.Sqrt(_fftRe[bin] * _fftRe[bin] + _fftIm[bin] * _fftIm[bin]);
                    if (magnitude > maxMagnitude)
                    {
                        maxMagnitude = magnitude;
                    }
                }

                // Normalize so a full-scale tone reaches 0 dB, then map [DecibelFloor, 0] dB onto [0, 1].
                double amplitude = maxMagnitude * 2.0 / _windowSum * sensitivity;
                double decibels = 20.0 * Math.Log10(amplitude + 1e-9);
                double normalized = (decibels - DecibelFloor) / range;
                targets[band] = Math.Clamp(normalized, 0.0, 1.0);
            }

            return targets;
        }

        /// <summary>
        /// Produces a static, pleasant-looking set of band magnitudes so the control previews well in designers.
        /// </summary>
        private double[] CreateDesignTimeTargets()
        {
            var targets = new double[_bandValues.Length];
            for (int band = 0; band < targets.Length; band++)
            {
                double t = (double)band / Math.Max(1, targets.Length - 1);
                targets[band] = 0.25 + 0.6 * Math.Abs(Math.Sin(t * Math.PI * 3.0)) * (1.0 - t * 0.4);
            }

            return targets;
        }

        #endregion

        #region Band Setup

        private void BuildBands()
        {
            int bandCount = BandCount;
            _bandValues = new double[bandCount];
            _peakValues = new double[bandCount];
            _bandBinStart = new int[bandCount];
            _bandBinEnd = new int[bandCount];

            // Distribute bins logarithmically between the first usable bin and Nyquist so that bass detail is
            // not crushed against the left edge the way a linear split would do.
            const int minBin = 1;
            int maxBin = FftSize / 2;
            double logMin = Math.Log(minBin);
            double logMax = Math.Log(maxBin);
            int previousEnd = minBin;

            for (int band = 0; band < bandCount; band++)
            {
                double fraction = (double)(band + 1) / bandCount;
                int end = (int)Math.Round(Math.Exp(logMin + (logMax - logMin) * fraction));
                end = Math.Clamp(end, previousEnd + 1, maxBin);

                _bandBinStart[band] = previousEnd;
                _bandBinEnd[band] = end;
                previousEnd = end;
            }
        }

        #endregion

        #region Capture Lifecycle

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var analyzer = (AudioPlayerSpectrumAnalyzer)d;
            if (!analyzer.IsLoaded || DesignerProperties.GetIsInDesignMode(analyzer))
            {
                return;
            }

            analyzer.QueueListeningChange((bool)e.NewValue);
        }

        private static void OnBandCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AudioPlayerSpectrumAnalyzer)d).BuildBands();
        }

        private static bool ValidateBandCount(object value) => value is int count && count > 0;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsActive && !DesignerProperties.GetIsInDesignMode(this))
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
            var cancellation = new CancellationTokenSource();
            var previous = Interlocked.Exchange(ref _lifecycleCancellation, cancellation);
            previous?.Cancel();
            _ = ChangeListeningAsync(shouldListen, cancellation);
        }

        private async Task ChangeListeningAsync(bool shouldListen, CancellationTokenSource cancellation)
        {
            var cancellationToken = cancellation.Token;
            try
            {
                await _lifecycleLock.WaitAsync(cancellationToken);
                try
                {
                    (AudioCapture? oldCapture, IDisposable? oldSession) = DetachCaptureSession();
                    _renderTimer.Stop();
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

                    var newCapture = new AudioCapture(ReportError);
                    IDisposable? newSession = null;
                    try
                    {
                        newSession = await Task.Run(() => CreateCaptureSession(newCapture), cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!IsLoaded || !IsActive)
                        {
                            await Task.Run(newSession.Dispose);
                            return;
                        }

                        lock (_captureLock)
                        {
                            _capture = newCapture;
                            _captureSession = newSession;
                        }

                        _renderTimer.Start();
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
                    _lifecycleLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ReportError(exception);
                if (IsLoaded)
                {
                    SetCurrentValue(IsActiveProperty, false);
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref _lifecycleCancellation, null, cancellation);
                cancellation.Dispose();
            }
        }

        private static IDisposable CreateCaptureSession(AudioCapture capture)
        {
            var tracker = new DeviceTracker(capture, _ => { });
            tracker.Start();
            return tracker;
        }

        private (AudioCapture? Capture, IDisposable? Session) DetachCaptureSession()
        {
            lock (_captureLock)
            {
                AudioCapture? capture = _capture;
                IDisposable? session = _captureSession;
                _capture = null;
                _captureSession = null;
                return (capture, session);
            }
        }

        #endregion

        #region Error Handling / FFT

        private void ReportError(Exception exception)
        {
            if (!Dispatcher.CheckAccess())
            {
                if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
                {
                    _ = Dispatcher.BeginInvoke(() => RaiseError(exception));
                }

                return;
            }

            RaiseError(exception);
        }

        private void RaiseError(Exception exception)
        {
            EventHandler<Exception>? handlers = OnError;
            if (handlers is null)
            {
                return;
            }

            foreach (EventHandler<Exception> handler in handlers.GetInvocationList().Cast<EventHandler<Exception>>())
            {
                try
                {
                    handler(this, exception);
                }
                catch
                {
                    // Error handlers must not destabilize audio capture or WPF rendering.
                }
            }
        }

        /// <summary>
        /// Performs an in-place iterative radix-2 Cooley-Tukey FFT on the supplied complex arrays.
        /// </summary>
        /// <param name="real">The real components; overwritten with the transform's real output.</param>
        /// <param name="imaginary">The imaginary components; overwritten with the transform's imaginary output.</param>
        private static void Fft(double[] real, double[] imaginary)
        {
            int n = real.Length;

            // Bit-reversal permutation.
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1)
                {
                    j ^= bit;
                }

                j ^= bit;

                if (i < j)
                {
                    (real[i], real[j]) = (real[j], real[i]);
                    (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
                }
            }

            for (int length = 2; length <= n; length <<= 1)
            {
                double angle = -2.0 * Math.PI / length;
                double wLengthReal = Math.Cos(angle);
                double wLengthImaginary = Math.Sin(angle);

                for (int i = 0; i < n; i += length)
                {
                    double wReal = 1.0;
                    double wImaginary = 0.0;
                    int half = length / 2;

                    for (int k = 0; k < half; k++)
                    {
                        int a = i + k;
                        int b = i + k + half;

                        double uReal = real[a];
                        double uImaginary = imaginary[a];
                        double vReal = real[b] * wReal - imaginary[b] * wImaginary;
                        double vImaginary = real[b] * wImaginary + imaginary[b] * wReal;

                        real[a] = uReal + vReal;
                        imaginary[a] = uImaginary + vImaginary;
                        real[b] = uReal - vReal;
                        imaginary[b] = uImaginary - vImaginary;

                        double nextWReal = wReal * wLengthReal - wImaginary * wLengthImaginary;
                        wImaginary = wReal * wLengthImaginary + wImaginary * wLengthReal;
                        wReal = nextWReal;
                    }
                }
            }
        }

        private static Brush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Builds the default bottom-to-top intensity gradient: green for quiet levels, rising through yellow and
        /// orange to red at the loudest, echoing the look of classic spectrum analyzers.
        /// </summary>
        private static Brush CreateIntensityGradient()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 1),
                EndPoint = new Point(0, 0)
            };

            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 200, 60), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(120, 210, 20), 0.35));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(230, 220, 30), 0.55));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 140, 0), 0.78));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(235, 30, 30), 1.0));
            brush.Freeze();
            return brush;
        }

        #endregion
    }
}
