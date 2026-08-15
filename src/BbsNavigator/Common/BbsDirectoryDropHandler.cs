/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using GongSolutions.Wpf.DragDrop;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Restricts BBS directory drag-and-drop operations to flat list reordering.
    /// </summary>
    public sealed class BbsDirectoryDropHandler : DefaultDropHandler
    {
        /// <summary>
        /// Gets the shared drop handler instance.
        /// </summary>
        public static BbsDirectoryDropHandler Instance { get; } = new();

        /// <inheritdoc />
        public override void DragOver(IDropInfo dropInfo)
        {
            dropInfo.AcceptChildItem = false;
            base.DragOver(dropInfo);
        }

        /// <inheritdoc />
        public override void Drop(IDropInfo dropInfo)
        {
            dropInfo.AcceptChildItem = false;
            base.Drop(dropInfo);
        }
    }
}
