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
    /// Wraps either a <see cref="WindowInfo"/> or a <see cref="BrowserTabInfo"/> for
    /// display in the Select Window/Tab dialog list.
    /// </summary>
    public partial class RunningWindowViewModel : ObservableObject
    {
        // ── Source data ───────────────────────────────────────────────────────

        /// <summary>
        /// The underlying window info snapshot. <see langword="null"/> when this item
        /// represents a browser tab rather than a desktop window.
        /// </summary>
        public WindowInfo? Source { get; }

        /// <summary>
        /// The underlying browser-tab snapshot. <see langword="null"/> when this item
        /// represents a regular desktop window.
        /// </summary>
        public BrowserTabInfo? BrowserTab { get; }

        // ── Display properties ────────────────────────────────────────────────

        /// <summary>
        /// <see langword="true"/> when this item represents a browser tab rather than a
        /// regular desktop window.
        /// </summary>
        public bool IsEdgeTab => BrowserTab != null;

        /// <summary>
        /// Window or tab title used for display and filtering.
        /// </summary>
        public string Title => IsEdgeTab ? BrowserTab!.Title : Source!.Title;

        /// <summary>
        /// Process name used for secondary display and filtering.
        /// </summary>
        public string ProcessName => IsEdgeTab ? BrowserTab!.BrowserProcessName : Source!.ProcessName;

        /// <summary>
        /// Full executable path shown as a tooltip.
        /// </summary>
        public string? ExecutablePath => IsEdgeTab ? BrowserTab!.ExecutablePath : Source!.ExecutablePath;

        /// <summary>
        /// Human-readable subtitle shown below the title in the picker.
        /// Regular windows show their process name; browser tabs show a descriptive label.
        /// </summary>
        public string DisplaySubtitle => IsEdgeTab ? BuildTabSubtitle() : ProcessName;

        /// <summary>
        /// Icon extracted for this item; set asynchronously after construction.
        /// </summary>
        [ObservableProperty]
        private ImageSource? _icon;

        // ── Constructors ──────────────────────────────────────────────────────

        /// <summary>
        /// Initializes a view model for a regular desktop window.
        /// </summary>
        public RunningWindowViewModel(WindowInfo source)
        {
            Source = source;
            BrowserTab = null;
        }

        /// <summary>
        /// Initializes a view model for a browser tab.
        /// </summary>
        public RunningWindowViewModel(BrowserTabInfo tab)
        {
            Source = null;
            BrowserTab = tab;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string BuildTabSubtitle()
        {
            string browser = BrowserTab!.BrowserProcessName switch
            {
                "msedge" => "Microsoft Edge",
                "chrome" => "Google Chrome",
                "brave" => "Brave",
                "opera" => "Opera",
                _ => BrowserTab.BrowserProcessName
            };

            return $"{browser} — Tab";
        }
    }
}
