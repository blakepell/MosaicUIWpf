/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Specifies what should happen with the image produced when a <see cref="UIElement"/> is captured
    /// by <see cref="VisualCapture"/>.
    /// </summary>
    public enum VisualCaptureMode
    {
        /// <summary>
        /// The rendered image is placed on the clipboard.
        /// </summary>
        CopyToClipboard = 0,

        /// <summary>
        /// The rendered image is written to an image file.  When no file path has been supplied the
        /// user is prompted with a save file dialog.
        /// </summary>
        SaveToFile = 1
    }
}
