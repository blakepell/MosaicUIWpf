/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// An image viewer modeled after the Windows Photos viewer: the image opens fitted to the available space,
    /// the mouse wheel zooms around the cursor, click-and-drag pans when the image is larger than the viewport,
    /// double-click toggles between "fit" and actual size, and a status bar shows the resolution, file size and
    /// a zoom slider.
    /// </summary>
    /// <remarks>
    /// A <see cref="Zoom"/> of <c>1.0</c> means 100% — one image pixel per physical screen pixel, independent of
    /// the image's embedded DPI or the monitor's scale factor. The image can be supplied either through
    /// <see cref="ImagePath"/> (a file system path or a pack/relative resource URI; the file size is reported and
    /// no file lock is held) or directly through <see cref="Source"/>.
    /// <para>
    /// Keyboard: <c>Ctrl + Plus</c>/<c>Ctrl + Minus</c> (or <c>+</c>/<c>-</c>) zoom, <c>Ctrl + 0</c> fits to the
    /// window, <c>Ctrl + 1</c> shows the actual size, <c>Ctrl + R</c> rotates clockwise,
    /// <c>Ctrl + Shift + R</c> rotates counter-clockwise and the arrow keys pan.
    /// </para>
    /// <para>
    /// Right-clicking the image opens a context menu with Save As (<c>Ctrl + S</c>), Copy Image (<c>Ctrl + C</c>)
    /// and Copy Path (<c>Ctrl + Shift + C</c>, only enabled when the image was loaded from a file on disk). Failures
    /// from these commands are reported through <see cref="OperationFailed"/> rather than thrown.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PartImageHost, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartImage, Type = typeof(Image))]
    [TemplatePart(Name = PartZoomSlider, Type = typeof(Slider))]
    [DefaultEvent(nameof(ZoomChanged))]
    [DefaultProperty(nameof(ImagePath))]
    public class ImageViewer : Control
    {
        #region Template Part Names

        private const string PartScrollViewer = "PART_ScrollViewer";
        private const string PartImageHost = "PART_ImageHost";
        private const string PartImage = "PART_Image";
        private const string PartZoomSlider = "PART_ZoomSlider";

        #endregion

        #region Constants

        /// <summary>
        /// The multiplicative zoom applied per mouse-wheel notch, zoom button click or keyboard zoom step.
        /// </summary>
        private const double ZoomStepFactor = 1.2;

        /// <summary>
        /// The number of device independent pixels the arrow keys pan by.
        /// </summary>
        private const double KeyboardPanStep = 48;

        #endregion

        #region Private Fields

        private ScrollViewer? _scrollViewer;
        private FrameworkElement? _imageHost;
        private Image? _image;
        private Slider? _zoomSlider;

        /// <summary>True while <see cref="Zoom"/> is being set by the fit logic so it does not cancel <see cref="IsZoomToFit"/>.</summary>
        private bool _isFitting;

        /// <summary>True while <see cref="Source"/> is being set from <see cref="ImagePath"/> so the file size is preserved.</summary>
        private bool _isLoadingFromPath;

        /// <summary>True while the zoom slider value is being synchronized from <see cref="Zoom"/>.</summary>
        private bool _suppressSliderChanged;

        private bool _isPanning;
        private Point _panStartPoint;
        private double _panStartHorizontalOffset;
        private double _panStartVerticalOffset;

        /// <summary>The encoded bytes of the image loaded through <see cref="ImagePath"/>, used for lossless "Save As".</summary>
        private byte[]? _originalBytes;

        /// <summary>The lower-case extension (e.g. <c>.jpg</c>) of the image loaded through <see cref="ImagePath"/>.</summary>
        private string? _originalExtension;

        /// <summary>The bitmap whose <c>DownloadCompleted</c> event is currently subscribed to, if any.</summary>
        private BitmapSource? _pendingBitmap;

        #endregion

        #region Commands

        /// <summary>
        /// Zooms in one step around the center of the viewport.
        /// </summary>
        public static RoutedUICommand ZoomInCommand { get; } = new("Zoom In", nameof(ZoomInCommand), typeof(ImageViewer));

        /// <summary>
        /// Zooms out one step around the center of the viewport.
        /// </summary>
        public static RoutedUICommand ZoomOutCommand { get; } = new("Zoom Out", nameof(ZoomOutCommand), typeof(ImageViewer));

        /// <summary>
        /// Fits the entire image into the viewport.
        /// </summary>
        public static RoutedUICommand ZoomToFitCommand { get; } = new("Fit to Window", nameof(ZoomToFitCommand), typeof(ImageViewer));

        /// <summary>
        /// Shows the image at its actual size (100%).
        /// </summary>
        public static RoutedUICommand ActualSizeCommand { get; } = new("Actual Size", nameof(ActualSizeCommand), typeof(ImageViewer));

        /// <summary>
        /// Rotates the image 90 degrees clockwise.
        /// </summary>
        public static RoutedUICommand RotateRightCommand { get; } = new("Rotate Right", nameof(RotateRightCommand), typeof(ImageViewer));

        /// <summary>
        /// Rotates the image 90 degrees counter-clockwise.
        /// </summary>
        public static RoutedUICommand RotateLeftCommand { get; } = new("Rotate Left", nameof(RotateLeftCommand), typeof(ImageViewer));

        /// <summary>
        /// Prompts for a location and saves the image as displayed (including rotation).
        /// </summary>
        public static RoutedUICommand SaveAsCommand { get; } = new("Save As...", nameof(SaveAsCommand), typeof(ImageViewer));

        /// <summary>
        /// Copies the full path of the image file to the clipboard. Only available when the image was loaded from disk.
        /// </summary>
        public static RoutedUICommand CopyPathCommand { get; } = new("Copy Path", nameof(CopyPathCommand), typeof(ImageViewer));

        /// <summary>
        /// Copies the image as displayed (including rotation) to the clipboard.
        /// </summary>
        public static RoutedUICommand CopyImageCommand { get; } = new("Copy Image", nameof(CopyImageCommand), typeof(ImageViewer));

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="ImagePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register(
            nameof(ImagePath), typeof(string), typeof(ImageViewer), new FrameworkPropertyMetadata(null, OnImagePathChanged));

        /// <summary>
        /// Gets or sets the path of the image to display. Accepts a file system path, an absolute URI or a
        /// pack/relative resource URI (for example <c>/Assets/photo.jpg</c>).
        /// </summary>
        [Category("Common")]
        [Description("The file path or resource URI of the image to display.")]
        public string? ImagePath
        {
            get => (string?)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(ImageSource), typeof(ImageViewer), new FrameworkPropertyMetadata(null, OnSourceChanged));

        /// <summary>
        /// Gets or sets the image being displayed. This is set automatically when <see cref="ImagePath"/> is used,
        /// but may also be assigned directly.
        /// </summary>
        [Category("Common")]
        [Description("The image being displayed.")]
        public ImageSource? Source
        {
            get => (ImageSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Zoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
            nameof(Zoom), typeof(double), typeof(ImageViewer),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnZoomChanged, CoerceZoom));

        /// <summary>
        /// Gets or sets the zoom level where <c>1.0</c> is 100% (one image pixel per screen pixel). Setting this
        /// value turns off <see cref="IsZoomToFit"/>.
        /// </summary>
        [Category("Zoom")]
        [Description("The zoom level where 1.0 is 100% (one image pixel per screen pixel).")]
        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register(
            nameof(MinZoom), typeof(double), typeof(ImageViewer), new FrameworkPropertyMetadata(0.05, OnZoomLimitsChanged));

        /// <summary>
        /// Gets or sets the minimum allowed <see cref="Zoom"/>.
        /// </summary>
        [Category("Zoom")]
        [Description("The minimum allowed zoom level.")]
        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register(
            nameof(MaxZoom), typeof(double), typeof(ImageViewer), new FrameworkPropertyMetadata(8.0, OnZoomLimitsChanged));

        /// <summary>
        /// Gets or sets the maximum allowed <see cref="Zoom"/>.
        /// </summary>
        [Category("Zoom")]
        [Description("The maximum allowed zoom level.")]
        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsZoomToFit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsZoomToFitProperty = DependencyProperty.Register(
            nameof(IsZoomToFit), typeof(bool), typeof(ImageViewer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsZoomToFitChanged));

        /// <summary>
        /// Gets or sets whether the zoom tracks the viewport so the whole image is always visible. Images smaller
        /// than the viewport are shown at their actual size rather than being enlarged. Any manual zoom turns this off.
        /// </summary>
        [Category("Zoom")]
        [Description("Whether the zoom tracks the viewport so the whole image is always visible.")]
        public bool IsZoomToFit
        {
            get => (bool)GetValue(IsZoomToFitProperty);
            set => SetValue(IsZoomToFitProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Rotation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(
            nameof(Rotation), typeof(double), typeof(ImageViewer),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRotationChanged, CoerceRotation));

        /// <summary>
        /// Gets or sets the display rotation in degrees. Values are normalized to 0, 90, 180 or 270. The rotation
        /// only affects how the image is shown; the source is not modified.
        /// </summary>
        [Category("Common")]
        [Description("The display rotation in degrees (0, 90, 180 or 270).")]
        public double Rotation
        {
            get => (double)GetValue(RotationProperty);
            set => SetValue(RotationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStatusBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStatusBarProperty = DependencyProperty.Register(
            nameof(ShowStatusBar), typeof(bool), typeof(ImageViewer), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the status bar (resolution, file size and zoom controls) is shown.
        /// </summary>
        [Category("Appearance")]
        [Description("Whether the status bar with resolution, file size and zoom controls is shown.")]
        public bool ShowStatusBar
        {
            get => (bool)GetValue(ShowStatusBarProperty);
            set => SetValue(ShowStatusBarProperty, value);
        }

        private static readonly DependencyPropertyKey HasImagePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasImage), typeof(bool), typeof(ImageViewer), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the read-only <see cref="HasImage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasImageProperty = HasImagePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether an image is currently loaded.
        /// </summary>
        [Browsable(false)]
        public bool HasImage => (bool)GetValue(HasImageProperty);

        private static readonly DependencyPropertyKey PixelWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PixelWidth), typeof(int), typeof(ImageViewer), new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Identifies the read-only <see cref="PixelWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PixelWidthProperty = PixelWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the width of the loaded image in pixels.
        /// </summary>
        [Browsable(false)]
        public int PixelWidth => (int)GetValue(PixelWidthProperty);

        private static readonly DependencyPropertyKey PixelHeightPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PixelHeight), typeof(int), typeof(ImageViewer), new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Identifies the read-only <see cref="PixelHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PixelHeightProperty = PixelHeightPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the height of the loaded image in pixels.
        /// </summary>
        [Browsable(false)]
        public int PixelHeight => (int)GetValue(PixelHeightProperty);

        private static readonly DependencyPropertyKey FileSizePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FileSize), typeof(long?), typeof(ImageViewer), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the read-only <see cref="FileSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileSizeProperty = FileSizePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the size in bytes of the image loaded through <see cref="ImagePath"/>, or <c>null</c> when unknown.
        /// </summary>
        [Browsable(false)]
        public long? FileSize => (long?)GetValue(FileSizeProperty);

        private static readonly DependencyPropertyKey FilePathPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FilePath), typeof(string), typeof(ImageViewer), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the read-only <see cref="FilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = FilePathPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the full path of the image file when the image was loaded from disk through
        /// <see cref="ImagePath"/>; <c>null</c> when it came from a resource, a remote URI or was set via <see cref="Source"/>.
        /// </summary>
        [Browsable(false)]
        public string? FilePath => (string?)GetValue(FilePathProperty);

        private static readonly DependencyPropertyKey ResolutionTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ResolutionText), typeof(string), typeof(ImageViewer), new FrameworkPropertyMetadata("—"));

        /// <summary>
        /// Identifies the read-only <see cref="ResolutionText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResolutionTextProperty = ResolutionTextPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the display text for the image resolution, e.g. <c>1920 × 1080 px</c>.
        /// </summary>
        [Browsable(false)]
        public string ResolutionText => (string)GetValue(ResolutionTextProperty);

        private static readonly DependencyPropertyKey FileSizeTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FileSizeText), typeof(string), typeof(ImageViewer), new FrameworkPropertyMetadata("—"));

        /// <summary>
        /// Identifies the read-only <see cref="FileSizeText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileSizeTextProperty = FileSizeTextPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the display text for the file size, e.g. <c>1.24 MB</c>.
        /// </summary>
        [Browsable(false)]
        public string FileSizeText => (string)GetValue(FileSizeTextProperty);

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="ZoomChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ZoomChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(ZoomChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(ImageViewer));

        /// <summary>
        /// Occurs when the <see cref="Zoom"/> level changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<double> ZoomChanged
        {
            add => AddHandler(ZoomChangedEvent, value);
            remove => RemoveHandler(ZoomChangedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="ImageLoaded"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ImageLoadedEvent = EventManager.RegisterRoutedEvent(
            nameof(ImageLoaded), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImageViewer));

        /// <summary>
        /// Occurs after a new image has been loaded and its metadata is available.
        /// </summary>
        public event RoutedEventHandler ImageLoaded
        {
            add => AddHandler(ImageLoadedEvent, value);
            remove => RemoveHandler(ImageLoadedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="ImageFailed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ImageFailedEvent = EventManager.RegisterRoutedEvent(
            nameof(ImageFailed), RoutingStrategy.Bubble, typeof(EventHandler<ImageViewerFailedEventArgs>), typeof(ImageViewer));

        /// <summary>
        /// Occurs when the image referenced by <see cref="ImagePath"/> could not be loaded.
        /// </summary>
        public event EventHandler<ImageViewerFailedEventArgs> ImageFailed
        {
            add => AddHandler(ImageFailedEvent, value);
            remove => RemoveHandler(ImageFailedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="OperationFailed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent OperationFailedEvent = EventManager.RegisterRoutedEvent(
            nameof(OperationFailed), RoutingStrategy.Bubble, typeof(EventHandler<ImageViewerFailedEventArgs>), typeof(ImageViewer));

        /// <summary>
        /// Occurs when a save or clipboard operation started from a command (context menu or keyboard) fails.
        /// <see cref="ImageViewerFailedEventArgs.Operation"/> identifies which one.
        /// </summary>
        public event EventHandler<ImageViewerFailedEventArgs> OperationFailed
        {
            add => AddHandler(OperationFailedEvent, value);
            remove => RemoveHandler(OperationFailedEvent, value);
        }

        #endregion

        #region Construction

        /// <summary>
        /// Initializes static metadata for the <see cref="ImageViewer"/> class.
        /// </summary>
        static ImageViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageViewer), new FrameworkPropertyMetadata(typeof(ImageViewer)));
            FocusableProperty.OverrideMetadata(typeof(ImageViewer), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewer"/> class.
        /// </summary>
        public ImageViewer()
        {
            CommandBindings.Add(new CommandBinding(ZoomInCommand, (_, _) => ZoomIn(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(ZoomOutCommand, (_, _) => ZoomOut(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(ZoomToFitCommand, (_, _) => ZoomToFit(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(ActualSizeCommand, (_, _) => ActualSize(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(RotateRightCommand, (_, _) => RotateRight(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(RotateLeftCommand, (_, _) => RotateLeft(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(SaveAsCommand, (_, _) => ShowSaveAsDialog(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(CopyImageCommand, (_, _) => TryCopyImage(), CanExecuteWhenImage));
            CommandBindings.Add(new CommandBinding(CopyPathCommand, (_, _) => TryCopyPath(), (_, e) => e.CanExecute = FilePath != null));

            InputBindings.Add(new KeyBinding(ZoomInCommand, Key.OemPlus, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ZoomInCommand, Key.Add, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ZoomOutCommand, Key.OemMinus, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ZoomOutCommand, Key.Subtract, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ZoomToFitCommand, Key.D0, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ZoomToFitCommand, Key.NumPad0, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ActualSizeCommand, Key.D1, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(ActualSizeCommand, Key.NumPad1, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(RotateRightCommand, Key.R, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(RotateLeftCommand, Key.R, ModifierKeys.Control | ModifierKeys.Shift));
            InputBindings.Add(new KeyBinding(SaveAsCommand, Key.S, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CopyImageCommand, Key.C, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(CopyPathCommand, Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        }

        private void CanExecuteWhenImage(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = HasImage;
        }

        #endregion

        #region Template

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            DetachTemplateParts();
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
            _imageHost = GetTemplateChild(PartImageHost) as FrameworkElement;
            _image = GetTemplateChild(PartImage) as Image;
            _zoomSlider = GetTemplateChild(PartZoomSlider) as Slider;

            if (_scrollViewer != null)
            {
                _scrollViewer.PreviewMouseWheel += OnScrollViewerPreviewMouseWheel;
                _scrollViewer.PreviewMouseLeftButtonDown += OnScrollViewerPreviewMouseLeftButtonDown;
                _scrollViewer.PreviewMouseMove += OnScrollViewerPreviewMouseMove;
                _scrollViewer.PreviewMouseLeftButtonUp += OnScrollViewerPreviewMouseLeftButtonUp;
                _scrollViewer.LostMouseCapture += OnScrollViewerLostMouseCapture;
                _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
                _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            }

            if (_image != null)
            {
                _image.Source = Source;
            }

            if (_zoomSlider != null)
            {
                SyncSliderRange();
                _zoomSlider.ValueChanged += OnZoomSliderValueChanged;
            }

            ApplyRotation();

            if (IsZoomToFit)
            {
                ApplyFit();
            }
            else
            {
                ApplyZoom();
            }
        }

        private void DetachTemplateParts()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.PreviewMouseWheel -= OnScrollViewerPreviewMouseWheel;
                _scrollViewer.PreviewMouseLeftButtonDown -= OnScrollViewerPreviewMouseLeftButtonDown;
                _scrollViewer.PreviewMouseMove -= OnScrollViewerPreviewMouseMove;
                _scrollViewer.PreviewMouseLeftButtonUp -= OnScrollViewerPreviewMouseLeftButtonUp;
                _scrollViewer.LostMouseCapture -= OnScrollViewerLostMouseCapture;
                _scrollViewer.SizeChanged -= OnScrollViewerSizeChanged;
                _scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;
                _scrollViewer = null;
            }

            if (_zoomSlider != null)
            {
                _zoomSlider.ValueChanged -= OnZoomSliderValueChanged;
                _zoomSlider = null;
            }

            _imageHost = null;
            _image = null;
        }

        /// <inheritdoc/>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ImageViewerAutomationPeer(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads the image at the specified file path or resource URI. Equivalent to setting <see cref="ImagePath"/>.
        /// </summary>
        /// <param name="path">A file system path, absolute URI or pack/relative resource URI.</param>
        public void LoadImage(string? path)
        {
            if (string.Equals(ImagePath, path, StringComparison.Ordinal))
            {
                // Same value: force a reload (e.g. the file changed on disk).
                LoadFromPath(path);
            }
            else
            {
                SetCurrentValue(ImagePathProperty, path);
            }
        }

        /// <summary>
        /// Zooms in one step, keeping the center of the viewport stationary.
        /// </summary>
        public void ZoomIn()
        {
            ZoomTo(Zoom * ZoomStepFactor, null);
        }

        /// <summary>
        /// Zooms out one step, keeping the center of the viewport stationary.
        /// </summary>
        public void ZoomOut()
        {
            ZoomTo(Zoom / ZoomStepFactor, null);
        }

        /// <summary>
        /// Fits the whole image into the viewport and keeps it fitted as the control resizes.
        /// </summary>
        public void ZoomToFit()
        {
            if (IsZoomToFit)
            {
                ApplyFit();
            }
            else
            {
                SetCurrentValue(IsZoomToFitProperty, true);
            }
        }

        /// <summary>
        /// Shows the image at its actual size (100%), keeping the center of the viewport stationary.
        /// </summary>
        public void ActualSize()
        {
            ZoomTo(1.0, null);
        }

        /// <summary>
        /// Rotates the displayed image 90 degrees clockwise.
        /// </summary>
        public void RotateRight()
        {
            SetCurrentValue(RotationProperty, Rotation + 90);
        }

        /// <summary>
        /// Rotates the displayed image 90 degrees counter-clockwise.
        /// </summary>
        public void RotateLeft()
        {
            SetCurrentValue(RotationProperty, Rotation - 90);
        }

        /// <summary>
        /// Sets the zoom level while keeping the image point under <paramref name="anchor"/> stationary.
        /// </summary>
        /// <param name="zoom">The requested zoom level; it is clamped to <see cref="MinZoom"/>..<see cref="MaxZoom"/>.</param>
        /// <param name="anchor">
        /// The point, relative to the viewport, that should stay fixed; <c>null</c> uses the center of the viewport.
        /// </param>
        public void ZoomTo(double zoom, Point? anchor)
        {
            if (!HasImage || _scrollViewer == null || _imageHost == null || _imageHost.ActualWidth <= 0 || _imageHost.ActualHeight <= 0)
            {
                SetCurrentValue(ZoomProperty, zoom);
                return;
            }

            var anchorPoint = anchor ?? new Point(_scrollViewer.ViewportWidth / 2, _scrollViewer.ViewportHeight / 2);

            // Remember where the anchor falls on the image as a fraction of its size.
            var inHost = _scrollViewer.TranslatePoint(anchorPoint, _imageHost);
            double relX = Math.Clamp(inHost.X / _imageHost.ActualWidth, 0, 1);
            double relY = Math.Clamp(inHost.Y / _imageHost.ActualHeight, 0, 1);

            SetCurrentValue(ZoomProperty, zoom);
            _scrollViewer.UpdateLayout();

            // Scroll so the same fraction of the image lands back under the anchor.
            var after = _imageHost.TranslatePoint(new Point(relX * _imageHost.ActualWidth, relY * _imageHost.ActualHeight), _scrollViewer);
            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + after.X - anchorPoint.X);
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + after.Y - anchorPoint.Y);
        }

        #endregion

        #region Property Changed Callbacks

        private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ImageViewer)d).LoadFromPath(e.NewValue as string);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ImageViewer)d).OnSourceChanged(e.NewValue as ImageSource);
        }

        private void OnSourceChanged(ImageSource? source)
        {
            if (_pendingBitmap != null)
            {
                _pendingBitmap.DownloadCompleted -= OnBitmapDownloadCompleted;
                _pendingBitmap = null;
            }

            if (_image != null)
            {
                _image.Source = source;
            }

            if (!_isLoadingFromPath)
            {
                SetFileSize(null);
                SetValue(FilePathPropertyKey, null);
                _originalBytes = null;
                _originalExtension = null;
            }

            if (source is BitmapSource { IsDownloading: true } downloading)
            {
                // Metadata (pixel size) isn't reliable until the download finishes.
                _pendingBitmap = downloading;
                downloading.DownloadCompleted += OnBitmapDownloadCompleted;
            }

            UpdateImageMetadata();
            SetCurrentValue(RotationProperty, 0.0);

            if (IsZoomToFit)
            {
                ApplyFit();
            }
            else
            {
                SetCurrentValue(IsZoomToFitProperty, true);
            }

            if (source != null)
            {
                RaiseEvent(new RoutedEventArgs(ImageLoadedEvent, this));
            }
        }

        private void OnBitmapDownloadCompleted(object? sender, EventArgs e)
        {
            if (_pendingBitmap != null)
            {
                _pendingBitmap.DownloadCompleted -= OnBitmapDownloadCompleted;
                _pendingBitmap = null;
            }

            UpdateImageMetadata();

            if (IsZoomToFit)
            {
                ApplyFit();
            }
            else
            {
                ApplyZoom();
            }
        }

        private static object CoerceZoom(DependencyObject d, object baseValue)
        {
            var viewer = (ImageViewer)d;
            double value = (double)baseValue;

            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return 1.0;
            }

            double min = Math.Max(viewer.MinZoom, 0.001);
            double max = Math.Max(viewer.MaxZoom, min);
            return Math.Clamp(value, min, max);
        }

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (ImageViewer)d;

            if (!viewer._isFitting && viewer.IsZoomToFit)
            {
                viewer.SetCurrentValue(IsZoomToFitProperty, false);
            }

            viewer.ApplyZoom();
            viewer.SyncSlider();
            viewer.RaiseEvent(new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue, ZoomChangedEvent));
        }

        private static void OnZoomLimitsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (ImageViewer)d;
            viewer.CoerceValue(ZoomProperty);
            viewer.SyncSliderRange();
        }

        private static void OnIsZoomToFitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                ((ImageViewer)d).ApplyFit();
            }
        }

        private static object CoerceRotation(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0.0;
            }

            double quarterTurns = Math.Round(value / 90.0);
            return ((quarterTurns % 4 + 4) % 4) * 90.0;
        }

        private static void OnRotationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (ImageViewer)d;
            viewer.ApplyRotation();

            if (viewer.IsZoomToFit)
            {
                viewer.ApplyFit();
            }
        }

        /// <inheritdoc/>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);

            if (IsZoomToFit)
            {
                ApplyFit();
            }
            else
            {
                ApplyZoom();
            }
        }

        #endregion

        #region Loading

        private void LoadFromPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SetSourceFromPath(null, null, null);
                return;
            }

            try
            {
                byte[]? bytes = null;
                string? filePath = null;
                BitmapImage bitmap;

                if (File.Exists(path))
                {
                    // Read the bytes up front so no file lock is held and the size is known.
                    bytes = File.ReadAllBytes(path);
                    filePath = Path.GetFullPath(path);
                    bitmap = CreateBitmap(new MemoryStream(bytes, writable: false));
                }
                else
                {
                    var uri = ResolveUri(path);

                    if (uri.IsAbsoluteUri && uri.Scheme == "pack")
                    {
                        var resource = Application.GetResourceStream(uri)
                                       ?? throw new FileNotFoundException($"The image resource '{path}' could not be found.", path);

                        using var stream = resource.Stream;
                        var buffer = new MemoryStream();
                        stream.CopyTo(buffer);
                        bytes = buffer.ToArray();
                        bitmap = CreateBitmap(new MemoryStream(bytes, writable: false));
                    }
                    else if (uri.IsAbsoluteUri && uri.IsFile)
                    {
                        throw new FileNotFoundException($"The image file '{path}' could not be found.", path);
                    }
                    else
                    {
                        // Remote URI: let WPF download it; the size is not known up front.
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = uri;
                        bitmap.EndInit();
                    }
                }

                _originalBytes = bytes;
                _originalExtension = bytes != null ? GetExtension(path) : null;
                SetSourceFromPath(bitmap, bytes?.LongLength, filePath);
            }
            catch (Exception ex)
            {
                SetSourceFromPath(null, null, null);
                RaiseEvent(new ImageViewerFailedEventArgs(ImageFailedEvent, this, path, ex));
            }
        }

        private static Uri ResolveUri(string path)
        {
            // A rooted "/Assets/x.png" style path is a resource reference, not an implicit file URI.
            bool isResourceStyle = (path.StartsWith('/') && !path.StartsWith("//")) || (path.StartsWith('\\') && !path.StartsWith(@"\\"));

            if (!isResourceStyle && Uri.TryCreate(path, UriKind.Absolute, out var absolute))
            {
                return absolute;
            }

            // Treat anything else as an application resource relative to the entry assembly.
            return new Uri("pack://application:,,,/" + path.TrimStart('/', '\\').Replace('\\', '/'), UriKind.Absolute);
        }

        private static BitmapImage CreateBitmap(Stream stream)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private void SetSourceFromPath(ImageSource? source, long? fileSize, string? filePath)
        {
            _isLoadingFromPath = true;

            try
            {
                if (source == null)
                {
                    _originalBytes = null;
                    _originalExtension = null;
                }

                SetFileSize(fileSize);
                SetValue(FilePathPropertyKey, filePath);
                SetCurrentValue(SourceProperty, source);
            }
            finally
            {
                _isLoadingFromPath = false;
            }
        }

        private void SetFileSize(long? fileSize)
        {
            SetValue(FileSizePropertyKey, fileSize);
            SetValue(FileSizeTextPropertyKey, fileSize.HasValue ? FormatFileSize(fileSize.Value) : "—");
        }

        private void UpdateImageMetadata()
        {
            int width = 0;
            int height = 0;

            switch (Source)
            {
                case BitmapSource { IsDownloading: false } bitmap:
                    width = bitmap.PixelWidth;
                    height = bitmap.PixelHeight;
                    break;
                case BitmapSource:
                    // Still downloading; metadata is filled in by OnBitmapDownloadCompleted.
                    break;
                case { } other:
                    width = (int)Math.Round(other.Width);
                    height = (int)Math.Round(other.Height);
                    break;
            }

            bool hasImage = width > 0 && height > 0;
            SetValue(PixelWidthPropertyKey, width);
            SetValue(PixelHeightPropertyKey, height);
            SetValue(HasImagePropertyKey, hasImage);
            SetValue(ResolutionTextPropertyKey, hasImage ? $"{width} × {height} px" : "—");
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Formats a byte count using binary units, e.g. <c>1.24 MB</c>.
        /// </summary>
        /// <param name="bytes">The number of bytes.</param>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        #endregion

        #region Save and Clipboard

        /// <summary>
        /// The file types offered by the Save As dialog, in filter order.
        /// </summary>
        private static readonly (string Name, string[] Extensions)[] SaveFormats =
        [
            ("PNG", [".png"]),
            ("JPEG", [".jpg", ".jpeg"]),
            ("Bitmap", [".bmp"]),
            ("GIF", [".gif"]),
            ("TIFF", [".tif", ".tiff"]),
        ];

        /// <summary>
        /// Saves the image as displayed (including <see cref="Rotation"/>) to <paramref name="path"/>. The format is
        /// chosen from the extension (.png, .jpg/.jpeg, .bmp, .gif, .tif/.tiff). When the image is not rotated and
        /// the extension matches the file it was loaded from, the original bytes are written unchanged so no quality
        /// or metadata is lost.
        /// </summary>
        /// <param name="path">The destination file path.</param>
        /// <exception cref="InvalidOperationException">No image is loaded.</exception>
        /// <exception cref="NotSupportedException">The extension is not a supported image format.</exception>
        public void SaveImage(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            string extension = GetExtension(path);

            if (Rotation == 0 && _originalBytes != null && IsSameFormat(extension, _originalExtension))
            {
                File.WriteAllBytes(path, _originalBytes);
                return;
            }

            var bitmap = GetDisplayedBitmap() ?? throw new InvalidOperationException("There is no image to save.");
            var encoder = CreateEncoder(extension);

            // JPEG and BMP have no alpha channel; flatten onto white so transparent areas don't turn black.
            if (encoder is JpegBitmapEncoder or BmpBitmapEncoder)
            {
                bitmap = FlattenOntoWhite(bitmap);
            }

            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = File.Create(path);
            encoder.Save(stream);
        }

        /// <summary>
        /// Copies the image as displayed (including <see cref="Rotation"/>) to the clipboard. Both a standard bitmap
        /// and a PNG (which preserves transparency in apps that support it) are placed on the clipboard.
        /// </summary>
        /// <exception cref="InvalidOperationException">No image is loaded.</exception>
        public void CopyImageToClipboard()
        {
            var bitmap = GetDisplayedBitmap() ?? throw new InvalidOperationException("There is no image to copy.");

            var png = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(png);
            png.Position = 0;

            var data = new DataObject();
            data.SetImage(bitmap);
            data.SetData("PNG", png, false);

            SetClipboard(() => Clipboard.SetDataObject(data, true));
        }

        /// <summary>
        /// Copies <see cref="FilePath"/> to the clipboard as text.
        /// </summary>
        /// <exception cref="InvalidOperationException">The image was not loaded from a file on disk.</exception>
        public void CopyPathToClipboard()
        {
            string path = FilePath ?? throw new InvalidOperationException("The image was not loaded from a file on disk.");
            SetClipboard(() => Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, path), true));
        }

        private void ShowSaveAsDialog()
        {
            string sourceName = FilePath ?? ImagePath ?? string.Empty;
            string suggestedName = Path.GetFileNameWithoutExtension(sourceName.Replace('/', Path.DirectorySeparatorChar));

            // Default to the original format; fall back to PNG for in-memory images.
            int formatIndex = Array.FindIndex(SaveFormats, f => IsSameFormat(f.Extensions[0], _originalExtension));
            formatIndex = formatIndex < 0 ? 0 : formatIndex;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save As",
                Filter = string.Join("|", SaveFormats.Select(f =>
                {
                    string patterns = string.Join(";", f.Extensions.Select(e => "*" + e));
                    return $"{f.Name} ({patterns})|{patterns}";
                })),
                FilterIndex = formatIndex + 1,
                DefaultExt = SaveFormats[formatIndex].Extensions[0],
                AddExtension = true,
                OverwritePrompt = true,
                FileName = string.IsNullOrWhiteSpace(suggestedName) ? "image" : suggestedName
            };

            if (FilePath != null)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(FilePath);
            }

            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            {
                return;
            }

            try
            {
                SaveImage(dialog.FileName);
            }
            catch (Exception ex)
            {
                RaiseEvent(new ImageViewerFailedEventArgs(OperationFailedEvent, this, dialog.FileName, ex, ImageViewerOperation.Save));
            }
        }

        private void TryCopyImage()
        {
            try
            {
                CopyImageToClipboard();
            }
            catch (Exception ex)
            {
                RaiseEvent(new ImageViewerFailedEventArgs(OperationFailedEvent, this, FilePath ?? ImagePath ?? string.Empty, ex, ImageViewerOperation.CopyImage));
            }
        }

        private void TryCopyPath()
        {
            try
            {
                CopyPathToClipboard();
            }
            catch (Exception ex)
            {
                RaiseEvent(new ImageViewerFailedEventArgs(OperationFailedEvent, this, FilePath ?? string.Empty, ex, ImageViewerOperation.CopyPath));
            }
        }

        /// <summary>
        /// Runs a clipboard write, retrying briefly when another process has the clipboard open.
        /// </summary>
        private static void SetClipboard(Action write)
        {
            const int clipboardCantOpen = unchecked((int)0x800401D0);

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    write();
                    return;
                }
                catch (COMException ex) when (ex.HResult == clipboardCantOpen && attempt < 5)
                {
                    Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// Returns the image as it is displayed: the source rendered to a bitmap if needed, with rotation applied.
        /// </summary>
        private BitmapSource? GetDisplayedBitmap()
        {
            if (!HasImage)
            {
                return null;
            }

            BitmapSource bitmap;

            if (Source is BitmapSource source)
            {
                bitmap = source;
            }
            else
            {
                // Vector sources (e.g. DrawingImage) are rasterized at their natural size.
                var visual = new DrawingVisual();

                using (var context = visual.RenderOpen())
                {
                    context.DrawImage(Source, new Rect(0, 0, PixelWidth, PixelHeight));
                }

                var target = new RenderTargetBitmap(PixelWidth, PixelHeight, 96, 96, PixelFormats.Pbgra32);
                target.Render(visual);
                bitmap = target;
            }

            if (Rotation != 0)
            {
                bitmap = new TransformedBitmap(bitmap, new RotateTransform(Rotation));
            }

            if (bitmap.CanFreeze)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }

        private static BitmapSource FlattenOntoWhite(BitmapSource bitmap)
        {
            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                var rect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                context.DrawRectangle(Brushes.White, null, rect);
                context.DrawImage(bitmap, rect);
            }

            var target = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);
            target.Freeze();
            return target;
        }

        private static BitmapEncoder CreateEncoder(string extension)
        {
            return extension switch
            {
                ".png" => new PngBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                ".bmp" => new BmpBitmapEncoder(),
                ".gif" => new GifBitmapEncoder(),
                ".tif" or ".tiff" => new TiffBitmapEncoder(),
                _ => throw new NotSupportedException($"Saving images with the extension '{extension}' is not supported.")
            };
        }

        private static string GetExtension(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant();
        }

        private static bool IsSameFormat(string? a, string? b)
        {
            static string? Normalize(string? e) => e switch
            {
                ".jpeg" => ".jpg",
                ".tiff" => ".tif",
                _ => e
            };

            return a != null && Normalize(a) == Normalize(b);
        }

        #endregion

        #region Layout

        private bool IsQuarterTurned => Rotation is 90.0 or 270.0;

        private void ApplyZoom()
        {
            if (_image == null)
            {
                return;
            }

            if (!HasImage)
            {
                _image.Width = double.NaN;
                _image.Height = double.NaN;
                return;
            }

            // Size the image in DIPs so that Zoom == 1.0 maps one image pixel to one device pixel.
            var dpi = VisualTreeHelper.GetDpi(this);
            _image.Width = PixelWidth * Zoom / dpi.DpiScaleX;
            _image.Height = PixelHeight * Zoom / dpi.DpiScaleY;
        }

        private void ApplyRotation()
        {
            if (_image == null)
            {
                return;
            }

            _image.LayoutTransform = Rotation == 0 ? Transform.Identity : new RotateTransform(Rotation);
        }

        private void ApplyFit()
        {
            if (!HasImage || _scrollViewer == null || _scrollViewer.ActualWidth <= 0 || _scrollViewer.ActualHeight <= 0)
            {
                ApplyZoom();
                return;
            }

            var dpi = VisualTreeHelper.GetDpi(this);
            double imageWidth = IsQuarterTurned ? PixelHeight : PixelWidth;
            double imageHeight = IsQuarterTurned ? PixelWidth : PixelHeight;

            // Leave a DIP of slack so rounding never makes the auto scrollbars appear.
            double availableWidth = Math.Max(_scrollViewer.ActualWidth - 1, 1) * dpi.DpiScaleX;
            double availableHeight = Math.Max(_scrollViewer.ActualHeight - 1, 1) * dpi.DpiScaleY;

            // Like Windows Photos, small images are shown at 100% rather than being enlarged.
            double fit = Math.Min(1.0, Math.Min(availableWidth / imageWidth, availableHeight / imageHeight));

            _isFitting = true;

            try
            {
                SetCurrentValue(ZoomProperty, fit);
            }
            finally
            {
                _isFitting = false;
            }

            // The callback won't fire if the value didn't change, so make sure the layout is current.
            ApplyZoom();
        }

        private void SyncSliderRange()
        {
            if (_zoomSlider == null)
            {
                return;
            }

            // The slider works on a log2 scale so each notch feels the same at 10% as it does at 400%.
            _suppressSliderChanged = true;

            try
            {
                _zoomSlider.Minimum = Math.Log2(Math.Max(MinZoom, 0.001));
                _zoomSlider.Maximum = Math.Log2(Math.Max(MaxZoom, MinZoom));
                _zoomSlider.SmallChange = Math.Log2(ZoomStepFactor);
                _zoomSlider.LargeChange = 1;
                _zoomSlider.Value = Math.Log2(Zoom);
            }
            finally
            {
                _suppressSliderChanged = false;
            }
        }

        private void SyncSlider()
        {
            if (_zoomSlider == null)
            {
                return;
            }

            _suppressSliderChanged = true;

            try
            {
                _zoomSlider.Value = Math.Log2(Zoom);
            }
            finally
            {
                _suppressSliderChanged = false;
            }
        }

        private void UpdatePanCursor()
        {
            if (_scrollViewer == null)
            {
                return;
            }

            if (_isPanning)
            {
                _scrollViewer.Cursor = Cursors.SizeAll;
            }
            else
            {
                _scrollViewer.Cursor = CanPan ? Cursors.Hand : null;
            }
        }

        private bool CanPan => _scrollViewer != null
                               && (_scrollViewer.ExtentWidth > _scrollViewer.ViewportWidth + 0.5
                                   || _scrollViewer.ExtentHeight > _scrollViewer.ViewportHeight + 0.5);

        #endregion

        #region Input

        private void OnZoomSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderChanged)
            {
                return;
            }

            ZoomTo(Math.Pow(2, e.NewValue), null);
        }

        private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsZoomToFit)
            {
                ApplyFit();
            }
        }

        private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdatePanCursor();
        }

        private void OnScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!HasImage || _scrollViewer == null)
            {
                return;
            }

            // Fractional deltas (precision touchpads) produce proportionally smaller zoom steps.
            double factor = Math.Pow(ZoomStepFactor, e.Delta / (double)Mouse.MouseWheelDeltaForOneLine);
            ZoomTo(Zoom * factor, e.GetPosition(_scrollViewer));
            e.Handled = true;
        }

        private void OnScrollViewerPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_scrollViewer == null || IsInsideScrollBar(e.OriginalSource as DependencyObject))
            {
                return;
            }

            Focus();

            if (!HasImage)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                // Double-click toggles between fitted and actual size around the clicked point.
                if (IsZoomToFit && Math.Abs(Zoom - 1.0) > 0.0001)
                {
                    ZoomTo(1.0, e.GetPosition(_scrollViewer));
                }
                else
                {
                    ZoomToFit();
                }

                e.Handled = true;
                return;
            }

            if (!CanPan)
            {
                return;
            }

            _isPanning = true;
            _panStartPoint = e.GetPosition(_scrollViewer);
            _panStartHorizontalOffset = _scrollViewer.HorizontalOffset;
            _panStartVerticalOffset = _scrollViewer.VerticalOffset;
            _scrollViewer.CaptureMouse();
            UpdatePanCursor();
            e.Handled = true;
        }

        private void OnScrollViewerPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning || _scrollViewer == null)
            {
                return;
            }

            var position = e.GetPosition(_scrollViewer);
            _scrollViewer.ScrollToHorizontalOffset(_panStartHorizontalOffset - (position.X - _panStartPoint.X));
            _scrollViewer.ScrollToVerticalOffset(_panStartVerticalOffset - (position.Y - _panStartPoint.Y));
            e.Handled = true;
        }

        private void OnScrollViewerPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isPanning)
            {
                return;
            }

            _scrollViewer?.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnScrollViewerLostMouseCapture(object sender, MouseEventArgs e)
        {
            _isPanning = false;
            UpdatePanCursor();
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || !HasImage || Keyboard.Modifiers != ModifierKeys.None)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                    ZoomIn();
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ZoomOut();
                    e.Handled = true;
                    break;
                case Key.Left:
                    e.Handled = Pan(-KeyboardPanStep, 0);
                    break;
                case Key.Right:
                    e.Handled = Pan(KeyboardPanStep, 0);
                    break;
                case Key.Up:
                    e.Handled = Pan(0, -KeyboardPanStep);
                    break;
                case Key.Down:
                    e.Handled = Pan(0, KeyboardPanStep);
                    break;
            }
        }

        private bool Pan(double dx, double dy)
        {
            if (_scrollViewer == null || !CanPan)
            {
                return false;
            }

            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + dx);
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + dy);
            return true;
        }

        private static bool IsInsideScrollBar(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is ScrollBar)
                {
                    return true;
                }

                if (element is ScrollViewer)
                {
                    return false;
                }

                element = element is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(element)
                    : LogicalTreeHelper.GetParent(element);
            }

            return false;
        }

        #endregion
    }
}
