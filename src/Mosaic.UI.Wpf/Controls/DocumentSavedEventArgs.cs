/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

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
        /// Gets the full path the document was saved to.
        /// </summary>
        public string FilePath { get; }
    }
}
