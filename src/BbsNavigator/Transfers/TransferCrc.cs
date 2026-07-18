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
    /// Provides the CRC routines used by the XMODEM, YMODEM, and ZMODEM protocols.
    /// </summary>
    public static class TransferCrc
    {
        private static readonly uint[] _crc32Table = BuildCrc32Table();

        /// <summary>
        /// Updates a CRC-16/XMODEM value (polynomial 0x1021, initial value 0) with one byte.
        /// </summary>
        /// <param name="crc">The running CRC value.</param>
        /// <param name="value">The byte to accumulate.</param>
        /// <returns>The updated CRC value.</returns>
        public static ushort UpdateCrc16(ushort crc, byte value)
        {
            crc ^= (ushort)(value << 8);

            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }

            return crc;
        }

        /// <summary>
        /// Computes a CRC-16/XMODEM value over a span of bytes.
        /// </summary>
        /// <param name="data">The bytes to checksum.</param>
        /// <returns>The CRC-16 of the span.</returns>
        public static ushort ComputeCrc16(ReadOnlySpan<byte> data)
        {
            ushort crc = 0;

            foreach (byte value in data)
            {
                crc = UpdateCrc16(crc, value);
            }

            return crc;
        }

        /// <summary>
        /// Updates a reflected CRC-32 value (polynomial 0xEDB88320) with one byte. Callers
        /// must start from 0xFFFFFFFF and complement the final result, which matches the
        /// ZMODEM CRC-32 frame convention.
        /// </summary>
        /// <param name="crc">The running CRC value.</param>
        /// <param name="value">The byte to accumulate.</param>
        /// <returns>The updated CRC value.</returns>
        public static uint UpdateCrc32(uint crc, byte value)
        {
            return _crc32Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        /// <summary>
        /// Computes the 8-bit arithmetic checksum used by original XMODEM.
        /// </summary>
        /// <param name="data">The bytes to checksum.</param>
        /// <returns>The low byte of the arithmetic sum.</returns>
        public static byte ComputeChecksum(ReadOnlySpan<byte> data)
        {
            byte sum = 0;

            foreach (byte value in data)
            {
                sum = unchecked((byte)(sum + value));
            }

            return sum;
        }

        private static uint[] BuildCrc32Table()
        {
            var table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;

                for (int bit = 0; bit < 8; bit++)
                {
                    entry = (entry & 1) != 0 ? (entry >> 1) ^ 0xEDB88320u : entry >> 1;
                }

                table[i] = entry;
            }

            return table;
        }
    }
}
