/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Common;

namespace Mosaic.UI.Wpf.Interfaces
{
    /// <summary>
    /// Implements the base set of properties that an AppSettings class should provide.
    /// </summary>
    public interface IAppSettings
    {
        /// <summary>
        /// The application data folder that holds the LocalSettings.json file.
        /// </summary>
        public string? ApplicationDataFolder { get; set; }

        /// <summary>
        /// Client settings that are specific to the app on a single workstation.
        /// </summary>
        public LocalSettings? ClientSettings { get; set; }

        /// <summary>
        /// The applications theme.
        /// </summary>
        public ThemeMode Theme { get; set; }
    }
}