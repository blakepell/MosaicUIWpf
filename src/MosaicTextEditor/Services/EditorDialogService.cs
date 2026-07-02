/*
 * MosaicTextEditor
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Win32;
using MosaicTextEditor.Models;
using System.IO;

namespace MosaicTextEditor.Services
{
    /// <summary>
    /// WPF common-dialog implementation for editor file operations.
    /// </summary>
    public sealed class EditorDialogService : IEditorDialogService
    {
        /// <inheritdoc />
        public string? ShowOpenFileDialog(string? initialDirectory)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*",
                InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
                Multiselect = false,
                Title = "Open File"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <inheritdoc />
        public string? ShowSaveFileDialog(EditorDocument document, string? initialDirectory)
        {
            string filter = document.Kind == EditorDocumentKind.Markdown
                ? "Markdown Files (*.md;*.markdown)|*.md;*.markdown|Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
                : "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                FileName = !string.IsNullOrWhiteSpace(document.FilePath)
                    ? Path.GetFileName(document.FilePath)
                    : document.FileName,
                Filter = filter,
                InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
                OverwritePrompt = true,
                Title = "Save File"
            };

            if (document.Kind == EditorDocumentKind.Markdown)
            {
                dialog.DefaultExt = ".md";
            }

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        /// <inheritdoc />
        public string? ShowOpenFolderDialog(string? initialDirectory)
        {
            var dialog = new OpenFolderDialog
            {
                InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : string.Empty,
                Title = "Open Folder"
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }
    }
}
