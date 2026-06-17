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
    /// Provides data for the <see cref="Files.FileDoubleClick"/> routed event.
    /// </summary>
    public class FileActivatedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The file that was activated (double-clicked).
        /// </summary>
        public FileInfo File { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileActivatedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier for this event.</param>
        /// <param name="source">The object that raised the event.</param>
        /// <param name="file">The file that was activated.</param>
        public FileActivatedEventArgs(RoutedEvent routedEvent, object source, FileInfo file)
            : base(routedEvent, source)
        {
            this.File = file;
        }
    }

    /// <summary>
    /// Represents the method that will handle the <see cref="Files.FileDoubleClick"/> routed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="FileActivatedEventArgs"/> that contains the activated file.</param>
    public delegate void FileActivatedEventHandler(object sender, FileActivatedEventArgs e);
}
