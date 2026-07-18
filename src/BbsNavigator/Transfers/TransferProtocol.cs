/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Transfers
{
    /// <summary>
    /// Identifies a classic BBS file transfer protocol.
    /// </summary>
    public enum TransferProtocol
    {
        /// <summary>
        /// ZMODEM with streaming subpackets, CRC-32 frames, and crash recovery. The
        /// de facto standard on late-era bulletin boards.
        /// </summary>
        [Description("ZMODEM")]
        Zmodem,

        /// <summary>
        /// YMODEM batch transfer: 1K blocks, CRC-16, and a header block that carries
        /// the file name and exact length.
        /// </summary>
        [Description("YMODEM (batch, 1K)")]
        Ymodem,

        /// <summary>
        /// XMODEM-1K: 1024-byte blocks with CRC-16 error detection.
        /// </summary>
        [Description("XMODEM-1K")]
        Xmodem1K,

        /// <summary>
        /// XMODEM-CRC: 128-byte blocks with CRC-16 error detection.
        /// </summary>
        [Description("XMODEM-CRC")]
        XmodemCrc,

        /// <summary>
        /// Original XMODEM: 128-byte blocks with an 8-bit arithmetic checksum.
        /// </summary>
        [Description("XMODEM (checksum)")]
        Xmodem
    }

    /// <summary>
    /// Identifies the direction of a file transfer relative to this client.
    /// </summary>
    public enum TransferDirection
    {
        /// <summary>
        /// The remote system is sending one or more files to this client.
        /// </summary>
        Download,

        /// <summary>
        /// This client is sending one or more files to the remote system.
        /// </summary>
        Upload
    }
}
