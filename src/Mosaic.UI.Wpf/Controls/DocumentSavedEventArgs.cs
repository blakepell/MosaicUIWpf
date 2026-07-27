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
using System;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides data for a document saved event, reported after content has been successfully written to disk.
    /// </summary>
    public sealed class DocumentSavedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentSavedEventArgs"/> class.
        /// </summary>
        /// <param name="filePath">The full path the document was saved to.</param>
        public DocumentSavedEventArgs(string filePath)
        {
            this.FilePath = filePath;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentSavedEventArgs"/> class.
        /// </summary>
        /// <param name="filePath">The full path the document was saved to.</param>
        /// <param name="document">The object that implements ISaveable.</param>
        public DocumentSavedEventArgs(string filePath, ISaveable document)
        {
            this.FilePath = filePath;
            this.Document = document;
        }

        /// <summary>
        /// Gets the document being saved, allowing the caller to inspect its current state before the save occurs.
        /// </summary>
        public ISaveable? Document { get; }

        /// <summary>
        /// Gets the full path the document was saved to.
        /// </summary>
        public string FilePath { get; }
    }
}
