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
    /// Provides data for a file attached event, reported after a file (such as an image) has been
    /// attached to the document.
    /// </summary>
    public sealed class FileAttachedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileAttachedEventArgs"/> class.
        /// </summary>
        /// <param name="filePath">
        /// The final location of the attached file: either the original file-system location, or the
        /// <c>{Guid}.{extension}</c> copy inside the configured storage folder.
        /// </param>
        /// <param name="originalFilePath">The location of the file the user originally selected.</param>
        /// <param name="copiedToStorageFolder">
        /// <c>true</c> when the file was copied into a storage folder; <c>false</c> when the original
        /// file was referenced in place.
        /// </param>
        public FileAttachedEventArgs(string filePath, string originalFilePath, bool copiedToStorageFolder)
        {
            this.FilePath = filePath;
            this.OriginalFilePath = originalFilePath;
            this.CopiedToStorageFolder = copiedToStorageFolder;
        }

        /// <summary>
        /// Gets the final location of the attached file. When the file was copied into a storage folder
        /// this is the full path of the generated <c>{Guid}.{extension}</c> copy; otherwise it is the
        /// original file-system location.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the location of the file the user originally selected.
        /// </summary>
        public string OriginalFilePath { get; }

        /// <summary>
        /// Gets a value indicating whether the file was copied into a storage folder (<c>true</c>) or
        /// referenced in place at its original location (<c>false</c>).
        /// </summary>
        public bool CopiedToStorageFolder { get; }
    }
}
