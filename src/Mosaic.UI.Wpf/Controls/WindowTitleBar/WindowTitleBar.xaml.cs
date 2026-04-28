/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Data;
using System.Windows.Media;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A self-contained custom title bar control for borderless/chrome-less WPF windows.
    /// Drop it into the first row of your window layout and it automatically wires up
    /// drag-to-move, double-click maximize/restore, and the standard window system buttons.
    /// </summary>
    /// <remarks>
    /// The control locates its parent <see cref="Window"/> on <see cref="FrameworkElement.Loaded"/>
    /// and detaches cleanly on <see cref="FrameworkElement.Unloaded"/>, so no code-behind is required
    /// in the host window.
    /// <para>
    /// If <see cref="TitleText"/> is not set the control automatically mirrors
    /// <see cref="Window.Title"/> through a live binding.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// <!-- Row 0 of your window grid, spanning all columns -->
    /// <mosaic:WindowTitleBar
    ///     Grid.Row="0"
    ///     Grid.ColumnSpan="2"
    ///     IconSource="/Assets/app-48.png"
    ///     TitleText="My Application">
    ///     <mosaic:WindowTitleBar.RightContent>
    ///         <!-- Extra buttons that appear to the left of the system buttons -->
    ///         <Button Click="OnSettingsClick" Content="⚙" />
    ///     </mosaic:WindowTitleBar.RightContent>
    /// </mosaic:WindowTitleBar>
    /// ]]>
    /// </code>
    /// </example>
    public partial class WindowTitleBar : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Gets or sets the title text displayed in the title bar.
        /// When <see langword="null"/> or empty the control automatically mirrors
        /// the parent <see cref="Window.Title"/>.
        /// </summary>
        public static readonly DependencyProperty TitleTextProperty = DependencyProperty.Register(
            nameof(TitleText),
            typeof(string),
            typeof(WindowTitleBar),
            new PropertyMetadata(null, OnTitleTextChanged));

        /// <summary>
        /// Gets or sets the horizontal alignment of the title text.
        /// </summary>
        public static readonly DependencyProperty TitleAlignmentProperty = DependencyProperty.Register(
            nameof(TitleAlignment),
            typeof(HorizontalAlignment),
            typeof(WindowTitleBar),
            new PropertyMetadata(HorizontalAlignment.Left));

        /// <summary>
        /// Gets or sets the image source used for the title bar icon.
        /// </summary>
        public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(
            nameof(IconSource),
            typeof(ImageSource),
            typeof(WindowTitleBar),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a value indicating whether the title bar icon is visible.
        /// </summary>
        public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register(
            nameof(ShowIcon),
            typeof(bool),
            typeof(WindowTitleBar),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the minimize button is visible.
        /// </summary>
        public static readonly DependencyProperty ShowMinimizeButtonProperty = DependencyProperty.Register(
            nameof(ShowMinimizeButton),
            typeof(bool),
            typeof(WindowTitleBar),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the maximize/restore button is visible.
        /// </summary>
        public static readonly DependencyProperty ShowMaxRestoreButtonProperty = DependencyProperty.Register(
            nameof(ShowMaxRestoreButton),
            typeof(bool),
            typeof(WindowTitleBar),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the close button is visible.
        /// </summary>
        public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(
            nameof(ShowCloseButton),
            typeof(bool),
            typeof(WindowTitleBar),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets content injected to the left of the title text (e.g. breadcrumbs or a search box).
        /// </summary>
        public static readonly DependencyProperty LeftContentProperty = DependencyProperty.Register(
            nameof(LeftContent),
            typeof(object),
            typeof(WindowTitleBar),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets content injected to the left of the system buttons (e.g. theme toggle, settings).
        /// </summary>
        public static readonly DependencyProperty RightContentProperty = DependencyProperty.Register(
            nameof(RightContent),
            typeof(object),
            typeof(WindowTitleBar),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the glyph displayed on the maximize/restore button when the window is maximized.
        /// Defaults to the Segoe MDL2 Assets Restore glyph (&#xE923;).
        /// </summary>
        public static readonly DependencyProperty MaximizedGlyphProperty = DependencyProperty.Register(
            nameof(MaximizedGlyph),
            typeof(string),
            typeof(WindowTitleBar),
            new PropertyMetadata("\xE923"));

        /// <summary>
        /// Gets or sets the glyph displayed on the maximize/restore button when the window is in the
        /// normal (restored) state.  Defaults to the Segoe MDL2 Assets Maximize glyph (&#xE922;).
        /// </summary>
        public static readonly DependencyProperty RestoredGlyphProperty = DependencyProperty.Register(
            nameof(RestoredGlyph),
            typeof(string),
            typeof(WindowTitleBar),
            new PropertyMetadata("\xE922"));

        #endregion

        #region Properties

        /// <inheritdoc cref="TitleTextProperty"/>
        public string? TitleText
        {
            get => (string?)GetValue(TitleTextProperty);
            set => SetValue(TitleTextProperty, value);
        }

        /// <inheritdoc cref="TitleAlignmentProperty"/>
        public HorizontalAlignment TitleAlignment
        {
            get => (HorizontalAlignment)GetValue(TitleAlignmentProperty);
            set => SetValue(TitleAlignmentProperty, value);
        }

        /// <inheritdoc cref="IconSourceProperty"/>
        public ImageSource? IconSource
        {
            get => (ImageSource?)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        /// <inheritdoc cref="ShowIconProperty"/>
        public bool ShowIcon
        {
            get => (bool)GetValue(ShowIconProperty);
            set => SetValue(ShowIconProperty, value);
        }

        /// <inheritdoc cref="ShowMinimizeButtonProperty"/>
        public bool ShowMinimizeButton
        {
            get => (bool)GetValue(ShowMinimizeButtonProperty);
            set => SetValue(ShowMinimizeButtonProperty, value);
        }

        /// <inheritdoc cref="ShowMaxRestoreButtonProperty"/>
        public bool ShowMaxRestoreButton
        {
            get => (bool)GetValue(ShowMaxRestoreButtonProperty);
            set => SetValue(ShowMaxRestoreButtonProperty, value);
        }

        /// <inheritdoc cref="ShowCloseButtonProperty"/>
        public bool ShowCloseButton
        {
            get => (bool)GetValue(ShowCloseButtonProperty);
            set => SetValue(ShowCloseButtonProperty, value);
        }

        /// <inheritdoc cref="LeftContentProperty"/>
        public object? LeftContent
        {
            get => GetValue(LeftContentProperty);
            set => SetValue(LeftContentProperty, value);
        }

        /// <inheritdoc cref="RightContentProperty"/>
        public object? RightContent
        {
            get => GetValue(RightContentProperty);
            set => SetValue(RightContentProperty, value);
        }

        /// <inheritdoc cref="MaximizedGlyphProperty"/>
        public string MaximizedGlyph
        {
            get => (string)GetValue(MaximizedGlyphProperty);
            set => SetValue(MaximizedGlyphProperty, value);
        }

        /// <inheritdoc cref="RestoredGlyphProperty"/>
        public string RestoredGlyph
        {
            get => (string)GetValue(RestoredGlyphProperty);
            set => SetValue(RestoredGlyphProperty, value);
        }

        #endregion

        private Window? _parentWindow;

        public WindowTitleBar()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region Lifecycle

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);

            if (_parentWindow == null)
            {
                return;
            }

            _parentWindow.StateChanged += OnParentWindowStateChanged;

            ApplyTitleBinding();
            UpdateMaxRestoreGlyph();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.StateChanged -= OnParentWindowStateChanged;
                _parentWindow = null;
            }
        }

        #endregion

        #region Title

        private static void OnTitleTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var titleBar = (WindowTitleBar)d;
            titleBar.ApplyTitleBinding();
        }

        /// <summary>
        /// Sets the title TextBlock's Text binding.  When <see cref="TitleText"/> has a value it is
        /// used directly; otherwise a live binding to <see cref="Window.Title"/> is established so that
        /// any runtime title changes are reflected automatically.
        /// </summary>
        private void ApplyTitleBinding()
        {
            if (!string.IsNullOrEmpty(TitleText))
            {
                BindingOperations.ClearBinding(TitleTextBlock, TextBlock.TextProperty);
                TitleTextBlock.Text = TitleText;
            }
            else if (_parentWindow != null)
            {
                var binding = new Binding(nameof(Window.Title)) { Source = _parentWindow };
                TitleTextBlock.SetBinding(TextBlock.TextProperty, binding);
            }
        }

        #endregion

        #region Window State

        private void OnParentWindowStateChanged(object? sender, EventArgs e)
        {
            UpdateMaxRestoreGlyph();
        }

        private void UpdateMaxRestoreGlyph()
        {
            if (_parentWindow == null)
            {
                return;
            }

            MaxRestoreButton.Content = _parentWindow.WindowState == WindowState.Maximized
                ? MaximizedGlyph
                : RestoredGlyph;
        }

        private void ToggleMaximize()
        {
            if (_parentWindow == null)
            {
                return;
            }

            _parentWindow.WindowState = _parentWindow.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        #endregion

        #region Event Handlers

        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                _parentWindow?.DragMove();
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
            {
                _parentWindow.WindowState = WindowState.Minimized;
            }
        }

        private void OnMaxRestoreClick(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            _parentWindow?.Close();
        }

        #endregion
    }
}
