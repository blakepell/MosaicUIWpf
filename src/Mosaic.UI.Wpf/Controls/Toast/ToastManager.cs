/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Documents;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Displays <see cref="ToastMessage"/> notifications in an adorner that overlays a host
    /// element.  Toasts stack within their <see cref="ToastQuadrant"/> (newest closest to the
    /// corner) and remaining toasts shift to fill the gap when one closes.
    /// </summary>
    public class ToastManager
    {
        private readonly UIElement _adornedElement;
        private readonly Dictionary<ToastQuadrant, StackPanel> _panels = new();
        private ToastHostAdorner? _adorner;
        private int _activeCount;

        /// <summary>
        /// The default application wide instance, available after <see cref="Initialize"/> is called.
        /// </summary>
        public static ToastManager? Default { get; private set; }

        /// <summary>
        /// Creates the <see cref="Default"/> instance over the provided element.  Typically called
        /// once with the main window's root content element.
        /// </summary>
        /// <param name="adornedElement">The element whose adorner layer hosts the toasts.</param>
        public static ToastManager Initialize(UIElement adornedElement)
        {
            Default = new ToastManager(adornedElement);
            return Default;
        }

        /// <summary>
        /// Displays <see cref="ToastMessage"/> notifications over the provided element.
        /// </summary>
        /// <param name="adornedElement">The element whose adorner layer hosts the toasts.</param>
        public ToastManager(UIElement adornedElement)
        {
            _adornedElement = adornedElement ?? throw new ArgumentNullException(nameof(adornedElement));
        }

        /// <summary>
        /// The number of toasts currently being displayed.
        /// </summary>
        public int ActiveCount => _activeCount;

        /// <summary>
        /// Raised when a toast is shown.
        /// </summary>
        public event EventHandler? ToastShown;

        /// <summary>
        /// Raised when the last open toast has been dismissed.
        /// </summary>
        public event EventHandler? AllDismissed;

        /// <summary>
        /// Shows a toast notification.
        /// </summary>
        /// <param name="title">The bolded title line.</param>
        /// <param name="message">The message body.</param>
        /// <param name="severity">The severity which determines the color scheme and icon.</param>
        /// <param name="duration">
        /// How long the toast stays open.  When null the toast stays open until the user closes
        /// it via its close button.
        /// </param>
        /// <param name="quadrant">The quadrant of the host surface to display the toast in.</param>
        /// <returns>The toast, whose <see cref="ToastMessage.Dismissed"/> event the caller can handle.</returns>
        public ToastMessage Show(string title, string message, ToastSeverity severity = ToastSeverity.Info,
            TimeSpan? duration = null, ToastQuadrant quadrant = ToastQuadrant.BottomRight)
        {
            this.EnsureAdorner();

            var toast = new ToastMessage(title, message, severity, duration);
            toast.Dismissed += this.OnToastDismissed;

            // Newest toast sits closest to the corner: first child for top quadrants
            // (stacking downward), last child for bottom quadrants (stacking upward).
            var panel = this.GetPanel(quadrant);

            if (quadrant is ToastQuadrant.BottomLeft or ToastQuadrant.BottomRight)
            {
                panel.Children.Add(toast);
            }
            else
            {
                panel.Children.Insert(0, toast);
            }

            _activeCount++;
            toast.BeginDisplay();
            this.ToastShown?.Invoke(this, EventArgs.Empty);

            return toast;
        }

        /// <summary>
        /// Dismisses every open toast.
        /// </summary>
        public void DismissAll()
        {
            foreach (var toast in _panels.Values.SelectMany(p => p.Children.OfType<ToastMessage>()).ToList())
            {
                toast.Dismiss();
            }
        }

        /// <summary>
        /// Occurs when a toast has finished dismissing and should be removed from its stack.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The <see cref="ToastDismissedEventArgs"/> that contains the event data.</param>
        private void OnToastDismissed(object? sender, ToastDismissedEventArgs e)
        {
            if (sender is not ToastMessage toast)
            {
                return;
            }

            toast.Dismissed -= this.OnToastDismissed;

            if (toast.Parent is StackPanel panel)
            {
                panel.Children.Remove(toast);
            }

            _activeCount--;

            if (_activeCount <= 0)
            {
                _activeCount = 0;
                this.AllDismissed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Creates the hosting adorner on first use.
        /// </summary>
        private void EnsureAdorner()
        {
            if (_adorner != null)
            {
                return;
            }

            var layer = AdornerLayer.GetAdornerLayer(_adornedElement)
                        ?? throw new InvalidOperationException("No adorner layer was found above the adorned element.  Ensure the element is inside an AdornerDecorator (window content is by default) and has been loaded.");

            _adorner = new ToastHostAdorner(_adornedElement);
            layer.Add(_adorner);
        }

        /// <summary>
        /// Gets (or creates) the stacking panel for a quadrant.
        /// </summary>
        /// <param name="quadrant">The quadrant of the host surface.</param>
        private StackPanel GetPanel(ToastQuadrant quadrant)
        {
            if (_panels.TryGetValue(quadrant, out var existing))
            {
                return existing;
            }

            var panel = new StackPanel
            {
                Margin = new Thickness(16),
                HorizontalAlignment = quadrant is ToastQuadrant.TopLeft or ToastQuadrant.BottomLeft
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Right,
                VerticalAlignment = quadrant is ToastQuadrant.TopLeft or ToastQuadrant.TopRight
                    ? VerticalAlignment.Top
                    : VerticalAlignment.Bottom
            };

            _panels[quadrant] = panel;
            _adorner!.Root.Children.Add(panel);

            return panel;
        }

        /// <summary>
        /// The adorner that overlays the host element and provides the surface the toast
        /// stacks render on.  Empty space passes hit testing through to the UI beneath.
        /// </summary>
        private sealed class ToastHostAdorner : Adorner
        {
            private readonly Grid _root;

            public ToastHostAdorner(UIElement adornedElement) : base(adornedElement)
            {
                _root = new Grid();
                this.AddVisualChild(_root);
                this.AddLogicalChild(_root);
            }

            /// <summary>
            /// The panel the quadrant stacks are placed in.
            /// </summary>
            public Grid Root => _root;

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
        }
    }
}
