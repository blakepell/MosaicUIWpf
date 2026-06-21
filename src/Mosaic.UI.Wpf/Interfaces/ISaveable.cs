/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Interfaces
{
    /// <summary>
    /// An object that can be saved.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Synchronously saves the object.
        /// </summary>
        void Save();

        /// <summary>
        /// Asynchronously saves the object.
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Asynchronously saves the object with user input as to the location.
        /// </summary>
        Task SaveAsAsync();

        /// <summary>
        /// Whether the object has been modified.
        /// </summary>
        bool IsModified { get; }

        /// <summary>
        /// The file path of the object.
        /// </summary>
        string? FilePath { get; }
    }
}
