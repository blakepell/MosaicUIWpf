using AvalonDock.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Represents the drop target.
    /// </summary>
    /// <typeparam name="T">The type of t.</typeparam>
    internal abstract class DropTarget<T> : DropTargetBase, IDropTarget
        where T : FrameworkElement
    {
        private Rect[] _detectionRect;
        private T _targetElement;
        private DropTargetType _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="DropTarget{T}"/> class.
        /// </summary>
        /// <param name="targetElement">The target element.</param>
        /// <param name="detectionRect">The detection rectangle.</param>
        /// <param name="type">The drop target type.</param>
        protected DropTarget(T targetElement, Rect detectionRect, DropTargetType type)
        {
            _targetElement = targetElement;
            _detectionRect = new[] { detectionRect };
            _type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DropTarget{T}"/> class.
        /// </summary>
        /// <param name="targetElement">The target element.</param>
        /// <param name="detectionRects">The detection rectangles.</param>
        /// <param name="type">The drop target type.</param>
        protected DropTarget(T targetElement, IEnumerable<Rect> detectionRects, DropTargetType type)
        {
            _targetElement = targetElement;
            _detectionRect = detectionRects.ToArray();
            _type = type;
        }

        /// <summary>
        /// Gets the detection rects.
        /// </summary>
        public Rect[] DetectionRects => _detectionRect;

        /// <summary>
        /// Gets the target element.
        /// </summary>
        public T TargetElement => _targetElement;

        /// <summary>
        /// Gets the type.
        /// </summary>
        public DropTargetType Type => _type;

        /// <summary>
        /// Drops the specified floating window onto the target.
        /// </summary>
        /// <param name="floatingWindow">The floating window.</param>
        protected virtual void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
        }

        /// <summary>
        /// Drops the specified floating window onto the target.
        /// </summary>
        /// <param name="floatingWindow">The floating window.</param>
        protected virtual void Drop(LayoutDocumentFloatingWindow floatingWindow)
        {
        }

        /// <summary>
        /// Determines whether the specified point intersects the target.
        /// </summary>
        /// <param name="dragPoint">The drag point.</param>
        /// <returns>true if the specified point intersects the target; otherwise, false.</returns>
        public bool HitTestScreen(Point dragPoint)
        {
            return HitTest(_targetElement.TransformToDeviceDPI(dragPoint));
        }

        /// <summary>
        /// Drops the specified floating window onto the target.
        /// </summary>
        /// <param name="floatingWindow">The floating window.</param>
        public void Drop(LayoutFloatingWindow floatingWindow)
        {
            var currentActiveContent = floatingWindow.Root.ActiveContent;

            if (floatingWindow is LayoutAnchorableFloatingWindow fwAsAnchorable)
            {
                Drop(fwAsAnchorable);
            }
            else
            {
                var fwAsDocument = floatingWindow as LayoutDocumentFloatingWindow;
                Drop(fwAsDocument);
            }

            if (currentActiveContent == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    currentActiveContent.IsSelected = false;
                    currentActiveContent.IsActive = false;
                    currentActiveContent.IsActive = true;
                }), DispatcherPriority.Background);
        }

        /// <summary>
        /// Determines whether the specified point intersects the target.
        /// </summary>
        /// <param name="dragPoint">The drag point.</param>
        /// <returns>true if the specified point intersects the target; otherwise, false.</returns>
        public bool HitTest(Point dragPoint)
        {
            return _detectionRect.Any(dr => dr.Contains(dragPoint));
        }

        /// <summary>
        /// Gets the preview path.
        /// </summary>
        /// <param name="overlayWindow">The overlay window.</param>
        /// <param name="floatingWindow">The floating window.</param>
        /// <returns>The preview path.</returns>
        public abstract Geometry GetPreviewPath(OverlayWindow overlayWindow, LayoutFloatingWindow floatingWindow);

        /// <summary>
        /// Drag enter.
        /// </summary>
        public void DragEnter()
        {
            SetIsDraggingOver(TargetElement, true);
        }

        /// <summary>
        /// Drag leave.
        /// </summary>
        public void DragLeave()
        {
            SetIsDraggingOver(TargetElement, false);
        }
    }
}