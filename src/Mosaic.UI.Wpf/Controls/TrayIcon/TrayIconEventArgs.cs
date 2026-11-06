/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Identifies how the user interacted with a <see cref="TrayIcon"/>.
    /// </summary>
    public enum TrayIconInputSource
    {
        /// <summary>The interaction came from the mouse (or pen/touch promoted to mouse).</summary>
        Mouse,

        /// <summary>The interaction came from the keyboard (Enter/Space, Shift+F10 or the Menu key while the icon has focus).</summary>
        Keyboard
    }

    /// <summary>
    /// Provides data for <see cref="TrayIcon"/> interaction events.
    /// </summary>
    public class TrayIconEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrayIconEventArgs"/> class.
        /// </summary>
        /// <param name="inputSource">The device that produced the interaction.</param>
        /// <param name="screenPosition">The anchor position in physical screen pixels.</param>
        public TrayIconEventArgs(TrayIconInputSource inputSource, Point screenPosition)
        {
            this.InputSource = inputSource;
            this.ScreenPosition = screenPosition;
        }

        /// <summary>
        /// Gets the device that produced the interaction.
        /// </summary>
        public TrayIconInputSource InputSource { get; }

        /// <summary>
        /// Gets the position reported by the shell, in physical screen pixels (not device-independent units).
        /// For mouse input this is the cursor; for keyboard input it is the icon's location.
        /// </summary>
        public Point ScreenPosition { get; }

        /// <summary>
        /// Gets or sets whether the interaction has been handled. Setting this in a
        /// <see cref="TrayIcon.RightClick"/> handler suppresses the associated command and the context menu;
        /// in the other events it suppresses the associated command.
        /// </summary>
        public bool Handled { get; set; }
    }
}
