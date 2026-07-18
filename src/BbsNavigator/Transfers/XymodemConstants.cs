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
    /// Control bytes and tuning values shared by the XMODEM and YMODEM implementations.
    /// </summary>
    internal static class XymodemConstants
    {
        /// <summary>Start of a 128-byte block.</summary>
        public const byte Soh = 0x01;

        /// <summary>Start of a 1024-byte block.</summary>
        public const byte Stx = 0x02;

        /// <summary>End of transmission.</summary>
        public const byte Eot = 0x04;

        /// <summary>Positive acknowledgement.</summary>
        public const byte Ack = 0x06;

        /// <summary>Negative acknowledgement / checksum-mode start request.</summary>
        public const byte Nak = 0x15;

        /// <summary>Cancel; two in a row aborts the transfer.</summary>
        public const byte Can = 0x18;

        /// <summary>Padding byte (CPMEOF) used to fill the final block.</summary>
        public const byte Sub = 0x1A;

        /// <summary>CRC-mode start request sent by the receiver.</summary>
        public const byte CrcStart = (byte)'C';

        /// <summary>The maximum number of retries for a single block before aborting.</summary>
        public const int MaxRetries = 10;

        /// <summary>The timeout applied while waiting for a block or handshake byte.</summary>
        public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(10);

        /// <summary>The timeout applied to each byte inside a block.</summary>
        public static readonly TimeSpan ByteTimeout = TimeSpan.FromSeconds(5);
    }
}
