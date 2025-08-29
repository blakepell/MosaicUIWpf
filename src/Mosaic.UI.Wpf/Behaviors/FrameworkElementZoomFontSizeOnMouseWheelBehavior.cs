/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.Windows.Documents;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that enables zooming the font size of a <see cref="FrameworkElement"/> using the mouse wheel while
    /// the Ctrl key is pressed.
    /// </summary>
    /// <remarks>This behavior adjusts the font size of the associated <see cref="FrameworkElement"/> based on
    /// the mouse wheel delta. The font size is constrained within the range defined by <see cref="MinFontSize"/> and
    /// <see cref="MaxFontSize"/>. To use this behavior, attach it to a <see cref="FrameworkElement"/> and ensure the
    /// Ctrl key is held down while scrolling the mouse wheel.</remarks>
    public class FrameworkElementZoomFontSizeOnMouseWheelBehavior : Behavior<FrameworkElement>
    {
        private double? _initialFontSize;

        private int _zoomOffset;

        /// <summary>
        /// Gets or sets the maximum font size allowed for text rendering.
        /// </summary>
        public double MaxFontSize { get; set; } = 96.0;

        /// <summary>
        /// Gets or sets the minimum font size allowed for text rendering.
        /// </summary>
        public double MinFontSize { get; set; } = 4.0;

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>
        /// Override this to hook up functionality to the AssociatedObject.
        /// </remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>
        /// Override this to unhook functionality from the AssociatedObject.
        /// </remarks>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewMouseWheel"/> event to adjust the font size of the associated object
        /// when the Ctrl key is pressed and the mouse wheel is scrolled.
        /// </summary>
        /// <remarks>This method modifies the font size of the associated object based on the mouse wheel
        /// delta, provided the Ctrl key is held down. The font size is constrained within the range defined by <see
        /// cref="MinFontSize"/> and  <see cref="MaxFontSize"/>. If the font size reaches the minimum or maximum limit,
        /// no further adjustments are made.</remarks>
        /// <param name="sender">The source of the event, typically the associated object.</param>
        /// <param name="e">The <see cref="MouseWheelEventArgs"/> containing event data, including the mouse wheel delta.</param>
        private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) || (e.Delta == 0))
            {
                return;
            }

            e.Handled = true;

            var newZoomOffset = _zoomOffset + Math.Sign(e.Delta);

            var frameworkElement = AssociatedObject;

            if (frameworkElement == null)
            {
                return;
            }

            _initialFontSize ??= TextElement.GetFontSize(frameworkElement);

            var newFontSize = _initialFontSize.Value + newZoomOffset;

            if ((newFontSize < this.MinFontSize) || (newFontSize >= this.MaxFontSize))
            {
                return;
            }

            _zoomOffset = newZoomOffset;

            TextElement.SetFontSize(frameworkElement, newFontSize);

            if (newZoomOffset == 0)
            {
                _initialFontSize = null;
            }
        }
    }
}
