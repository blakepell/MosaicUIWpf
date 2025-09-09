/*
 * MosaicTemplateApp
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;

namespace MosaicTemplateApp.Common
{
    /// <summary>
    /// A static instance application view model that can be used for observable
    /// app settings.  The AppViewModel is generally not persisted beyond the execution
    /// run of the app.  Prefer the <see cref="AppSettings"/> class for settings or state
    /// that needs to be saved and reloaded.
    /// </summary>
    public partial class AppViewModel : ObservableObject
    {
        /// <summary>
        /// A reference to the <see cref="AppSettings"/> for the project so that the application
        /// view model can be bound to the base windows/controls and we don't have to manage some
        /// things being bound to different things.
        /// </summary>
        [ObservableProperty]
        private AppSettings _appSettings = new();

        /// <summary>
        /// The title of the application
        /// </summary>
        [ObservableProperty]
        private string _title = "MosaicTemplateApp";

    }
}
