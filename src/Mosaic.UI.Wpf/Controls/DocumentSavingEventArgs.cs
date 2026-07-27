/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Interfaces;
using System.ComponentModel;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides data for a document saving event, raised before content is written to disk. Set
    /// <see cref="CancelEventArgs.Cancel"/> to <c>true</c> to abort the save. Consumers may inspect
    /// <see cref="Document"/> to examine the current state of the control prior to the save.
    /// </summary>
    public class DocumentSavingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentSavingEventArgs"/> class.
        /// </summary>
        /// <param name="filePath">The full path the document will be saved to, or <c>null</c> if not yet determined.</param>
        public DocumentSavingEventArgs(string filePath)
        {
            this.FilePath = filePath;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentSavingEventArgs"/> class.
        /// </summary>
        /// <param name="document">The document being saved.</param>
        /// <param name="filePath">The full path the document will be saved to, or <c>null</c> if not yet determined.</param>
        public DocumentSavingEventArgs(string? filePath, ISaveable document)
        {
            this.Document = document;
            this.FilePath = filePath;
        }

        /// <summary>
        /// Gets the document being saved, allowing the caller to inspect its current state before the save occurs.
        /// </summary>
        public ISaveable? Document { get; }

        /// <summary>
        /// Gets the full path the document will be saved to, or <c>null</c> if a location has not yet been determined.
        /// </summary>
        public string? FilePath { get; }

    }
}
