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

using System.Windows.Automation.Peers;
using System.Windows.Media.Imaging;
using Mosaic.UI.Wpf.Common;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Displays a live webcam feed captured through Windows Media Foundation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Select a camera with <see cref="Device"/> (see <see cref="WebcamDeviceManager.GetDevices"/>) and call
    /// <see cref="StartAsync"/>, or set <see cref="AutoStart"/>.  Changing <see cref="Device"/> or
    /// <see cref="Resolution"/> while running switches cameras/modes seamlessly; rapid changes are coalesced.
    /// </para>
    /// <para>
    /// Frames are captured on a background thread and the preview always shows the newest frame:
    /// if the UI is busy, stale frames are dropped rather than queued.
    /// </para>
    /// <para>
    /// The camera is released when the control is unloaded (for example when its window closes) and
    /// resumed if it is loaded again while still started.  Call <see cref="StopAsync"/> to release it
    /// explicitly; once that task completes other applications can open the device.
    /// </para>
    /// <para>
    /// Lifecycle members must be called on the UI thread, or they marshal themselves onto it.  In the XAML
    /// designer the control shows an empty placeholder and never touches Media Foundation.
    /// </para>
    /// </remarks>
    [DefaultEvent(nameof(Error))]
    [DefaultProperty(nameof(Device))]
    [TemplatePart(Name = PartImage, Type = typeof(Image))]
    public class Webcam : Control
    {
        private const string PartImage = "PART_Image";
        private static readonly TimeSpan FirstFrameTimeout = TimeSpan.FromSeconds(3);

        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private readonly Action _renderFrame;
        private Image? _image;
        private WriteableBitmap? _bitmap;
        private MediaFoundationCamera? _camera;
        private WebcamFrameExchange? _exchange;
        private TaskCompletionSource? _firstFrame;
        private bool _wantRunning;
        private bool _suspended;
        private bool _dispatcherShutdownHooked;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Device"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DeviceProperty = DependencyProperty.Register(
            nameof(Device), typeof(WebcamDevice), typeof(Webcam), new FrameworkPropertyMetadata(null, OnDeviceChanged));

        /// <summary>
        /// Gets or sets the camera to display.  Changing it while running switches to the new camera;
        /// setting it to <see langword="null"/> stops the preview until a device is set again.
        /// </summary>
        [Category("Common")]
        [Description("The camera to display.")]
        public WebcamDevice? Device
        {
            get => (WebcamDevice?)GetValue(DeviceProperty);
            set => SetValue(DeviceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Resolution"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register(
            nameof(Resolution), typeof(WebcamResolution), typeof(Webcam),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnResolutionChanged));

        /// <summary>
        /// Gets or sets the requested capture mode, normally one of <see cref="AvailableResolutions"/>.
        /// <see langword="null"/> (the default) selects the best mode near 1920 × 1080 @ 30 fps automatically.
        /// If the device does not offer the requested mode, the closest one is used; see <see cref="ActualResolution"/>.
        /// </summary>
        [Category("Common")]
        [Description("The requested capture mode, or null for automatic selection.")]
        public WebcamResolution? Resolution
        {
            get => (WebcamResolution?)GetValue(ResolutionProperty);
            set => SetValue(ResolutionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
            nameof(Stretch), typeof(Stretch), typeof(Webcam),
            new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Gets or sets how the video is scaled to fill the control.  The default is <see cref="System.Windows.Media.Stretch.Uniform"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("How the video is scaled to fill the control.")]
        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Mirror"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MirrorProperty = DependencyProperty.Register(
            nameof(Mirror), typeof(bool), typeof(Webcam), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the preview is flipped horizontally, which feels natural for front-facing
        /// cameras.  Mirroring is a render transform; frame data and <see cref="CaptureFrameAsync"/> are unaffected.
        /// </summary>
        [Category("Appearance")]
        [Description("Flips the preview horizontally.")]
        public bool Mirror
        {
            get => (bool)GetValue(MirrorProperty);
            set => SetValue(MirrorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoStart"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoStartProperty = DependencyProperty.Register(
            nameof(AutoStart), typeof(bool), typeof(Webcam), new FrameworkPropertyMetadata(false, OnAutoStartChanged));

        /// <summary>
        /// Gets or sets whether the camera starts automatically when the control is loaded and a
        /// <see cref="Device"/> is set.  Failures are reported through <see cref="Error"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Starts the camera automatically when loaded and a device is set.")]
        public bool AutoStart
        {
            get => (bool)GetValue(AutoStartProperty);
            set => SetValue(AutoStartProperty, value);
        }

        private static readonly DependencyPropertyKey StatePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(State), typeof(WebcamState), typeof(Webcam), new FrameworkPropertyMetadata(WebcamState.Stopped, OnStateChanged));

        /// <summary>
        /// Identifies the <see cref="State"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StateProperty = StatePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the lifecycle state of the camera.
        /// </summary>
        [Category("Common")]
        [Description("The lifecycle state of the camera.")]
        public WebcamState State => (WebcamState)GetValue(StateProperty);

        private static readonly DependencyPropertyKey IsRunningPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsRunning), typeof(bool), typeof(Webcam), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsRunning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRunningProperty = IsRunningPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the camera is open and delivering frames (<see cref="State"/> is <see cref="WebcamState.Running"/>).
        /// </summary>
        [Category("Common")]
        public bool IsRunning => (bool)GetValue(IsRunningProperty);

        private static readonly DependencyPropertyKey AvailableResolutionsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AvailableResolutions), typeof(IReadOnlyList<WebcamResolution>), typeof(Webcam),
            new FrameworkPropertyMetadata(Array.Empty<WebcamResolution>()));

        /// <summary>
        /// Identifies the <see cref="AvailableResolutions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AvailableResolutionsProperty = AvailableResolutionsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the capture modes offered by the current <see cref="Device"/>, largest first.  Populated when the
        /// camera starts and cleared when a different device is selected.  Use
        /// <see cref="WebcamDeviceManager.GetResolutionsAsync"/> to query a device that is not running.
        /// </summary>
        [Category("Common")]
        public IReadOnlyList<WebcamResolution> AvailableResolutions => (IReadOnlyList<WebcamResolution>)GetValue(AvailableResolutionsProperty);

        private static readonly DependencyPropertyKey ActualResolutionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ActualResolution), typeof(WebcamResolution), typeof(Webcam), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="ActualResolution"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActualResolutionProperty = ActualResolutionPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the capture mode actually negotiated with the device, or <see langword="null"/> when not running.
        /// The frame size is in camera pixels and is independent of display DPI.
        /// </summary>
        [Category("Common")]
        public WebcamResolution? ActualResolution => (WebcamResolution?)GetValue(ActualResolutionProperty);

        #endregion

        #region Commands

        /// <summary>
        /// Starts the target <see cref="Webcam"/>.  Can execute when a device is set and the camera is stopped.
        /// </summary>
        public static RoutedUICommand StartCommand { get; } = new("Start", nameof(StartCommand), typeof(Webcam));

        /// <summary>
        /// Stops the target <see cref="Webcam"/>.  Can execute while the camera is starting or running.
        /// </summary>
        public static RoutedUICommand StopCommand { get; } = new("Stop", nameof(StopCommand), typeof(Webcam));

        #endregion

        #region Events

        /// <summary>
        /// Identifies the <see cref="StateChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent StateChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(StateChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<WebcamState>), typeof(Webcam));

        /// <summary>
        /// Occurs when <see cref="State"/> changes.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when the camera state changes.")]
        public event RoutedPropertyChangedEventHandler<WebcamState> StateChanged
        {
            add => AddHandler(StateChangedEvent, value);
            remove => RemoveHandler(StateChangedEvent, value);
        }

        /// <summary>
        /// Occurs on the UI thread when the camera has started and frames are flowing.  Raised again after
        /// each successful switch to a different device or resolution.
        /// </summary>
        public event EventHandler? Started;

        /// <summary>
        /// Occurs on the UI thread when the camera has been stopped and all of its resources released.
        /// An unexpected stop raises <see cref="Error"/> instead and leaves <see cref="State"/> at <see cref="WebcamState.Error"/>.
        /// </summary>
        public event EventHandler? Stopped;

        /// <summary>
        /// Occurs on the UI thread when the camera fails to start or stops unexpectedly (for example
        /// because it was disconnected).  Resources are already released when this is raised.
        /// </summary>
        public event EventHandler<WebcamErrorEventArgs>? Error;

        /// <summary>
        /// Occurs on the UI thread after each displayed frame.  Frames skipped because the UI was busy are
        /// not reported.  Costs nothing when there are no subscribers; see <see cref="WebcamFrameEventArgs"/>.
        /// </summary>
        public event EventHandler<WebcamFrameEventArgs>? FrameReceived;

        #endregion

        static Webcam()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Webcam), new FrameworkPropertyMetadata(typeof(Webcam)));
            CommandManager.RegisterClassCommandBinding(typeof(Webcam), new CommandBinding(StartCommand, OnStartCommand, OnCanStartCommand));
            CommandManager.RegisterClassCommandBinding(typeof(Webcam), new CommandBinding(StopCommand, OnStopCommand, OnCanStopCommand));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Webcam"/> class.
        /// </summary>
        public Webcam()
        {
            _renderFrame = RenderLatestFrame;

            if (!DesignerHelper.IsInDesignMode)
            {
                Loaded += OnLoaded;
                Unloaded += OnUnloaded;
            }
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_image is not null)
            {
                _image.Source = null;
            }

            _image = GetTemplateChild(PartImage) as Image;

            if (_image is not null)
            {
                _image.Source = _bitmap;
            }
        }

        /// <summary>
        /// Opens <see cref="Device"/> and starts the preview.  Calling it while starting or running does nothing.
        /// </summary>
        /// <remarks>
        /// If the control has been unloaded, the camera starts when it is loaded again.  Failures are
        /// raised through <see cref="Error"/> as well as faulting the returned task.
        /// </remarks>
        /// <returns>A task that completes when frames are flowing.</returns>
        /// <exception cref="WebcamException">No device is selected, or the camera could not be opened.
        /// <see cref="WebcamException.Kind"/> identifies the reason (in use, access denied, unsupported format, ...).</exception>
        public Task StartAsync()
        {
            if (!Dispatcher.CheckAccess())
            {
                return Dispatcher.InvokeAsync(StartAsync).Task.Unwrap();
            }

            if (DesignerHelper.IsInDesignMode)
            {
                return Task.CompletedTask;
            }

            if (Device is null)
            {
                var exception = WebcamErrors.Create(WebcamErrorKind.DeviceNotSelected, null);
                RaiseError(exception);
                return Task.FromException(exception);
            }

            _wantRunning = true;
            return ReconcileAsync(throwOnStartFailure: true);
        }

        /// <summary>
        /// Stops the preview and releases the camera.  Calling it while already stopped does nothing.
        /// When the returned task completes the device is free for other applications.
        /// </summary>
        /// <returns>A task that completes when every camera resource has been released.  It does not fault.</returns>
        public Task StopAsync()
        {
            if (!Dispatcher.CheckAccess())
            {
                return Dispatcher.InvokeAsync(StopAsync).Task.Unwrap();
            }

            if (DesignerHelper.IsInDesignMode)
            {
                return Task.CompletedTask;
            }

            _wantRunning = false;
            return StopCoreAsync();
        }

        /// <summary>
        /// Captures the frame currently shown in the preview without interrupting the camera.
        /// </summary>
        /// <remarks>
        /// The snapshot is an independent, frozen copy (safe to use on any thread and unaffected by later
        /// frames) at the camera's native pixel size and 96 DPI.  It is the raw camera image, so
        /// <see cref="Mirror"/> is not applied.  If the camera is starting, this waits briefly for the first frame.
        /// </remarks>
        /// <returns>The snapshot, or <see langword="null"/> when no frame is available.</returns>
        public async Task<BitmapSource?> CaptureFrameAsync()
        {
            if (!Dispatcher.CheckAccess())
            {
                return await Dispatcher.InvokeAsync(CaptureFrameAsync).Task.Unwrap();
            }

            if (_bitmap is null && _firstFrame is { } firstFrame && State is WebcamState.Starting or WebcamState.Running)
            {
                await Task.WhenAny(firstFrame.Task, Task.Delay(FirstFrameTimeout));
            }

            if (_bitmap is null)
            {
                return null;
            }

            var snapshot = new WriteableBitmap(_bitmap);
            snapshot.Freeze();
            return snapshot;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WebcamAutomationPeer(this);
        }

        #region Lifecycle

        /// <summary>
        /// Brings the camera in line with the desired state (<see cref="_wantRunning"/>, <see cref="Device"/>,
        /// <see cref="Resolution"/>, loaded).  Serialized by <see cref="_lifecycleLock"/>; each pass
        /// re-reads the desired state, so requests that arrive while another transition is in progress
        /// are coalesced into the final outcome instead of racing.
        /// </summary>
        private async Task ReconcileAsync(bool throwOnStartFailure)
        {
            WebcamException? startFailure = null;
            await _lifecycleLock.WaitAsync();

            try
            {
                while (true)
                {
                    WebcamDevice? targetDevice = _wantRunning && !_suspended ? Device : null;
                    WebcamResolution? targetResolution = Resolution;

                    if (_camera is not null)
                    {
                        if (targetDevice is not null && _camera.Device.Equals(targetDevice) && Equals(_camera.RequestedResolution, targetResolution))
                        {
                            break;
                        }

                        await StopCameraAsync(finalStop: targetDevice is null);
                        continue;
                    }

                    if (targetDevice is null)
                    {
                        break;
                    }

                    try
                    {
                        await StartCameraAsync(targetDevice, targetResolution);
                    }
                    catch (Exception ex)
                    {
                        startFailure = WebcamErrors.Translate(ex, targetDevice, WebcamErrorKind.DeviceUnavailable);
                        _wantRunning = false;
                        SetState(WebcamState.Error);
                        RaiseError(startFailure);
                        break;
                    }
                }
            }
            finally
            {
                _lifecycleLock.Release();
            }

            if (startFailure is not null && throwOnStartFailure)
            {
                throw startFailure;
            }
        }

        private async Task StopCoreAsync()
        {
            await ReconcileAsync(throwOnStartFailure: false);

            // An explicit stop acknowledges a previous error.
            if (State == WebcamState.Error)
            {
                SetState(WebcamState.Stopped);
            }
        }

        /// <summary>
        /// Starts a reconcile pass whose failures are reported only through <see cref="Error"/>.
        /// Used for changes that have no awaiting caller (device/resolution changes, load/unload, AutoStart).
        /// </summary>
        private async void QueueReconcile()
        {
            try
            {
                await ReconcileAsync(throwOnStartFailure: false);
            }
            catch (Exception ex)
            {
                RaiseError(WebcamErrors.Translate(ex, Device, WebcamErrorKind.Unknown));
            }
        }

        private async Task StartCameraAsync(WebcamDevice device, WebcamResolution? resolution)
        {
            SetState(WebcamState.Starting);

            var exchange = new WebcamFrameExchange();
            var camera = new MediaFoundationCamera(device, resolution, exchange, ScheduleRender, OnCameraFaulted);

            // Assigned before the worker starts so the first scheduled render finds its frame.
            _exchange = exchange;
            _firstFrame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                await camera.StartAsync();
            }
            catch
            {
                _exchange = null;
                _firstFrame.TrySetResult();
                throw;
            }

            _camera = camera;
            HookDispatcherShutdown();
            SetValue(AvailableResolutionsPropertyKey, camera.AvailableResolutions);
            SetValue(ActualResolutionPropertyKey, camera.ActualResolution);
            SetState(WebcamState.Running);
            Started?.Invoke(this, EventArgs.Empty);
        }

        private async Task StopCameraAsync(bool finalStop)
        {
            MediaFoundationCamera camera = _camera!;
            _camera = null;
            SetState(WebcamState.Stopping);

            await camera.StopAsync();

            _exchange = null;
            _firstFrame?.TrySetResult();
            _firstFrame = null;
            UnhookDispatcherShutdown();
            ClearPreview();

            if (finalStop)
            {
                SetState(WebcamState.Stopped);
                Stopped?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Worker thread: the session ended on its own (disconnect, device error).  Its resources are
        /// already released; move the control to <see cref="WebcamState.Error"/> on the UI thread.
        /// </summary>
        private void OnCameraFaulted(MediaFoundationCamera camera, WebcamException exception)
        {
            if (!Dispatcher.HasShutdownStarted)
            {
                Dispatcher.BeginInvoke(() => HandleCameraFault(camera, exception));
            }
        }

        private async void HandleCameraFault(MediaFoundationCamera camera, WebcamException exception)
        {
            await _lifecycleLock.WaitAsync();

            try
            {
                // Ignore faults from a session that has already been replaced or stopped.
                if (!ReferenceEquals(_camera, camera))
                {
                    return;
                }

                _wantRunning = false;
                await StopCameraAsync(finalStop: false);
                SetState(WebcamState.Error);
                RaiseError(exception);
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _suspended = false;

            if (AutoStart && Device is not null)
            {
                _wantRunning = true;
            }

            if (_wantRunning)
            {
                QueueReconcile();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Release the device while not in the visual tree; _wantRunning is kept so a reload resumes.
            _suspended = true;

            if (_camera is not null || _wantRunning)
            {
                QueueReconcile();
            }
        }

        private void OnConfigurationChanged()
        {
            if (DesignerHelper.IsInDesignMode)
            {
                return;
            }

            if (AutoStart && IsLoaded && Device is not null)
            {
                _wantRunning = true;
            }

            if (_wantRunning || _camera is not null)
            {
                QueueReconcile();
            }
        }

        private void HookDispatcherShutdown()
        {
            if (!_dispatcherShutdownHooked)
            {
                Dispatcher.ShutdownStarted += OnDispatcherShutdownStarted;
                _dispatcherShutdownHooked = true;
            }
        }

        private void UnhookDispatcherShutdown()
        {
            if (_dispatcherShutdownHooked)
            {
                Dispatcher.ShutdownStarted -= OnDispatcherShutdownStarted;
                _dispatcherShutdownHooked = false;
            }
        }

        private void OnDispatcherShutdownStarted(object? sender, EventArgs e)
        {
            // The application is exiting and no more async continuations will run; stop the worker so it
            // releases the device on its own thread.
            _camera?.RequestStop();
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Worker thread: a new frame is waiting and no UI update is pending.  At most one update is ever
        /// queued, so the dispatcher cannot build a backlog.
        /// </summary>
        private void ScheduleRender()
        {
            if (!Dispatcher.HasShutdownStarted)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Render, _renderFrame);
            }
        }

        /// <summary>
        /// UI thread: copies the newest frame into the reusable <see cref="WriteableBitmap"/>.
        /// </summary>
        private void RenderLatestFrame()
        {
            WebcamFrame? frame = _exchange?.AcquireLatest();

            if (frame is null)
            {
                return;
            }

            EnsureBitmap(frame.Width, frame.Height);
            _bitmap!.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), frame.Pixels, frame.Stride, 0);
            _firstFrame?.TrySetResult();

            EventHandler<WebcamFrameEventArgs>? handler = FrameReceived;

            if (handler is not null)
            {
                var args = new WebcamFrameEventArgs(frame);

                try
                {
                    handler(this, args);
                }
                finally
                {
                    args.Invalidate();
                }
            }
        }

        /// <summary>
        /// Creates the bitmap only when the frame size changes.  96 DPI makes one bitmap pixel one device
        /// independent unit; layout then scales it like any other content, independent of monitor DPI.
        /// </summary>
        private void EnsureBitmap(int width, int height)
        {
            if (_bitmap is not null && _bitmap.PixelWidth == width && _bitmap.PixelHeight == height)
            {
                return;
            }

            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

            if (_image is not null)
            {
                _image.Source = _bitmap;
            }

            WebcamResolution? actual = ActualResolution;

            if (actual is null || actual.Width != width || actual.Height != height)
            {
                SetValue(ActualResolutionPropertyKey, new WebcamResolution(width, height, actual?.FramesPerSecond ?? 0));
            }
        }

        private void ClearPreview()
        {
            _bitmap = null;

            if (_image is not null)
            {
                _image.Source = null;
            }

            SetValue(ActualResolutionPropertyKey, null);
        }

        #endregion

        #region State and notifications

        private void SetState(WebcamState state)
        {
            SetValue(StatePropertyKey, state);
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var webcam = (Webcam)d;
            var oldState = (WebcamState)e.OldValue;
            var newState = (WebcamState)e.NewValue;

            webcam.SetValue(IsRunningPropertyKey, newState == WebcamState.Running);
            webcam.RaiseEvent(new RoutedPropertyChangedEventArgs<WebcamState>(oldState, newState, StateChangedEvent));
            (UIElementAutomationPeer.FromElement(webcam) as WebcamAutomationPeer)?.RaiseStateChanged(oldState, newState);
            CommandManager.InvalidateRequerySuggested();
        }

        private static void OnDeviceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var webcam = (Webcam)d;

            // Modes belong to a device; keep them only if the "new" device is the same physical camera.
            if (!Equals(e.OldValue, e.NewValue))
            {
                webcam.SetValue(AvailableResolutionsPropertyKey, Array.Empty<WebcamResolution>());
            }

            webcam.OnConfigurationChanged();
        }

        private static void OnResolutionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Webcam)d).OnConfigurationChanged();
        }

        private static void OnAutoStartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((Webcam)d).OnConfigurationChanged();
            }
        }

        private void RaiseError(WebcamException exception)
        {
            Error?.Invoke(this, new WebcamErrorEventArgs(exception));
        }

        #endregion

        #region Command handlers

        private static void OnCanStartCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            var webcam = (Webcam)sender;
            e.CanExecute = webcam.Device is not null && webcam.State is WebcamState.Stopped or WebcamState.Error;
            e.Handled = true;
        }

        private static async void OnStartCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            try
            {
                await ((Webcam)sender).StartAsync();
            }
            catch (WebcamException)
            {
                // Already reported through the Error event; a command has no caller to rethrow to.
            }
        }

        private static void OnCanStopCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ((Webcam)sender).State is WebcamState.Starting or WebcamState.Running;
            e.Handled = true;
        }

        private static void OnStopCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            _ = ((Webcam)sender).StopAsync();
        }

        #endregion
    }
}
