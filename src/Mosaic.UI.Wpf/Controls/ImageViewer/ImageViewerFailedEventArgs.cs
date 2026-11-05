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
    /// Identifies the <see cref="ImageViewer"/> operation that failed.
    /// </summary>
    public enum ImageViewerOperation
    {
        /// <summary>Loading the image referenced by <see cref="ImageViewer.ImagePath"/>.</summary>
        Load,

        /// <summary>Saving the image to disk ("Save As").</summary>
        Save,

        /// <summary>Copying the image to the clipboard.</summary>
        CopyImage,

        /// <summary>Copying the image's file path to the clipboard.</summary>
        CopyPath
    }

    /// <summary>
    /// Provides data for the <see cref="ImageViewer.ImageFailed"/> and <see cref="ImageViewer.OperationFailed"/> routed events.
    /// </summary>
    public sealed class ImageViewerFailedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewerFailedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The element raising the event.</param>
        /// <param name="path">The path or URI involved in the failed operation.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="operation">The operation that failed.</param>
        public ImageViewerFailedEventArgs(RoutedEvent routedEvent, object source, string path, Exception exception,
            ImageViewerOperation operation = ImageViewerOperation.Load)
            : base(routedEvent, source)
        {
            Path = path;
            Exception = exception;
            Operation = operation;
        }

        /// <summary>
        /// Gets the path or URI involved in the failed operation: the image being loaded, the save destination or
        /// the source image for clipboard operations. May be empty for in-memory images.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the exception that caused the failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the operation that failed.
        /// </summary>
        public ImageViewerOperation Operation { get; }

        /// <inheritdoc/>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            ((EventHandler<ImageViewerFailedEventArgs>)genericHandler)(genericTarget, this);
        }
    }
}
