/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Provides application-wide observable state.
    /// </summary>
    public partial class AppViewModel : ObservableObject
    {
        /// <summary>
        /// Gets or sets the application settings exposed to the user interface.
        /// </summary>
        /// <value>The current application settings.</value>
        [ObservableProperty]
        private AppSettings _appSettings = new();

        /// <summary>
        /// Gets or sets the application title.
        /// </summary>
        /// <value>The title displayed by the application.</value>
        [ObservableProperty]
        private string _title = "BBS Navigator";
    }
}
