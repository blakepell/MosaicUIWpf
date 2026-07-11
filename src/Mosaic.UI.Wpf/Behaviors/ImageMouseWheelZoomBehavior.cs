/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.ComponentModel;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Adds mouse-wheel zooming to an <see cref="Image"/> by applying a render scale transform.
    /// </summary>
    /// <remarks>
    /// The behavior zooms around the current mouse position and resets when the image source changes
    /// unless <see cref="ResetOnSourceChanged"/> is set to <see langword="false" />. By default the
    /// transform is applied to the image itself; set <see cref="ZoomTarget"/> to scale a containing
    /// element instead (for example a border that draws a checkerboard behind the image) so the
    /// image and its backdrop zoom together.
    /// </remarks>
    public class ImageMouseWheelZoomBehavior : Behavior<Image>
    {
        private readonly ScaleTransform _scaleTransform = new(1.0, 1.0);

        private DependencyPropertyDescriptor? _sourceDescriptor;
        private FrameworkElement? _appliedTarget;
        private Transform? _originalRenderTransform;
        private Point _originalRenderTransformOrigin;
        private double _zoom = 1.0;

        /// <summary>
        /// Identifies the <see cref="MinZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register(
            nameof(MinZoom),
            typeof(double),
            typeof(ImageMouseWheelZoomBehavior),
            new PropertyMetadata(1.0));

        /// <summary>
        /// Gets or sets the minimum zoom scale.
        /// </summary>
        /// <value>
        /// The minimum zoom scale. The default is 1.0.
        /// </value>
        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxZoom"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register(
            nameof(MaxZoom),
            typeof(double),
            typeof(ImageMouseWheelZoomBehavior),
            new PropertyMetadata(32.0));

        /// <summary>
        /// Gets or sets the maximum zoom scale.
        /// </summary>
        /// <value>
        /// The maximum zoom scale. The default is 32.0.
        /// </value>
        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ZoomFactor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomFactorProperty = DependencyProperty.Register(
            nameof(ZoomFactor),
            typeof(double),
            typeof(ImageMouseWheelZoomBehavior),
            new PropertyMetadata(1.2));

        /// <summary>
        /// Gets or sets the multiplier applied for each mouse-wheel step.
        /// </summary>
        /// <value>
        /// The zoom multiplier. The default is 1.2.
        /// </value>
        public double ZoomFactor
        {
            get => (double)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResetOnSourceChanged"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResetOnSourceChangedProperty = DependencyProperty.Register(
            nameof(ResetOnSourceChanged),
            typeof(bool),
            typeof(ImageMouseWheelZoomBehavior),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether zoom resets when the image source changes.
        /// </summary>
        /// <value>
        /// <see langword="true" /> to reset zoom when <see cref="Image.Source"/> changes; otherwise,
        /// <see langword="false" />. The default is <see langword="true" />.
        /// </value>
        public bool ResetOnSourceChanged
        {
            get => (bool)GetValue(ResetOnSourceChangedProperty);
            set => SetValue(ResetOnSourceChangedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RequireCtrl"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RequireCtrlProperty = DependencyProperty.Register(
            nameof(RequireCtrl),
            typeof(bool),
            typeof(ImageMouseWheelZoomBehavior),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value that indicates whether the Ctrl key must be held to zoom.
        /// </summary>
        /// <value>
        /// <see langword="true" /> to require Ctrl while using the mouse wheel; otherwise,
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        public bool RequireCtrl
        {
            get => (bool)GetValue(RequireCtrlProperty);
            set => SetValue(RequireCtrlProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ZoomTarget"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomTargetProperty = DependencyProperty.Register(
            nameof(ZoomTarget),
            typeof(FrameworkElement),
            typeof(ImageMouseWheelZoomBehavior),
            new PropertyMetadata(null, ZoomTargetChanged));

        /// <summary>
        /// Gets or sets the element the zoom transform is applied to.
        /// </summary>
        /// <value>
        /// The element to scale, or <see langword="null" /> to scale the image itself. Set this to a
        /// containing element (e.g. a border with a checkerboard background) so the image and its
        /// backdrop zoom together. The default is <see langword="null" />.
        /// </value>
        public FrameworkElement? ZoomTarget
        {
            get => (FrameworkElement?)GetValue(ZoomTargetProperty);
            set => SetValue(ZoomTargetProperty, value);
        }

        private static void ZoomTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ImageMouseWheelZoomBehavior)d;

            // Only retarget when already attached; OnAttached handles the initial application.
            if (behavior._appliedTarget != null)
            {
                behavior.RestoreTarget();
                behavior.ApplyToTarget();
            }
        }

        /// <summary>
        /// Attaches the mouse-wheel handler and initializes the zoom transform.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.ApplyToTarget();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;

            _sourceDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof(Image));
            _sourceDescriptor?.AddValueChanged(AssociatedObject, AssociatedObject_SourceChanged);
        }

        /// <summary>
        /// Detaches the mouse-wheel handler and restores the original transform.
        /// </summary>
        protected override void OnDetaching()
        {
            _sourceDescriptor?.RemoveValueChanged(AssociatedObject, AssociatedObject_SourceChanged);
            _sourceDescriptor = null;

            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            this.RestoreTarget();

            base.OnDetaching();
        }

        private void ApplyToTarget()
        {
            var target = this.ZoomTarget ?? AssociatedObject;

            _appliedTarget = target;
            _originalRenderTransform = target.RenderTransform;
            _originalRenderTransformOrigin = target.RenderTransformOrigin;

            _zoom = 1.0;
            _scaleTransform.ScaleX = 1.0;
            _scaleTransform.ScaleY = 1.0;

            target.RenderTransform = CreateTransform();
        }

        private void RestoreTarget()
        {
            if (_appliedTarget == null)
            {
                return;
            }

            _appliedTarget.RenderTransform = _originalRenderTransform;
            _appliedTarget.RenderTransformOrigin = _originalRenderTransformOrigin;
            _appliedTarget = null;
        }

        private Transform CreateTransform()
        {
            if (_originalRenderTransform == null || _originalRenderTransform == Transform.Identity)
            {
                return _scaleTransform;
            }

            var group = new TransformGroup();
            group.Children.Add(_originalRenderTransform);
            group.Children.Add(_scaleTransform);
            return group;
        }

        private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var target = _appliedTarget;

            if (target == null || e.Delta == 0 || (this.RequireCtrl && (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control))
            {
                return;
            }

            double minZoom = Math.Max(0.01, this.MinZoom);
            double maxZoom = Math.Max(minZoom, this.MaxZoom);
            double zoomFactor = Math.Max(1.01, this.ZoomFactor);
            double requestedZoom = _zoom * (e.Delta > 0 ? zoomFactor : 1.0 / zoomFactor);
            double newZoom = Math.Clamp(requestedZoom, minZoom, maxZoom);

            if (Math.Abs(newZoom - _zoom) < 0.0001)
            {
                e.Handled = true;
                return;
            }

            Point position = e.GetPosition(target);
            if (target.ActualWidth > 0 && target.ActualHeight > 0)
            {
                target.RenderTransformOrigin = new Point(
                    Math.Clamp(position.X / target.ActualWidth, 0, 1),
                    Math.Clamp(position.Y / target.ActualHeight, 0, 1));
            }

            _zoom = newZoom;
            _scaleTransform.ScaleX = newZoom;
            _scaleTransform.ScaleY = newZoom;
            e.Handled = true;
        }

        private void AssociatedObject_SourceChanged(object? sender, EventArgs e)
        {
            if (this.ResetOnSourceChanged)
            {
                ResetZoom();
            }
        }

        private void ResetZoom()
        {
            _zoom = 1.0;
            _scaleTransform.ScaleX = 1.0;
            _scaleTransform.ScaleY = 1.0;

            if (_appliedTarget != null)
            {
                _appliedTarget.RenderTransformOrigin = _originalRenderTransformOrigin;
            }
        }
    }
}
