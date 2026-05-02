/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// Defines the connection surface used by <see cref="VT52Terminal"/>.
    /// </summary>
    public interface ITerminalConnection : IDisposable
    {
        /// <summary>
        /// Raised when remote terminal data is available.
        /// </summary>
        event EventHandler<string>? DataReceived;

        /// <summary>
        /// Gets whether the connection is active.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets or sets the terminal column count requested from the remote host.
        /// </summary>
        int Columns { get; set; }

        /// <summary>
        /// Gets or sets the terminal row count requested from the remote host.
        /// </summary>
        int Rows { get; set; }

        /// <summary>
        /// Gets or sets the terminal width in device pixels.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// Gets or sets the terminal height in device pixels.
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Opens the connection.
        /// </summary>
        bool Connect();

        /// <summary>
        /// Opens the connection asynchronously.
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Closes the connection.
        /// </summary>
        bool Disconnect();

        /// <summary>
        /// Closes the connection asynchronously.
        /// </summary>
        Task<bool> DisconnectAsync();

        /// <summary>
        /// Sends text to the remote host.
        /// </summary>
        void Send(string text);

        /// <summary>
        /// Sends text to the remote host asynchronously.
        /// </summary>
        Task SendAsync(string text);

        /// <summary>
        /// Sends bytes to the remote host.
        /// </summary>
        void Send(byte[] data);

        /// <summary>
        /// Sends bytes to the remote host asynchronously.
        /// </summary>
        Task SendAsync(byte[] data);

        /// <summary>
        /// Notifies the remote host that the terminal size changed.
        /// </summary>
        void SendWindowChangeRequest(uint cols, uint rows, uint width, uint height);
    }
}
