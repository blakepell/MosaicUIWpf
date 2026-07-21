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
    /// Provides data for the <see cref="Files.RenameError"/> routed event.
    /// </summary>
    public sealed class FilesRenameErrorEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilesRenameErrorEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier.</param>
        /// <param name="source">The object that raised the event.</param>
        /// <param name="sourcePath">The original full path.</param>
        /// <param name="destinationPath">The requested full path.</param>
        /// <param name="exception">The exception raised by the file system.</param>
        public FilesRenameErrorEventArgs(
            RoutedEvent routedEvent,
            object source,
            string sourcePath,
            string destinationPath,
            Exception exception)
            : base(routedEvent, source)
        {
            this.SourcePath = sourcePath;
            this.DestinationPath = destinationPath;
            this.Exception = exception;
        }

        /// <summary>
        /// Gets the original full path.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Gets the requested full path.
        /// </summary>
        public string DestinationPath { get; }

        /// <summary>
        /// Gets the exception raised by the file system.
        /// </summary>
        public Exception Exception { get; }
    }

    /// <summary>
    /// Represents the method that handles the <see cref="Files.RenameError"/> routed event.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The rename failure details.</param>
    public delegate void FilesRenameErrorEventHandler(object sender, FilesRenameErrorEventArgs e);
}
