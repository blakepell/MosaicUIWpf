/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using static Mosaic.UI.Wpf.Controls.TrayIconNative.NativeMethods;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Displays an icon in the Windows notification area (system tray) with a genuine WPF <see cref="System.Windows.Controls.ContextMenu"/>,
    /// click events and commands. No Windows Forms or System.Drawing dependency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Lifecycle.</b> Creating a <see cref="TrayIcon"/> (in code or as a XAML resource) creates no shell state. The icon
    /// is registered when <see cref="IsVisible"/> becomes <see langword="true"/> (or <see cref="Show"/> is called) and
    /// removed when it becomes <see langword="false"/> (or <see cref="Hide"/> is called); both paths drive the same
    /// property. <see cref="Dispose()"/> removes the icon permanently and may be called any number of times. A long-lived
    /// application icon should be kept in a field or resource and disposed at shutdown; the component also removes itself
    /// when its dispatcher shuts down, so the icon never lingers after a normal exit.
    /// </para>
    /// <para>
    /// <b>Events and commands.</b> For every interaction the CLR event is raised first, then the matching command is
    /// executed if <see cref="ICommand.CanExecute"/> returns <see langword="true"/> and the event was not marked
    /// <see cref="TrayIconEventArgs.Handled"/>. A right click (or Shift+F10 / the Menu key) raises <see cref="RightClick"/>,
    /// executes <see cref="RightClickCommand"/> and then opens <see cref="ContextMenu"/>. When a double-click handler or
    /// <see cref="DoubleClickCommand"/> is present, <see cref="LeftClick"/> is delayed by the system double-click time so
    /// that a double-click never also produces a single click; otherwise it fires immediately. Pressing Enter or Space on
    /// the focused icon raises <see cref="LeftClick"/> (or <see cref="DoubleClick"/> when only double-click is handled)
    /// with <see cref="TrayIconInputSource.Keyboard"/>.
    /// </para>
    /// <para>
    /// <b>Data context.</b> The context menu lives outside any visual tree, so unless the menu has its own
    /// <see cref="FrameworkElement.DataContext"/>, the component binds it to <see cref="DataContext"/>. When the tray icon is
    /// declared in an element's resources, it inherits that element's data context automatically; in
    /// <c>Application.Resources</c> set <see cref="DataContext"/> from code.
    /// </para>
    /// <para>
    /// <b>Tray-only applications.</b> No window is required. Set <c>ShutdownMode="OnExplicitShutdown"</c> on the
    /// application (the component never changes it) and call <see cref="Application.Shutdown()"/> from an Exit menu item.
    /// </para>
    /// <para>
    /// <b>Threading.</b> The component is dispatcher-affine like any WPF object: all members must be used on the thread
    /// that created it. <see cref="Dispose()"/> called from another thread is marshaled asynchronously to the owner.
    /// </para>
    /// <para>
    /// <b>Freezable.</b> <see cref="TrayIcon"/> derives from <see cref="Freezable"/> so it gets an inheritance context (and
    /// therefore working bindings) when declared in resources. It can never be frozen, and clones are always created
    /// hidden, so a copy never owns a shell registration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var tray = new TrayIcon
    /// {
    ///     Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/tray.png")),
    ///     ToolTipText = "Mosaic Application",
    ///     ContextMenu = new ContextMenu
    ///     {
    ///         Items = { new MenuItem { Header = "Open" }, new Separator(), new MenuItem { Header = "Exit" } }
    ///     }
    /// };
    ///
    /// tray.DoubleClick += (_, _) => Application.Current.MainWindow?.Show();
    /// tray.Show();
    ///
    /// // At shutdown:
    /// tray.Dispose();
    /// ]]></code>
    /// <code language="xml"><![CDATA[
    /// <Application.Resources>
    ///     <mosaic:TrayIcon x:Key="ApplicationTray" Icon="/Assets/tray.png" ToolTipText="Mosaic Application">
    ///         <ContextMenu>
    ///             <MenuItem Header="Open" Command="{Binding OpenCommand}" />
    ///             <Separator />
    ///             <MenuItem Header="Exit" Command="{Binding ExitCommand}" />
    ///         </ContextMenu>
    ///     </mosaic:TrayIcon>
    /// </Application.Resources>
    ///
    /// // App.OnStartup:  var tray = (TrayIcon)FindResource("ApplicationTray"); tray.DataContext = viewModel; tray.Show();
    /// // App.OnExit:     ((TrayIcon)FindResource("ApplicationTray")).Dispose();
    /// ]]></code>
    /// </example>
    [DefaultEvent(nameof(DoubleClick))]
    [DefaultProperty(nameof(Icon))]
    [ContentProperty(nameof(ContextMenu))]
    public class TrayIcon : Freezable, IDisposable, ISupportInitialize
    {
        /// <summary>
        /// A <see cref="TrayNotificationKind.ContextMenu"/> arriving within this window after a right-button-up is
        /// attributed to the mouse; otherwise it came from the keyboard.
        /// </summary>
        private const long MouseContextMenuWindowMs = 1000;

        /// <summary>
        /// The shell can deliver NIN_KEYSELECT twice for a single Enter press; repeats inside this window are ignored.
        /// </summary>
        private const long KeySelectDebounceMs = 250;

        /// <summary>
        /// "Never happened" timestamp. Far enough in the past to exceed every window, near enough that
        /// subtracting it from <see cref="Environment.TickCount64"/> cannot overflow.
        /// </summary>
        private const long NoTick = int.MinValue;

        private static readonly TimeSpan HoverCheckInterval = TimeSpan.FromMilliseconds(200);

        private TrayIconNative? _native;
        private bool _disposed;
        private int _suppressRegistration;

        // Icon conversion cache: the source and pixel size last pushed to the native layer.
        private ImageSource? _renderedSource;
        private int _renderedSize;
        private bool _hasRenderedIcon;
        private BitmapSource? _downloadingSource;

        // Click / double-click disambiguation.
        private DispatcherTimer? _clickTimer;
        private Point _pendingClickPosition;
        private bool _suppressNextLeftUp;
        private long _lastRightUpTick = NoTick;
        private long _lastKeySelectTick = NoTick;

        // Hover tracking (timer only runs while the cursor is over the icon).
        private DispatcherTimer? _hoverTimer;
        private bool _isHovering;
        private TrayNotification _lastMove;
        private long _lastMoveTick;

        // Context menu state.
        private ContextMenu? _attachedMenu;
        private Binding? _menuDataContextBinding;
        private bool _menuOpenedByTray;
        private POINT _menuAnchor;
        private TrayIconInputSource _menuInputSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayIcon"/> class. No shell state is created until the icon is shown.
        /// </summary>
        public TrayIcon()
        {
            // A source-less binding on a Freezable resolves against the data context of the element that provides
            // its inheritance context (e.g. the element whose Resources declare it). DataContext surfaces that value
            // through coercion whenever it has no local value of its own.
            BindingOperations.SetBinding(this, InheritedDataContextProperty, new Binding());
        }

        #region Dependency properties

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(ImageSource), typeof(TrayIcon),
            new FrameworkPropertyMetadata(null, OnIconChanged));

        /// <summary>
        /// Gets or sets the image shown in the notification area. Any <see cref="ImageSource"/> works, including PNG/ICO
        /// resources and vector <see cref="DrawingImage"/>s; it is rendered at the shell's small-icon size for the
        /// current DPI. When <see langword="null"/> (or not renderable), the executable's own icon is used.
        /// </summary>
        [Category("Common")]
        [Description("The image shown in the notification area.")]
        public ImageSource? Icon
        {
            get => (ImageSource?)this.GetValue(IconProperty);
            set => this.SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToolTipText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToolTipTextProperty = DependencyProperty.Register(
            nameof(ToolTipText), typeof(string), typeof(TrayIcon),
            new FrameworkPropertyMetadata(null, OnToolTipTextChanged));

        /// <summary>
        /// Gets or sets the native tooltip shown when hovering the icon. Text longer than 127 characters is truncated.
        /// </summary>
        [Category("Common")]
        [Description("The native tooltip shown when hovering the icon.")]
        public string? ToolTipText
        {
            get => (string?)this.GetValue(ToolTipTextProperty);
            set => this.SetValue(ToolTipTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ContextMenu"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContextMenuProperty = DependencyProperty.Register(
            nameof(ContextMenu), typeof(ContextMenu), typeof(TrayIcon),
            new FrameworkPropertyMetadata(null, OnContextMenuChanged));

        /// <summary>
        /// Gets or sets the WPF context menu opened when the user right-clicks the icon or presses Shift+F10 / the Menu key on it.
        /// </summary>
        [Category("Common")]
        [Description("The WPF context menu opened from the tray icon.")]
        public ContextMenu? ContextMenu
        {
            get => (ContextMenu?)this.GetValue(ContextMenuProperty);
            set => this.SetValue(ContextMenuProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(
            nameof(IsVisible), typeof(bool), typeof(TrayIcon),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsVisibleChanged, CoerceIsVisible));

        /// <summary>
        /// Gets or sets whether the icon is registered in the notification area. This is the same state that
        /// <see cref="Show"/> and <see cref="Hide"/> change. Always <see langword="false"/> after disposal.
        /// </summary>
        [Category("Common")]
        [Description("Whether the icon is registered in the notification area.")]
        public bool IsVisible
        {
            get => (bool)this.GetValue(IsVisibleProperty);
            set => this.SetValue(IsVisibleProperty, value);
        }

        private static readonly DependencyPropertyKey IsRegisteredPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsRegistered), typeof(bool), typeof(TrayIcon), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsRegistered"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRegisteredProperty = IsRegisteredPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the shell has actually accepted the icon. This can be <see langword="false"/> while
        /// <see cref="IsVisible"/> is <see langword="true"/> if Explorer is not running yet (for example at logon);
        /// registration is retried automatically when the taskbar is created.
        /// </summary>
        [Category("Common")]
        [Description("Whether the shell has accepted the icon registration.")]
        public bool IsRegistered => (bool)this.GetValue(IsRegisteredProperty);

        /// <summary>
        /// Identifies the <see cref="DataContext"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataContextProperty = FrameworkElement.DataContextProperty.AddOwner(
            typeof(TrayIcon), new FrameworkPropertyMetadata(null, OnDataContextChanged, CoerceDataContext));

        private static void OnDataContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // ClearValue falls back to the default without coercing; re-coerce so inheritance resumes.
            if (e.NewValue == null && d.GetValue(InheritedDataContextProperty) != null)
            {
                d.CoerceValue(DataContextProperty);
            }
        }

        /// <summary>
        /// Receives the data context of the inheritance-context mentor (see the constructor).
        /// </summary>
        private static readonly DependencyProperty InheritedDataContextProperty = DependencyProperty.Register(
            "InheritedDataContext", typeof(object), typeof(TrayIcon),
            new FrameworkPropertyMetadata(null, (d, _) => d.CoerceValue(DataContextProperty)));

        private static object? CoerceDataContext(DependencyObject d, object? baseValue)
        {
            // A non-null value (set or bound) always wins; null means "inherit from the declaring element".
            // (ReadLocalValue cannot be used here: during SetValue it still reports the previous value.)
            return baseValue ?? d.GetValue(InheritedDataContextProperty);
        }

        /// <summary>
        /// Gets or sets the data context the <see cref="ContextMenu"/> is bound to (unless the menu sets its own).
        /// </summary>
        /// <remarks>
        /// When not set (or <see langword="null"/>), a tray icon declared in an element's <see cref="FrameworkElement.Resources"/> inherits that
        /// element's data context. Do not write <c>DataContext="{Binding}"</c> in XAML: WPF resolves a binding that
        /// targets a <c>DataContext</c> property against the <i>parent</i> element's context.
        /// </remarks>
        [Category("Common")]
        [Description("The data context provided to the context menu.")]
        public object? DataContext
        {
            get => this.GetValue(DataContextProperty);
            set => this.SetValue(DataContextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LeftClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftClickCommandProperty = DependencyProperty.Register(
            nameof(LeftClickCommand), typeof(ICommand), typeof(TrayIcon), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed after <see cref="LeftClick"/>.
        /// </summary>
        [Category("Action")]
        [Description("The command executed when the icon is left-clicked.")]
        public ICommand? LeftClickCommand
        {
            get => (ICommand?)this.GetValue(LeftClickCommandProperty);
            set => this.SetValue(LeftClickCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DoubleClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DoubleClickCommandProperty = DependencyProperty.Register(
            nameof(DoubleClickCommand), typeof(ICommand), typeof(TrayIcon), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed after <see cref="DoubleClick"/>.
        /// </summary>
        [Category("Action")]
        [Description("The command executed when the icon is double-clicked.")]
        public ICommand? DoubleClickCommand
        {
            get => (ICommand?)this.GetValue(DoubleClickCommandProperty);
            set => this.SetValue(DoubleClickCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RightClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RightClickCommandProperty = DependencyProperty.Register(
            nameof(RightClickCommand), typeof(ICommand), typeof(TrayIcon), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed after <see cref="RightClick"/> and before the context menu opens.
        /// </summary>
        [Category("Action")]
        [Description("The command executed when the icon is right-clicked.")]
        public ICommand? RightClickCommand
        {
            get => (ICommand?)this.GetValue(RightClickCommandProperty);
            set => this.SetValue(RightClickCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(TrayIcon), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="LeftClickCommand"/>, <see cref="DoubleClickCommand"/> and <see cref="RightClickCommand"/>.
        /// </summary>
        [Category("Action")]
        [Description("The parameter passed to the click commands.")]
        public object? CommandParameter
        {
            get => this.GetValue(CommandParameterProperty);
            set => this.SetValue(CommandParameterProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the icon is left-clicked, or activated with Enter/Space. Delayed by the double-click time when
        /// double-click is also handled.
        /// </summary>
        public event EventHandler<TrayIconEventArgs>? LeftClick;

        /// <summary>
        /// Occurs when the icon is double-clicked with the left button.
        /// </summary>
        public event EventHandler<TrayIconEventArgs>? DoubleClick;

        /// <summary>
        /// Occurs when the user requests the icon's context menu with a right click or from the keyboard, before the menu opens.
        /// Set <see cref="TrayIconEventArgs.Handled"/> to suppress <see cref="RightClickCommand"/> and the menu.
        /// </summary>
        public event EventHandler<TrayIconEventArgs>? RightClick;

        /// <summary>
        /// Occurs when the mouse cursor moves onto the icon.
        /// </summary>
        public event EventHandler<TrayIconEventArgs>? MouseEnter;

        /// <summary>
        /// Occurs when the mouse cursor leaves the icon.
        /// </summary>
        public event EventHandler<TrayIconEventArgs>? MouseLeave;

        #endregion

        /// <summary>
        /// Gets the native layer. Exposed to the test assembly only.
        /// </summary>
        internal TrayIconNative? Native => _native;

        /// <summary>
        /// Registers the icon in the notification area. Equivalent to setting <see cref="IsVisible"/> to <see langword="true"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The tray icon has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Called from a thread other than the owning dispatcher thread.</exception>
        public void Show()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            this.SetCurrentValue(IsVisibleProperty, true);
        }

        /// <summary>
        /// Removes the icon from the notification area. Equivalent to setting <see cref="IsVisible"/> to <see langword="false"/>.
        /// The instance can be shown again later.
        /// </summary>
        public void Hide()
        {
            this.SetCurrentValue(IsVisibleProperty, false);
        }

        /// <summary>
        /// Opens <see cref="ContextMenu"/> next to the tray icon, as if the user had requested it from the keyboard.
        /// Does nothing if there is no menu or the icon is not shown.
        /// </summary>
        public void ShowContextMenu()
        {
            this.VerifyAccess();

            if (_native == null)
            {
                return;
            }

            GetCursorPos(out var cursor);
            this.OpenContextMenu(cursor.X, cursor.Y, TrayIconInputSource.Keyboard);
        }

        /// <summary>
        /// Removes the icon and releases all native resources. Safe to call repeatedly.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases native resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
            {
                return;
            }

            if (!this.CheckAccess())
            {
                if (!this.Dispatcher.HasShutdownFinished)
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(this.Dispose));
                }

                return;
            }

            _disposed = true;
            this.TearDownNative();
            this.DetachMenu(_attachedMenu);
            _attachedMenu = null;
            this.CoerceValue(IsVisibleProperty);
        }

        #region ISupportInitialize

        /// <summary>
        /// Defers native registration until <see cref="EndInit"/> (used by the XAML parser).
        /// </summary>
        public void BeginInit()
        {
            _suppressRegistration++;
        }

        /// <summary>
        /// Applies any deferred registration.
        /// </summary>
        public void EndInit()
        {
            if (_suppressRegistration > 0)
            {
                _suppressRegistration--;
            }

            this.UpdateRegistration();
        }

        #endregion

        #region Freezable

        /// <inheritdoc />
        protected override Freezable CreateInstanceCore() => new TrayIcon();

        /// <summary>
        /// A tray icon owns mutable native state and can never be frozen.
        /// </summary>
        /// <returns>Always <see langword="false"/>.</returns>
        protected override bool FreezeCore(bool isChecking) => false;

        /// <inheritdoc />
        protected override void CloneCore(Freezable sourceFreezable)
        {
            this.CloneHidden(() => base.CloneCore(sourceFreezable));
        }

        /// <inheritdoc />
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            this.CloneHidden(() => base.CloneCurrentValueCore(sourceFreezable));
        }

        /// <inheritdoc />
        protected override void GetAsFrozenCore(Freezable sourceFreezable)
        {
            this.CloneHidden(() => base.GetAsFrozenCore(sourceFreezable));
        }

        /// <inheritdoc />
        protected override void GetCurrentValueAsFrozenCore(Freezable sourceFreezable)
        {
            this.CloneHidden(() => base.GetCurrentValueAsFrozenCore(sourceFreezable));
        }

        private void CloneHidden(Action copy)
        {
            _suppressRegistration++;

            try
            {
                copy();
            }
            finally
            {
                _suppressRegistration--;
            }

            // Clones never inherit a registration.
            this.ClearValue(IsVisibleProperty);
        }

        #endregion

        #region Property callbacks

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tray = (TrayIcon)d;

            if (tray._native != null)
            {
                tray.RefreshIcon(force: true);
                tray.ApplyNative();
            }
        }

        private static void OnToolTipTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tray = (TrayIcon)d;

            if (tray._native != null)
            {
                tray._native.SetToolTip((string?)e.NewValue);
                tray.ApplyNative();
            }
        }

        private static void OnContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tray = (TrayIcon)d;

            if (tray._disposed)
            {
                return;
            }

            tray.DetachMenu(e.OldValue as ContextMenu);
            tray._attachedMenu = e.NewValue as ContextMenu;
            tray.AttachMenu(tray._attachedMenu);
        }

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TrayIcon)d).UpdateRegistration();
        }

        private static object CoerceIsVisible(DependencyObject d, object baseValue)
        {
            return ((TrayIcon)d)._disposed ? false : baseValue;
        }

        #endregion

        #region Native registration

        private void UpdateRegistration()
        {
            if (_suppressRegistration > 0 || _disposed)
            {
                return;
            }

            if (this.IsVisible)
            {
                if (_native == null)
                {
                    _native = new TrayIconNative(this.OnNotification);
                    this.Dispatcher.ShutdownStarted += this.OnDispatcherShutdownStarted;
                    _native.SetToolTip(this.ToolTipText);
                }

                this.RefreshIcon(force: false);
                this.ApplyNative();
            }
            else
            {
                this.TearDownNative();
            }
        }

        private void ApplyNative()
        {
            if (_native == null)
            {
                return;
            }

            bool registered = _native.Apply();

            if (registered != this.IsRegistered)
            {
                this.SetValue(IsRegisteredPropertyKey, registered);
            }
        }

        private void TearDownNative()
        {
            if (_native == null)
            {
                return;
            }

            if (_menuOpenedByTray && _attachedMenu is { IsOpen: true } menu)
            {
                menu.IsOpen = false;
            }

            _clickTimer?.Stop();
            _hoverTimer?.Stop();
            _isHovering = false;
            _suppressNextLeftUp = false;
            this.UnhookDownload();

            this.Dispatcher.ShutdownStarted -= this.OnDispatcherShutdownStarted;
            _native.Dispose();
            _native = null;

            _renderedSource = null;
            _hasRenderedIcon = false;

            if (this.IsRegistered)
            {
                this.SetValue(IsRegisteredPropertyKey, false);
            }
        }

        private void OnDispatcherShutdownStarted(object? sender, EventArgs e)
        {
            // Remove the icon before the message window is torn down so no "ghost" icon remains after exit.
            this.Dispose();
        }

        /// <summary>
        /// Converts <see cref="Icon"/> and hands the handle to the native layer, skipping the work when neither the
        /// source nor the required pixel size changed.
        /// </summary>
        private void RefreshIcon(bool force)
        {
            if (_native == null)
            {
                return;
            }

            int size = TrayIconImageConverter.GetTrayIconSize();
            var source = this.Icon;

            if (!force && _hasRenderedIcon && ReferenceEquals(source, _renderedSource) && size == _renderedSize)
            {
                return;
            }

            this.UnhookDownload();
            TrayIconHandle? handle = null;

            if (source is BitmapSource { IsDownloading: true } downloading)
            {
                // Show the default icon until the download completes, then convert again.
                _downloadingSource = downloading;
                downloading.DownloadCompleted += this.OnIconDownloadFinished;
                downloading.DownloadFailed += this.OnIconDownloadFinished;
            }
            else if (source != null)
            {
                handle = TrayIconImageConverter.Convert(source, size);
            }

            handle ??= TrayIconImageConverter.GetDefaultIcon(size);

            _native.SetIcon(handle);
            _renderedSource = source;
            _renderedSize = size;
            _hasRenderedIcon = true;
        }

        private void OnIconDownloadFinished(object? sender, EventArgs e)
        {
            this.UnhookDownload();

            if (_native != null && ReferenceEquals(sender, this.Icon))
            {
                this.RefreshIcon(force: true);
                this.ApplyNative();
            }
        }

        private void UnhookDownload()
        {
            if (_downloadingSource != null)
            {
                _downloadingSource.DownloadCompleted -= this.OnIconDownloadFinished;
                _downloadingSource.DownloadFailed -= this.OnIconDownloadFinished;
                _downloadingSource = null;
            }
        }

        #endregion

        #region Notifications

        private void OnNotification(TrayNotification n)
        {
            if (_disposed || _native == null)
            {
                return;
            }

            switch (n.Kind)
            {
                case TrayNotificationKind.MouseMove:
                case TrayNotificationKind.PopupOpen:
                    _lastMove = n;
                    _lastMoveTick = Environment.TickCount64;
                    this.BeginHover(n);
                    break;

                case TrayNotificationKind.PopupClose:
                    this.EndHover();
                    break;

                case TrayNotificationKind.LeftButtonDown:
                    break;

                case TrayNotificationKind.LeftButtonUp:
                    this.OnLeftButtonUp(n);
                    break;

                case TrayNotificationKind.LeftButtonDoubleClick:
                    _clickTimer?.Stop();
                    _suppressNextLeftUp = true;
                    this.RaiseInteraction(this.DoubleClick, this.DoubleClickCommand, new TrayIconEventArgs(TrayIconInputSource.Mouse, new Point(n.X, n.Y)));
                    break;

                case TrayNotificationKind.RightButtonUp:
                    _lastRightUpTick = Environment.TickCount64;
                    break;

                case TrayNotificationKind.ContextMenu:
                    this.OnContextMenuRequested(n);
                    break;

                case TrayNotificationKind.KeySelect:
                    this.OnKeySelect(n);
                    break;

                case TrayNotificationKind.TaskbarCreated:
                    // Explorer restarted (or the taskbar was recreated for a DPI change): the icon size may have
                    // changed and the registration is gone. Only icons that are still meant to be visible come back.
                    if (this.IsVisible)
                    {
                        this.RefreshIcon(force: false);
                        this.ApplyNative();
                    }
                    else if (this.IsRegistered)
                    {
                        this.SetValue(IsRegisteredPropertyKey, false);
                    }

                    break;

                case TrayNotificationKind.DisplaySettingsChanged:
                    if (TrayIconImageConverter.GetTrayIconSize() != _renderedSize)
                    {
                        this.RefreshIcon(force: true);
                        this.ApplyNative();
                    }

                    break;
            }
        }

        private bool HasDoubleClickListeners => this.DoubleClick != null || this.DoubleClickCommand != null;

        private bool HasLeftClickListeners => this.LeftClick != null || this.LeftClickCommand != null;

        private void OnLeftButtonUp(TrayNotification n)
        {
            if (_suppressNextLeftUp)
            {
                // The button-up that completes a double-click.
                _suppressNextLeftUp = false;
                return;
            }

            var position = new Point(n.X, n.Y);

            if (!this.HasDoubleClickListeners)
            {
                this.RaiseInteraction(this.LeftClick, this.LeftClickCommand, new TrayIconEventArgs(TrayIconInputSource.Mouse, position));
                return;
            }

            if (_clickTimer == null)
            {
                _clickTimer = new DispatcherTimer(DispatcherPriority.Input, this.Dispatcher);
                _clickTimer.Tick += this.OnClickTimerTick;
            }

            _clickTimer.Stop();
            _clickTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1u, GetDoubleClickTime()));
            _pendingClickPosition = position;
            _clickTimer.Start();
        }

        private void OnClickTimerTick(object? sender, EventArgs e)
        {
            _clickTimer?.Stop();

            if (_native != null)
            {
                this.RaiseInteraction(this.LeftClick, this.LeftClickCommand, new TrayIconEventArgs(TrayIconInputSource.Mouse, _pendingClickPosition));
            }
        }

        private void OnKeySelect(TrayNotification n)
        {
            long now = Environment.TickCount64;

            if (now - _lastKeySelectTick < KeySelectDebounceMs)
            {
                return;
            }

            _lastKeySelectTick = now;
            var args = new TrayIconEventArgs(TrayIconInputSource.Keyboard, new Point(n.X, n.Y));

            if (!this.HasLeftClickListeners && this.HasDoubleClickListeners)
            {
                this.RaiseInteraction(this.DoubleClick, this.DoubleClickCommand, args);
            }
            else
            {
                this.RaiseInteraction(this.LeftClick, this.LeftClickCommand, args);
            }
        }

        private void OnContextMenuRequested(TrayNotification n)
        {
            var source = Environment.TickCount64 - _lastRightUpTick <= MouseContextMenuWindowMs
                ? TrayIconInputSource.Mouse
                : TrayIconInputSource.Keyboard;

            _lastRightUpTick = NoTick;

            var args = new TrayIconEventArgs(source, new Point(n.X, n.Y));
            this.RaiseInteraction(this.RightClick, this.RightClickCommand, args);

            if (!args.Handled && _native != null)
            {
                this.OpenContextMenu(n.X, n.Y, source);
            }
        }

        private void RaiseInteraction(EventHandler<TrayIconEventArgs>? handler, ICommand? command, TrayIconEventArgs args)
        {
            handler?.Invoke(this, args);

            if (args.Handled || command == null)
            {
                return;
            }

            object? parameter = this.CommandParameter;

            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        private void BeginHover(TrayNotification n)
        {
            if (_isHovering)
            {
                return;
            }

            _isHovering = true;

            if (_hoverTimer == null)
            {
                _hoverTimer = new DispatcherTimer(DispatcherPriority.Background, this.Dispatcher) { Interval = HoverCheckInterval };
                _hoverTimer.Tick += this.OnHoverTimerTick;
            }

            _hoverTimer.Start();
            this.MouseEnter?.Invoke(this, new TrayIconEventArgs(TrayIconInputSource.Mouse, new Point(n.X, n.Y)));
        }

        private void OnHoverTimerTick(object? sender, EventArgs e)
        {
            if (_native == null || !GetCursorPos(out var cursor))
            {
                this.EndHover();
                return;
            }

            if (_native.TryGetIconRect(out var rect))
            {
                if (!rect.Contains(cursor.X, cursor.Y))
                {
                    this.EndHover();
                }

                return;
            }

            // No rectangle from the shell: treat the cursor as gone once it has moved and the icon stopped
            // reporting mouse moves.
            if (Environment.TickCount64 - _lastMoveTick > HoverCheckInterval.TotalMilliseconds * 2
                && (cursor.X != _lastMove.X || cursor.Y != _lastMove.Y))
            {
                this.EndHover();
            }
        }

        private void EndHover()
        {
            if (!_isHovering)
            {
                return;
            }

            _isHovering = false;
            _hoverTimer?.Stop();

            GetCursorPos(out var cursor);
            this.MouseLeave?.Invoke(this, new TrayIconEventArgs(TrayIconInputSource.Mouse, new Point(cursor.X, cursor.Y)));
        }

        #endregion

        #region Context menu

        private void AttachMenu(ContextMenu? menu)
        {
            if (menu == null)
            {
                return;
            }

            menu.Opened += this.OnMenuOpened;
            menu.Closed += this.OnMenuClosed;

            // Respect a data context the consumer set (or bound) on the menu itself.
            if (menu.ReadLocalValue(FrameworkElement.DataContextProperty) == DependencyProperty.UnsetValue)
            {
                _menuDataContextBinding = new Binding { Path = new PropertyPath(DataContextProperty), Source = this };
                BindingOperations.SetBinding(menu, FrameworkElement.DataContextProperty, _menuDataContextBinding);
            }
        }

        private void DetachMenu(ContextMenu? menu)
        {
            if (menu == null)
            {
                return;
            }

            menu.Opened -= this.OnMenuOpened;
            menu.Closed -= this.OnMenuClosed;

            if (_menuOpenedByTray && menu.IsOpen)
            {
                menu.IsOpen = false;
            }

            _menuOpenedByTray = false;

            // Only remove the binding if it is still ours (XAML may have replaced it after the menu was attached).
            if (_menuDataContextBinding != null
                && ReferenceEquals(BindingOperations.GetBindingBase(menu, FrameworkElement.DataContextProperty), _menuDataContextBinding))
            {
                BindingOperations.ClearBinding(menu, FrameworkElement.DataContextProperty);
            }

            _menuDataContextBinding = null;
        }

        private void OpenContextMenu(int x, int y, TrayIconInputSource source)
        {
            var menu = this.ContextMenu;

            if (menu == null || _native == null)
            {
                return;
            }

            if (menu.IsOpen)
            {
                menu.IsOpen = false;
            }

            // Keyboard requests anchor on the icon itself rather than wherever the cursor happens to be.
            if (source == TrayIconInputSource.Keyboard && _native.TryGetIconRect(out var iconRect))
            {
                x = iconRect.Left + (iconRect.Width / 2);
                y = iconRect.Top + (iconRect.Height / 2);
            }

            _menuAnchor = new POINT { X = x, Y = y };
            _menuInputSource = source;

            // First estimate the position from the measured size so the menu appears in roughly the right place;
            // OnMenuOpened then corrects it using the real window size.
            double scale = GetDpiForPoint(_menuAnchor) / 96.0;
            menu.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            int width = (int)Math.Ceiling(menu.DesiredSize.Width * scale);
            int height = (int)Math.Ceiling(menu.DesiredSize.Height * scale);

            if (TryGetWorkArea(_menuAnchor, out var workArea))
            {
                var position = ComputeMenuPosition(_menuAnchor, width, height, workArea);
                menu.HorizontalOffset = position.X / scale;
                menu.VerticalOffset = position.Y / scale;
            }
            else
            {
                menu.HorizontalOffset = x / scale;
                menu.VerticalOffset = y / scale;
            }

            menu.Placement = PlacementMode.Absolute;
            _menuOpenedByTray = true;
            menu.IsOpen = true;
        }

        private void OnMenuOpened(object sender, RoutedEventArgs e)
        {
            if (!_menuOpenedByTray || sender is not ContextMenu menu)
            {
                return;
            }

            if (PresentationSource.FromVisual(menu) is HwndSource source && source.CompositionTarget != null)
            {
                IntPtr hwnd = source.Handle;

                if (GetWindowRect(hwnd, out var windowRect) && TryGetWorkArea(_menuAnchor, out var workArea))
                {
                    var position = ComputeMenuPosition(_menuAnchor, windowRect.Width, windowRect.Height, workArea);

                    if (position.X != windowRect.Left || position.Y != windowRect.Top)
                    {
                        var dip = source.CompositionTarget.TransformFromDevice.Transform(new Point(position.X, position.Y));
                        menu.HorizontalOffset = dip.X;
                        menu.VerticalOffset = dip.Y;
                    }
                }

                // The shell grants this process foreground rights while it handles the tray notification. Activating
                // the popup lets WPF see a deactivation (and close the menu) when the user clicks anywhere else, and
                // gives the menu keyboard input for arrows/Escape -- without activating any application window.
                SetForegroundWindow(hwnd);
            }

            menu.Focus();

            if (_menuInputSource == TrayIconInputSource.Keyboard)
            {
                menu.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        }

        private void OnMenuClosed(object sender, RoutedEventArgs e)
        {
            _menuOpenedByTray = false;
        }

        /// <summary>
        /// Places a <paramref name="width"/> × <paramref name="height"/> menu next to <paramref name="anchor"/>, opening
        /// up and to the right where possible, flipping when it would leave the work area, and finally clamping into
        /// it. Because the anchor sits on the taskbar, clamping pushes the menu off whichever edge the taskbar is on.
        /// All values are physical pixels.
        /// </summary>
        internal static POINT ComputeMenuPosition(POINT anchor, int width, int height, RECT workArea)
        {
            int x = anchor.X;
            int y = anchor.Y - height;

            if (x + width > workArea.Right)
            {
                x = anchor.X - width;
            }

            if (y < workArea.Top)
            {
                y = anchor.Y;
            }

            x = Math.Max(workArea.Left, Math.Min(x, workArea.Right - width));
            y = Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - height));

            return new POINT { X = x, Y = y };
        }

        #endregion
    }
}
