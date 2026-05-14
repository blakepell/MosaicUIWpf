/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.IO.HotKey
{
    /// <summary>
    /// Arguments for key pressed event which contain information about pressed hot key.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyPressedEventArgs"/> class.
        /// </summary>
        /// <param name="hotKey">The hot key.</param>
        public KeyPressedEventArgs(HotKey hotKey)
        {
            this.HotKey = hotKey;
        }

        /// <summary>
        /// Gets the hot key.
        /// </summary>
        public HotKey HotKey { get; private set; }
    }
}