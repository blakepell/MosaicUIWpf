/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System;

namespace Mosaic.UI.Wpf.Interfaces
{
    /// <summary>
    /// An object that can be saved.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Raised before a save operation begins. Set <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to
        /// <c>true</c> to cancel the save. The supplied <see cref="DocumentSavingEventArgs"/> exposes the document and
        /// its target file path so the caller can inspect the current state before the save occurs.
        /// </summary>
        event EventHandler<DocumentSavingEventArgs>? OnSaving;

        /// <summary>
        /// Raised after the object has been successfully written to disk.
        /// </summary>
        event EventHandler<DocumentSavedEventArgs>? OnSaved;

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
