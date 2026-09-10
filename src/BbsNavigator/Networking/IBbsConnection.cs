/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls.VT52Terminal;
using System.Text;

namespace BbsNavigator.Networking
{
    /// <summary>
    /// Handles the raw payload bytes a BBS connection received, before they were decoded to text.
    /// </summary>
    /// <param name="payload">The bytes as they arrived from the remote system.</param>
    /// <remarks>The span is only valid for the duration of the call; copy anything you retain.</remarks>
    public delegate void BbsRawDataHandler(ReadOnlySpan<byte> payload);

    /// <summary>
    /// Defines the connection surface a BBS session document drives, regardless of whether
    /// the session runs over Telnet or SSH.
    /// </summary>
    public interface IBbsConnection : ITerminalConnection, IAsyncDisposable
    {
        /// <summary>
        /// Gets the remote host name.
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Gets the remote port.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets or sets the encoding used to interpret incoming terminal text. Assigning a new
        /// value while connected swaps the incremental decoder and takes effect on the next read.
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// Gets the total payload bytes received since the connection opened.
        /// </summary>
        long BytesReceived { get; }

        /// <summary>
        /// Gets the total payload bytes sent since the connection opened.
        /// </summary>
        long BytesSent { get; }

        /// <summary>
        /// Gets the UTC time the current connection was established, or <see langword="null"/> when disconnected.
        /// </summary>
        DateTime? ConnectedAtUtc { get; }

        /// <summary>
        /// Gets whether a file transfer currently owns the byte stream.
        /// </summary>
        bool IsBinaryModeActive { get; }

        /// <summary>
        /// Occurs when the peer closes the connection or the transport fails.
        /// </summary>
        event EventHandler<Exception?>? ConnectionLost;

        /// <summary>
        /// Occurs when terminal payload bytes arrive, before they are decoded to text. Bytes routed
        /// to a file transfer channel are not reported here.
        /// </summary>
        event BbsRawDataHandler? RawDataReceived;

        /// <summary>
        /// Connects asynchronously with cancellation support.
        /// </summary>
        /// <param name="cancellationToken">A token that abandons the connection attempt.</param>
        /// <returns><see langword="true"/> when the session is established.</returns>
        Task<bool> ConnectAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Suspends terminal text delivery and hands the raw payload stream to a file
        /// transfer protocol. Dispose the returned channel to resume terminal operation.
        /// </summary>
        /// <returns>The transfer channel that now owns the byte stream.</returns>
        /// <exception cref="InvalidOperationException">Not connected, or a transfer is already active.</exception>
        IBbsBinaryChannel EnterBinaryMode();
    }
}
