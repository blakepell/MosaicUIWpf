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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides data for the <see cref="ImageViewer.ImageFailed"/> routed event.
    /// </summary>
    public sealed class ImageViewerFailedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewerFailedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The element raising the event.</param>
        /// <param name="path">The path or URI that failed to load.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        public ImageViewerFailedEventArgs(RoutedEvent routedEvent, object source, string path, Exception exception)
            : base(routedEvent, source)
        {
            Path = path;
            Exception = exception;
        }

        /// <summary>
        /// Gets the path or URI that failed to load.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the exception that caused the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <inheritdoc/>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ((EventHandler<ImageViewerFailedEventArgs>)genericHandler)(genericTarget, this);
        }
    }
}
