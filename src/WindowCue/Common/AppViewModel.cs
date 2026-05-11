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

namespace WindowCue.Common
{
    public partial class AppViewModel : ObservableObject
    {
        [ObservableProperty]
        private AppSettings _appSettings = new();

        [ObservableProperty]
        private string _title = "WindowCue";
    }
}
