/*
 * MosaicTextEditor
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace MosaicTextEditor.Models
{
    /// <summary>
    /// Identifies the editor surface used by an open document.
    /// </summary>
    public enum EditorDocumentKind
    {
        /// <summary>
        /// A plain Mosaic syntax editor document.
        /// </summary>
        Syntax,

        /// <summary>
        /// A Mosaic markdown editor document.
        /// </summary>
        Markdown
    }
}
