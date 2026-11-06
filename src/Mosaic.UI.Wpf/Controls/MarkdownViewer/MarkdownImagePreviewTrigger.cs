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
    /// The mouse gesture that opens an image in a <see cref="MarkdownViewer"/> in a preview window
    /// hosting an <see cref="ImageViewer"/>.
    /// </summary>
    public enum MarkdownImagePreviewTrigger
    {
        /// <summary>
        /// Images cannot be opened in a preview window.
        /// </summary>
        None,

        /// <summary>
        /// A single left click on an image opens the preview.
        /// </summary>
        Click,

        /// <summary>
        /// A double click on an image opens the preview.
        /// </summary>
        DoubleClick
    }
}
