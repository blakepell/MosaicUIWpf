/*
 * MosaicTextEditor
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;

namespace MosaicTextEditor.Models
{
    /// <summary>
    /// Resolves which editor surface should host a file based on its extension. This is the single
    /// reference point that routes an opened file to a specialized editor (e.g. markdown); anything
    /// without a registration falls back to the general-purpose syntax editor.
    /// </summary>
    public static class EditorRegistry
    {
        /// <summary>
        /// Maps a file extension (including the leading dot) to the specialized editor surface that
        /// should host it. Register additional specialized editors here as they are added.
        /// </summary>
        private static readonly Dictionary<string, EditorDocumentKind> KindsByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".md"] = EditorDocumentKind.Markdown,
            [".markdown"] = EditorDocumentKind.Markdown,
            [".mdown"] = EditorDocumentKind.Markdown,
            [".mkd"] = EditorDocumentKind.Markdown
        };

        /// <summary>
        /// Resolves the editor surface kind for the specified file. Files that do not match a
        /// specialized editor registration fall back to <see cref="EditorDocumentKind.Syntax"/>.
        /// </summary>
        /// <param name="path">The file path or file name being opened.</param>
        public static EditorDocumentKind ResolveKind(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return EditorDocumentKind.Syntax;
            }

            string extension = Path.GetExtension(path);
            return KindsByExtension.TryGetValue(extension, out var kind) ? kind : EditorDocumentKind.Syntax;
        }
    }
}
