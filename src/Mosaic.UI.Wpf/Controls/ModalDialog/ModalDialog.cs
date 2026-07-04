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
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A lookless modal dialog that displays its content centered in the adorner layer of a host
    /// element.  While the dialog is open the host element (and everything beneath it) is blurred
    /// and dimmed; the dialog itself renders in the adorner layer and stays sharp.  Await
    /// <see cref="ShowAsync"/> to get the result passed to <see cref="Close"/>.
    /// </summary>
    [TemplatePart(Name = PartCloseButton, Type = typeof(ButtonBase))]
    [DefaultProperty(nameof(Content))]
    public class ModalDialog : ContentControl
    {
        private const string PartCloseButton = "PART_CloseButton";

        private ButtonBase? _closeButton;
        private ModalDialogAdorner? _adorner;
        private AdornerLayer? _adornerLayer;
        private UIElement? _host;
        private Effect? _previousHostEffect;
        private BlurEffect? _blurEffect;
        private IInputElement? _previouslyFocused;
        private TaskCompletionSource<bool?>? _completion;

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(ModalDialog), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the title displayed in the dialog header.
        /// </summary>
        [Category("Common")]
        [Description("The title displayed in the dialog header.")]
        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(ModalDialog), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the optional secondary text displayed under the title.
        /// </summary>
        [Category("Common")]
        [Description("Optional secondary text displayed under the title.")]
        public string Description
        {
            get => (string)this.GetValue(DescriptionProperty);
            set => this.SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(ModalDialog), new FrameworkPropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Gets or sets the radius used to round the dialog corners.
        /// </summary>
        [Category("Appearance")]
        [Description("The radius used to round the dialog corners.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)this.GetValue(CornerRadiusProperty);
            set => this.SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowCloseButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(
            nameof(ShowCloseButton), typeof(bool), typeof(ModalDialog), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the header close (X) button is shown.  Clicking it closes the
        /// dialog with a null result.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the header close (X) button is shown.")]
        public bool ShowCloseButton
        {
            get => (bool)this.GetValue(ShowCloseButtonProperty);
            set => this.SetValue(ShowCloseButtonProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseOnEscape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseOnEscapeProperty = DependencyProperty.Register(
            nameof(CloseOnEscape), typeof(bool), typeof(ModalDialog), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether pressing Escape closes the dialog with a null result.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether pressing Escape closes the dialog with a null result.")]
        public bool CloseOnEscape
        {
            get => (bool)this.GetValue(CloseOnEscapeProperty);
            set => this.SetValue(CloseOnEscapeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CloseOnBackdropClick"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseOnBackdropClickProperty = DependencyProperty.Register(
            nameof(CloseOnBackdropClick), typeof(bool), typeof(ModalDialog), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether clicking the dimmed backdrop closes the dialog with a null result.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether clicking the dimmed backdrop closes the dialog with a null result.")]
        public bool CloseOnBackdropClick
        {
            get => (bool)this.GetValue(CloseOnBackdropClickProperty);
            set => this.SetValue(CloseOnBackdropClickProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BackdropBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackdropBrushProperty = DependencyProperty.Register(
            nameof(BackdropBrush), typeof(Brush), typeof(ModalDialog),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00))));

        /// <summary>
        /// Gets or sets the brush painted over the blurred host while the dialog is open.
        /// </summary>
        [Category("Appearance")]
        [Description("The brush painted over the blurred host while the dialog is open.")]
        public Brush BackdropBrush
        {
            get => (Brush)this.GetValue(BackdropBrushProperty);
            set => this.SetValue(BackdropBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BlurRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(
            nameof(BlurRadius), typeof(double), typeof(ModalDialog), new FrameworkPropertyMetadata(12.0));

        /// <summary>
        /// Gets or sets the blur radius applied to the host element while the dialog is open.
        /// Set to zero to disable the blur and keep only the dimmed backdrop.
        /// </summary>
        [Category("Appearance")]
        [Description("The blur radius applied to the host element while the dialog is open.")]
        public double BlurRadius
        {
            get => (double)this.GetValue(BlurRadiusProperty);
            set => this.SetValue(BlurRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Opened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(Opened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModalDialog));

        /// <summary>
        /// Raised after the dialog has been added to the adorner layer and displayed.
        /// </summary>
        public event RoutedEventHandler Opened
        {
            add => this.AddHandler(OpenedEvent, value);
            remove => this.RemoveHandler(OpenedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="Closed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
            nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ModalDialog));

        /// <summary>
        /// Raised after the dialog has been dismissed and removed from the adorner layer.
        /// </summary>
        public event RoutedEventHandler Closed
        {
            add => this.AddHandler(ClosedEvent, value);
            remove => this.RemoveHandler(ClosedEvent, value);
        }

        /// <summary>
        /// Gets whether the dialog is currently displayed.
        /// </summary>
        [Browsable(false)]
        public bool IsOpen => _adorner != null;

        static ModalDialog()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ModalDialog), new FrameworkPropertyMetadata(typeof(ModalDialog)));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_closeButton != null)
            {
                _closeButton.Click -= this.CloseButton_Click;
            }

            _closeButton = this.GetTemplateChild(PartCloseButton) as ButtonBase;

            if (_closeButton != null)
            {
                _closeButton.Click += this.CloseButton_Click;
            }
        }

        /// <summary>
        /// Walks up the visual tree from an element inside the dialog content and returns the
        /// containing <see cref="ModalDialog"/>, or null when the element is not hosted in one.
        /// Useful for buttons inside dialog content that need to call <see cref="Close"/>.
        /// </summary>
        /// <param name="element">An element inside the dialog's content.</param>
        public static ModalDialog? FindHost(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is ModalDialog dialog)
                {
                    return dialog;
                }

                element = element is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(element)
                    : LogicalTreeHelper.GetParent(element);
            }

            return null;
        }

        /// <summary>
        /// Shows the dialog modally over the provided host element and returns a task that
        /// completes with the value passed to <see cref="Close"/> when the dialog is dismissed.
        /// The host element (typically the window's root content) is blurred and dimmed while
        /// the dialog is open; the dialog itself renders sharp in the adorner layer above it.
        /// </summary>
        /// <param name="host">
        /// The element whose adorner layer hosts the dialog and which receives the blur.  The
        /// element must be loaded and located beneath an <see cref="AdornerDecorator"/> (window
        /// content is by default).
        /// </param>
        /// <exception cref="InvalidOperationException">The dialog is already open or no adorner layer was found.</exception>
        public Task<bool?> ShowAsync(UIElement host)
        {
            ArgumentNullException.ThrowIfNull(host);

            if (this.IsOpen)
            {
                throw new InvalidOperationException("The dialog is already open.");
            }

            var layer = AdornerLayer.GetAdornerLayer(host)
                        ?? throw new InvalidOperationException("No adorner layer was found above the host element.  Ensure the element is inside an AdornerDecorator (window content is by default) and has been loaded.");

            _host = host;
            _adornerLayer = layer;
            _completion = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _previouslyFocused = Keyboard.FocusedElement;

            // Blur what's beneath: the effect goes on the adorned element so the adorner layer
            // (a sibling visual inside the AdornerDecorator) stays sharp.
            _previousHostEffect = host.Effect;

            if (this.BlurRadius > 0)
            {
                _blurEffect = new BlurEffect { Radius = 0 };
                host.Effect = _blurEffect;
                _blurEffect.BeginAnimation(BlurEffect.RadiusProperty,
                    new DoubleAnimation(0, this.BlurRadius, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }

            _adorner = new ModalDialogAdorner(host, this);
            layer.Add(_adorner);

            this.AnimateEntrance();

            // Move keyboard focus into the dialog once it has rendered.
            this.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (this.IsOpen)
                {
                    this.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                }
            });

            this.RaiseEvent(new RoutedEventArgs(OpenedEvent, this));

            return _completion.Task;
        }

        /// <summary>
        /// Closes the dialog, un-blurs the host and completes the task returned by
        /// <see cref="ShowAsync"/> with the provided result.  Safe to call when already closed.
        /// </summary>
        /// <param name="result">The dialog result: true for confirm, false/null for cancel.</param>
        public void Close(bool? result = null)
        {
            if (_adorner == null || _host == null)
            {
                return;
            }

            var adorner = _adorner;
            var layer = _adornerLayer;
            var host = _host;
            var previousEffect = _previousHostEffect;
            var blur = _blurEffect;
            var completion = _completion;
            var previouslyFocused = _previouslyFocused;

            _adorner = null;
            _adornerLayer = null;
            _host = null;
            _previousHostEffect = null;
            _blurEffect = null;
            _completion = null;
            _previouslyFocused = null;

            if (blur != null)
            {
                var restore = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
                restore.Completed += (_, _) =>
                {
                    blur.BeginAnimation(BlurEffect.RadiusProperty, null);

                    if (ReferenceEquals(host.Effect, blur))
                    {
                        host.Effect = previousEffect;
                    }
                };
                blur.BeginAnimation(BlurEffect.RadiusProperty, restore);
            }

            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
            fade.Completed += (_, _) =>
            {
                adorner.Detach();
                layer?.Remove(adorner);
            };
            adorner.BeginAnimation(OpacityProperty, fade);

            this.RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
            completion?.TrySetResult(result);

            try
            {
                previouslyFocused?.Focus();
            }
            catch
            {
                // Restoring focus is best effort; the previous element may be gone.
            }
        }

        /// <summary>
        /// Occurs when the template close button is clicked.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event data.</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(null);
        }

        /// <summary>
        /// Runs the scale + fade entrance animation on the dialog card.
        /// </summary>
        private void AnimateEntrance()
        {
            var scale = new ScaleTransform(0.94, 0.94);
            this.RenderTransform = scale;
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            this.Opacity = 0;

            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.94, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.94, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
        }

        /// <summary>
        /// The adorner that hosts the backdrop and the dialog card above the blurred host.
        /// </summary>
        private sealed class ModalDialogAdorner : Adorner
        {
            private readonly Grid _root;
            private readonly ModalDialog _dialog;

            public ModalDialogAdorner(UIElement adornedElement, ModalDialog dialog) : base(adornedElement)
            {
                _dialog = dialog;

                var backdrop = new Border
                {
                    Background = dialog.BackdropBrush,
                    IsHitTestVisible = true
                };

                backdrop.MouseLeftButtonDown += (_, e) =>
                {
                    if (_dialog.CloseOnBackdropClick)
                    {
                        e.Handled = true;
                        _dialog.Close(null);
                    }
                };

                _root = new Grid();
                _root.Children.Add(backdrop);
                _root.Children.Add(dialog);

                dialog.HorizontalAlignment = HorizontalAlignment.Center;
                dialog.VerticalAlignment = VerticalAlignment.Center;

                // Keep tab and arrow-key navigation trapped inside the dialog while it's open.
                KeyboardNavigation.SetTabNavigation(_root, KeyboardNavigationMode.Cycle);
                KeyboardNavigation.SetControlTabNavigation(_root, KeyboardNavigationMode.Cycle);
                KeyboardNavigation.SetDirectionalNavigation(_root, KeyboardNavigationMode.Cycle);

                this.AddVisualChild(_root);
                this.AddLogicalChild(_root);

                backdrop.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            }

            /// <summary>
            /// Removes the dialog from the adorner's tree so it can be shown again later.
            /// </summary>
            public void Detach()
            {
                _root.Children.Remove(_dialog);
            }

            /// <inheritdoc />
            protected override int VisualChildrenCount => 1;

            /// <inheritdoc />
            protected override Visual GetVisualChild(int index) => _root;

            /// <inheritdoc />
            protected override Size MeasureOverride(Size constraint)
            {
                var size = this.AdornedElement.RenderSize;
                _root.Measure(size);
                return size;
            }

            /// <inheritdoc />
            protected override Size ArrangeOverride(Size finalSize)
            {
                _root.Arrange(new Rect(this.AdornedElement.RenderSize));
                return finalSize;
            }

            /// <inheritdoc />
            protected override void OnPreviewKeyDown(KeyEventArgs e)
            {
                if (e.Key == Key.Escape && _dialog.CloseOnEscape)
                {
                    e.Handled = true;
                    _dialog.Close(null);
                    return;
                }

                base.OnPreviewKeyDown(e);
            }
        }
    }
}
