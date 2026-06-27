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
    /// Provides data for the <see cref="Files.NavigateError"/> routed event, raised when folder
    /// navigation fails (for example the folder no longer exists or access is denied).
    /// </summary>
    public class FilesNavigateErrorEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The full path of the folder that the control attempted to navigate to.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The exception that occurred while attempting to navigate.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilesNavigateErrorEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier for this event.</param>
        /// <param name="source">The object that raised the event.</param>
        /// <param name="path">The full path of the folder that navigation was attempted to.</param>
        /// <param name="exception">The exception that occurred while navigating.</param>
        public FilesNavigateErrorEventArgs(RoutedEvent routedEvent, object source, string path, Exception exception)
            : base(routedEvent, source)
        {
            this.Path = path;
            this.Exception = exception;
        }
    }

    /// <summary>
    /// Represents the method that will handle the <see cref="Files.NavigateError"/> routed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="FilesNavigateErrorEventArgs"/> that contains the failure details.</param>
    public delegate void FilesNavigateErrorEventHandler(object sender, FilesNavigateErrorEventArgs e);
}
