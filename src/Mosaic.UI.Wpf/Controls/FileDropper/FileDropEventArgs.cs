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
    /// Provides data for the <see cref="FileDropper.FileDrop"/> routed event.
    /// </summary>
    public class FileDropEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The full paths of the files that were dropped onto the <see cref="FileDropper"/>.
        /// </summary>
        public IReadOnlyList<string> Files { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDropEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier for this event.</param>
        /// <param name="source">The object that raised the event.</param>
        /// <param name="files">The full paths of the dropped files.</param>
        public FileDropEventArgs(RoutedEvent routedEvent, object source, IReadOnlyList<string> files)
            : base(routedEvent, source)
        {
            this.Files = files;
        }
    }

    /// <summary>
    /// Represents the method that will handle the <see cref="FileDropper.FileDrop"/> routed event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="FileDropEventArgs"/> that contains the dropped files.</param>
    public delegate void FileDropEventHandler(object sender, FileDropEventArgs e);
}
