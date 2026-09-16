/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Common;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Event data for a completed or failed visual capture.
    /// </summary>
    public class VisualCapturedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualCapturedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The element raising the event.</param>
        /// <param name="mode">The capture that was attempted.</param>
        /// <param name="target">The element that was captured.</param>
        /// <param name="filePath">The file written for <see cref="VisualCaptureMode.SaveToFile"/>, otherwise <see langword="null"/>.</param>
        /// <param name="error">The exception that stopped the capture, or <see langword="null"/> on success.</param>
        public VisualCapturedEventArgs(RoutedEvent routedEvent, object source, VisualCaptureMode mode, UIElement? target, string? filePath, Exception? error)
            : base(routedEvent, source)
        {
            this.Mode = mode;
            this.Target = target;
            this.FilePath = filePath;
            this.Error = error;
        }

        /// <summary>
        /// Gets the capture that was attempted.
        /// </summary>
        public VisualCaptureMode Mode { get; }

        /// <summary>
        /// Gets the element that was captured, or <see langword="null"/> if it could not be resolved.
        /// </summary>
        public UIElement? Target { get; }

        /// <summary>
        /// Gets the path the image was saved to.  <see langword="null"/> for clipboard copies and failures.
        /// </summary>
        public string? FilePath { get; }

        /// <summary>
        /// Gets the exception that prevented the capture, or <see langword="null"/> when it succeeded.
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// Gets whether the capture succeeded.
        /// </summary>
        public bool Succeeded => this.Error == null;
    }
}
