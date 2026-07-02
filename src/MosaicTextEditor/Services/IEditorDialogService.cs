/*
 * MosaicTextEditor
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using MosaicTextEditor.Models;

namespace MosaicTextEditor.Services
{
    /// <summary>
    /// Provides file and folder dialogs for editor commands.
    /// </summary>
    public interface IEditorDialogService
    {
        /// <summary>
        /// Prompts the user for a file to open.
        /// </summary>
        string? ShowOpenFileDialog(string? initialDirectory);

        /// <summary>
        /// Prompts the user for a destination file path.
        /// </summary>
        string? ShowSaveFileDialog(EditorDocument document, string? initialDirectory);

        /// <summary>
        /// Prompts the user for a folder to display in the file explorer.
        /// </summary>
        string? ShowOpenFolderDialog(string? initialDirectory);
    }
}
