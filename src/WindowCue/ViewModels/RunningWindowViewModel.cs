/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using WindowCue.Interop;

namespace WindowCue.ViewModels
{
    /// <summary>
    /// Wraps a <see cref="WindowInfo"/> for display in the Select Window dialog list.
    /// </summary>
    public partial class RunningWindowViewModel : ObservableObject
    {
        /// <summary>The underlying window info snapshot.</summary>
        public WindowInfo Source { get; }

        /// <summary>Window title used for display and filtering.</summary>
        public string Title => Source.Title;

        /// <summary>Process name used for secondary display and filtering.</summary>
        public string ProcessName => Source.ProcessName;

        /// <summary>Full executable path shown as a tooltip.</summary>
        public string? ExecutablePath => Source.ExecutablePath;

        /// <summary>Icon extracted for this window; set asynchronously after construction.</summary>
        [ObservableProperty]
        private ImageSource? _icon;

        public RunningWindowViewModel(WindowInfo source)
        {
            Source = source;
        }
    }
}
