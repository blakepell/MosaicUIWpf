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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Mosaic.UI.Wpf.Themes;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A clickable card that represents a single file on disk, showing the operating system's icon for the
    /// file type alongside the file name and its formatted size.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the file name is displayed; the full path is surfaced as the tooltip. When the file is missing an
    /// error glyph replaces the shell icon and the size is omitted, but the intended file name is still shown.
    /// </para>
    /// <para>
    /// With <see cref="IsTintEnabled"/> set the card background is washed with a small amount of the icon's
    /// dominant hue. The wash is always mixed into the active theme's control background color, so the card
    /// stays inside the Light, Dark, or Blue palette rather than painting an arbitrary color.
    /// </para>
    /// </remarks>
    [DefaultProperty(nameof(FilePath))]
    [DefaultEvent(nameof(Click))]
    [TemplatePart(Name = PartCard, Type = typeof(Border))]
    public class FileCard : Control
    {
        /// <summary>
        /// The border that is raised and lowered as the pointer interacts with the card.
        /// </summary>
        private const string PartCard = "PART_Card";

        /// <summary>
        /// How much of the dominant color is mixed into the theme's control background color.
        /// </summary>
        private const double TintStrength = 0.16;

        /// <summary>
        /// The packaged asset shown when <see cref="FilePath"/> does not exist on disk.
        /// </summary>
        private const string ErrorIconUri = "pack://application:,,,/Mosaic.UI.Wpf;component/Assets/Images/error-48.png";

        /// <summary>
        /// The error glyph, loaded once and frozen for reuse across every card instance.
        /// </summary>
        private static ImageSource? _errorIcon;

        /// <summary>
        /// Tracks whether this instance is currently subscribed to <see cref="ThemeManager.ThemeChanged"/>.
        /// </summary>
        private bool _subscribedToThemeChanged;

        /// <summary>
        /// The transform applied to the card border that produces the raise and lower.
        /// </summary>
        private TranslateTransform? _cardTransform;

        /// <summary>
        /// The shadow under the card border, deepened as the card rises and flattened as it is pressed.
        /// </summary>
        private DropShadowEffect? _cardShadow;

        #region FilePath

        /// <summary>
        /// Identifies the <see cref="FilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            nameof(FilePath),
            typeof(string),
            typeof(FileCard),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnFilePathChanged));

        /// <summary>
        /// Gets or sets the full path of the file the card represents. Only the file name portion is rendered;
        /// the full path is shown as the tooltip on the file name.
        /// </summary>
        [Category("Common")]
        [Description("The full path of the file the card represents.")]
        public string? FilePath
        {
            get => (string?)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        /// <summary>
        /// Re-reads the file from disk whenever <see cref="FilePath"/> changes.
        /// </summary>
        private static void OnFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FileCard)d).Refresh();
        }

        #endregion

        #region IsTintEnabled

        /// <summary>
        /// Identifies the <see cref="IsTintEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTintEnabledProperty = DependencyProperty.Register(
            nameof(IsTintEnabled),
            typeof(bool),
            typeof(FileCard),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnIsTintEnabledChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the card background is tinted with the dominant color of the
        /// file's icon. When <c>false</c> the card uses the theme's control background color unaltered.
        /// </summary>
        [Category("Appearance")]
        [Description("Tints the card background with a small amount of the file icon's dominant color.")]
        public bool IsTintEnabled
        {
            get => (bool)GetValue(IsTintEnabledProperty);
            set => SetValue(IsTintEnabledProperty, value);
        }

        /// <summary>
        /// Recomputes the card background when the tint is switched on or off.
        /// </summary>
        private static void OnIsTintEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FileCard)d).UpdateCardBackground();
        }

        #endregion

        #region CornerRadius

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(FileCard),
            new FrameworkPropertyMetadata(new CornerRadius(6), FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Gets or sets the corner radius of the card.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius of the card.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region IconSize

        /// <summary>
        /// Identifies the <see cref="IconSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(FileCard),
            new FrameworkPropertyMetadata(32d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Gets or sets the rendered width and height of the file icon.
        /// </summary>
        [Category("Appearance")]
        [Description("The rendered width and height of the file icon.")]
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        #endregion

        #region Command

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(FileCard), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command invoked when the card is clicked. When <see cref="CommandParameter"/> has not
        /// been set the command receives the card's <see cref="FilePath"/>.
        /// </summary>
        [Category("Action")]
        [Description("The command invoked when the card is clicked.")]
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(FileCard), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="Command"/>. Leave this unset to have the card pass its
        /// <see cref="FilePath"/> instead.
        /// </summary>
        [Category("Action")]
        [Description("The parameter passed to Command. Defaults to the FilePath when unset.")]
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        #endregion

        #region Read-only state

        /// <summary>
        /// Identifies the <see cref="FileName"/> read-only dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey FileNamePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FileName), typeof(string), typeof(FileCard), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="FileName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileNameProperty = FileNamePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the file name portion of <see cref="FilePath"/>, which is what the card displays.
        /// </summary>
        public string FileName => (string)GetValue(FileNameProperty);

        /// <summary>
        /// Identifies the <see cref="FileSizeText"/> read-only dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey FileSizeTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FileSizeText), typeof(string), typeof(FileCard), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="FileSizeText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileSizeTextProperty = FileSizeTextPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the formatted file size (for example <c>2.1 MB</c>), or an empty string when the file does not exist.
        /// </summary>
        public string FileSizeText => (string)GetValue(FileSizeTextProperty);

        /// <summary>
        /// Identifies the <see cref="Icon"/> read-only dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey IconPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Icon), typeof(ImageSource), typeof(FileCard), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty = IconPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the shell icon for the file type, or the error glyph when the file is missing.
        /// </summary>
        public ImageSource? Icon => (ImageSource?)GetValue(IconProperty);

        /// <summary>
        /// Identifies the <see cref="FileExists"/> read-only dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey FileExistsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FileExists), typeof(bool), typeof(FileCard), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="FileExists"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileExistsProperty = FileExistsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether <see cref="FilePath"/> was found on disk the last time it was read.
        /// </summary>
        public bool FileExists => (bool)GetValue(FileExistsProperty);

        /// <summary>
        /// Identifies the <see cref="CardBackground"/> read-only dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey CardBackgroundPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(CardBackground), typeof(Brush), typeof(FileCard), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CardBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CardBackgroundProperty = CardBackgroundPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the brush the card paints itself with: the theme's control background color, optionally tinted
        /// with the icon's dominant hue.
        /// </summary>
        public Brush? CardBackground => (Brush?)GetValue(CardBackgroundProperty);

        /// <summary>
        /// Identifies the <see cref="IsPressed"/> read-only dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey IsPressedPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsPressed), typeof(bool), typeof(FileCard), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsPressed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty = IsPressedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether the card is currently being pressed. The default template uses this to
        /// lower the card.
        /// </summary>
        public bool IsPressed => (bool)GetValue(IsPressedProperty);

        #endregion

        #region Click

        /// <summary>
        /// Identifies the <see cref="Click"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FileCard));

        /// <summary>
        /// Occurs when the card is clicked with the mouse or activated from the keyboard.
        /// </summary>
        [Category("Action")]
        [Description("Occurs when the card is clicked or activated from the keyboard.")]
        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        #endregion

        /// <summary>
        /// Initializes the <see cref="FileCard"/> class and overrides the default style key metadata.
        /// </summary>
        static FileCard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FileCard), new FrameworkPropertyMetadata(typeof(FileCard)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCard"/> class.
        /// </summary>
        public FileCard()
        {
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
            this.IsEnabledChanged += this.OnIsEnabledChanged;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // The transform and shadow are created here rather than declared in the template so they are
            // guaranteed to be per-instance and unfrozen, which is what makes them animatable.
            _cardTransform = null;
            _cardShadow = null;

            if (this.GetTemplateChild(PartCard) is Border card)
            {
                _cardTransform = new TranslateTransform();
                _cardShadow = new DropShadowEffect
                {
                    Direction = 270,
                    Color = Colors.Black
                };

                card.RenderTransform = _cardTransform;
                card.Effect = _cardShadow;
            }

            this.UpdateElevation(animate: false);
        }

        /// <summary>
        /// Drops a disabled card back to its resting elevation so it cannot be left raised.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data carrying the new enabled state.</param>
        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!this.IsEnabled)
            {
                SetValue(IsPressedPropertyKey, false);
            }

            this.UpdateElevation(animate: false);
        }

        /// <summary>
        /// Moves the card to the elevation its current state calls for: raised while the pointer is over it,
        /// settled down while it is pressed, and flat otherwise.
        /// </summary>
        /// <param name="animate">
        /// When <c>false</c> the card snaps to the target elevation, which is what a freshly applied template
        /// or a disabled card wants.
        /// </param>
        private void UpdateElevation(bool animate)
        {
            if (_cardTransform is null || _cardShadow is null)
            {
                return;
            }

            double offset;
            double shadowDepth;
            double blurRadius;
            double shadowOpacity;
            var duration = TimeSpan.FromSeconds(0.12);

            if (this.IsEnabled && this.IsPressed)
            {
                // Pressed wins over hover, and settles faster than the card rises.
                offset = 1;
                shadowDepth = 0;
                blurRadius = 3;
                shadowOpacity = 0.12;
                duration = TimeSpan.FromSeconds(0.08);
            }
            else if (this.IsEnabled && this.IsMouseOver)
            {
                offset = -1.5;
                shadowDepth = 5;
                blurRadius = 14;
                shadowOpacity = 0.32;
            }
            else
            {
                offset = 0;
                shadowDepth = 1;
                blurRadius = 6;
                shadowOpacity = 0.16;
            }

            Animate(_cardTransform, TranslateTransform.YProperty, offset, animate, duration);
            Animate(_cardShadow, DropShadowEffect.ShadowDepthProperty, shadowDepth, animate, duration);
            Animate(_cardShadow, DropShadowEffect.BlurRadiusProperty, blurRadius, animate, duration);
            Animate(_cardShadow, DropShadowEffect.OpacityProperty, shadowOpacity, animate, duration);
        }

        /// <summary>
        /// Animates (or immediately sets) a double property on one of the card's render objects.
        /// </summary>
        /// <param name="target">The animatable object to change.</param>
        /// <param name="property">The property being changed.</param>
        /// <param name="to">The value to land on.</param>
        /// <param name="animate">When <c>false</c> the value is applied without an animation.</param>
        /// <param name="duration">How long the animation runs.</param>
        private static void Animate(Animatable target, DependencyProperty property, double to, bool animate, TimeSpan duration)
        {
            if (!animate)
            {
                // Clearing the clock first is required, otherwise a held animation keeps overriding the value.
                target.BeginAnimation(property, null);
                target.SetValue(property, to);
                return;
            }

            var animation = new DoubleAnimation(to, new Duration(duration))
            {
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            target.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Subscribes to theme changes and refreshes once the control is in a visual tree, at which point the
        /// theme resources the tint is derived from can be resolved.
        /// </summary>
        /// <param name="sender">The source of the loaded event.</param>
        /// <param name="e">The event data for the load operation.</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_subscribedToThemeChanged)
            {
                ThemeManager.ThemeChanged += this.OnThemeChanged;
                _subscribedToThemeChanged = true;
            }

            this.Refresh();
        }

        /// <summary>
        /// Unsubscribes from theme changes so the card does not outlive its visual tree.
        /// </summary>
        /// <param name="sender">The source of the unloaded event.</param>
        /// <param name="e">The event data for the unload operation.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_subscribedToThemeChanged)
            {
                ThemeManager.ThemeChanged -= this.OnThemeChanged;
                _subscribedToThemeChanged = false;
            }
        }

        /// <summary>
        /// Recomputes the tinted background against the newly applied theme.
        /// </summary>
        /// <param name="sender">The source of the theme notification.</param>
        /// <param name="e">The theme that is now active.</param>
        private void OnThemeChanged(object? sender, MosaicThemeMode e)
        {
            this.UpdateCardBackground();
        }

        /// <summary>
        /// Re-reads <see cref="FilePath"/> from disk and updates the displayed name, size, icon, and tint.
        /// </summary>
        /// <remarks>
        /// Call this after the underlying file has changed on disk; the card does not watch the file system.
        /// </remarks>
        public void Refresh()
        {
            string? path = this.FilePath;

            if (string.IsNullOrWhiteSpace(path))
            {
                SetValue(FileNamePropertyKey, string.Empty);
                SetValue(FileSizeTextPropertyKey, string.Empty);
                SetValue(FileExistsPropertyKey, false);
                SetValue(IconPropertyKey, GetErrorIcon());
                this.UpdateCardBackground();
                return;
            }

            // Path.GetFileName throws on characters that are illegal in a path, and a card should render
            // whatever it was handed rather than bring down the app.
            string name;

            try
            {
                name = Path.GetFileName(path);
            }
            catch (ArgumentException)
            {
                name = path;
            }

            SetValue(FileNamePropertyKey, string.IsNullOrEmpty(name) ? path : name);

            bool exists = false;
            long length = 0;

            try
            {
                var info = new FileInfo(path);
                exists = info.Exists;

                if (exists)
                {
                    length = info.Length;
                }
            }
            catch (Exception)
            {
                // An unreadable or malformed path is treated exactly like a missing file.
                exists = false;
            }

            SetValue(FileExistsPropertyKey, exists);

            if (exists)
            {
                SetValue(FileSizeTextPropertyKey, FileItem.FormatSize(length));
                SetValue(IconPropertyKey, FileIconHelper.GetIcon(path, large: true) ?? GetErrorIcon());
            }
            else
            {
                // The intended name still shows, but there is no size to report for a file that is not there.
                SetValue(FileSizeTextPropertyKey, string.Empty);
                SetValue(IconPropertyKey, GetErrorIcon());
            }

            this.UpdateCardBackground();
        }

        /// <summary>
        /// Recomputes <see cref="CardBackground"/> from the active theme's control background color and, when
        /// tinting is enabled, the dominant color of the current icon.
        /// </summary>
        private void UpdateCardBackground()
        {
            var baseColor = this.GetThemeBackgroundColor();

            if (!this.IsTintEnabled)
            {
                SetValue(CardBackgroundPropertyKey, CreateFrozenBrush(baseColor));
                return;
            }

            // Shell icons are per file type, so keying the dominant color cache by extension means a folder full
            // of the same type only pays for the pixel walk once.
            string cacheKey = this.FileExists
                ? $"ext:{Path.GetExtension(this.FilePath ?? string.Empty)}"
                : "mosaic:error-48";

            var dominant = DominantColorHelper.GetDominantColor(cacheKey, this.Icon);

            if (dominant is null)
            {
                SetValue(CardBackgroundPropertyKey, CreateFrozenBrush(baseColor));
                return;
            }

            SetValue(CardBackgroundPropertyKey, CreateFrozenBrush(DominantColorHelper.Tint(baseColor, dominant.Value, TintStrength)));
        }

        /// <summary>
        /// Resolves the active theme's control background color, falling back to a neutral surface when the card
        /// is not yet attached to a tree that carries the Mosaic theme resources.
        /// </summary>
        private Color GetThemeBackgroundColor()
        {
            if (this.TryFindResource(MosaicTheme.ControlBackgroundBrush) is SolidColorBrush brush)
            {
                return brush.Color;
            }

            if (this.TryFindResource(MosaicTheme.ControlBackgroundColor) is Color color)
            {
                return color;
            }

            return Color.FromRgb(0xF3, 0xF3, 0xF3);
        }

        /// <summary>
        /// Creates a frozen <see cref="SolidColorBrush"/> for the supplied color.
        /// </summary>
        /// <param name="color">The color the brush paints.</param>
        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Loads (once) and returns the error glyph shown for files that are not on disk.
        /// </summary>
        private static ImageSource? GetErrorIcon()
        {
            if (_errorIcon is not null)
            {
                return _errorIcon;
            }

            try
            {
                var image = new BitmapImage(new Uri(ErrorIconUri, UriKind.Absolute));
                image.Freeze();
                _errorIcon = image;
            }
            catch
            {
                _errorIcon = null;
            }

            return _errorIcon;
        }

        /// <summary>
        /// Sets the read-only <see cref="IsPressed"/> state and moves the card to the matching elevation.
        /// </summary>
        /// <param name="pressed">Whether the card is now pressed.</param>
        private void SetIsPressed(bool pressed)
        {
            if (this.IsPressed == pressed)
            {
                return;
            }

            SetValue(IsPressedPropertyKey, pressed);
            this.UpdateElevation(animate: true);
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            this.UpdateElevation(animate: true);
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            // The pointer has left the card's surface, so it drops back down even if a press is still
            // being tracked through the mouse capture.
            this.UpdateElevation(animate: true);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (!this.IsEnabled)
            {
                return;
            }

            this.Focus();

            if (this.CaptureMouse())
            {
                this.SetIsPressed(true);
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            bool wasPressed = this.IsPressed;

            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
            }

            this.SetIsPressed(false);

            // Only a press and release that both land on the card counts as a click, matching button behavior.
            if (wasPressed && this.IsMouseOver)
            {
                this.RaiseClick();
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            this.SetIsPressed(false);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || (e.Key != Key.Space && e.Key != Key.Enter))
            {
                return;
            }

            this.SetIsPressed(true);
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Handled || (e.Key != Key.Space && e.Key != Key.Enter))
            {
                return;
            }

            if (this.IsPressed)
            {
                this.SetIsPressed(false);
                this.RaiseClick();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Raises the <see cref="Click"/> routed event and executes <see cref="Command"/> with the card's file path
        /// (or <see cref="CommandParameter"/> when one has been supplied).
        /// </summary>
        public void RaiseClick()
        {
            this.RaiseEvent(new RoutedEventArgs(ClickEvent, this));

            var parameter = this.CommandParameter ?? this.FilePath;
            var command = this.Command;

            if (command is not null && command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new FileCardAutomationPeer(this);
        }
    }
}
