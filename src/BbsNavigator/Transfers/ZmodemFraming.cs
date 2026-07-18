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
    /// ZMODEM frame type identifiers.
    /// </summary>
    internal enum ZFrameType : byte
    {
        /// <summary>Request receive init (sent by the sender to start).</summary>
        ZRQINIT = 0,

        /// <summary>Receive init (receiver capabilities).</summary>
        ZRINIT = 1,

        /// <summary>Send init (optional sender attention string).</summary>
        ZSINIT = 2,

        /// <summary>Acknowledgement with position.</summary>
        ZACK = 3,

        /// <summary>File name and information follows.</summary>
        ZFILE = 4,

        /// <summary>Skip this file.</summary>
        ZSKIP = 5,

        /// <summary>Last header or subpacket was garbled.</summary>
        ZNAK = 6,

        /// <summary>Abort the batch.</summary>
        ZABORT = 7,

        /// <summary>Finish the session.</summary>
        ZFIN = 8,

        /// <summary>Resume transmission at this position.</summary>
        ZRPOS = 9,

        /// <summary>File data follows starting at this position.</summary>
        ZDATA = 10,

        /// <summary>End of file at this position.</summary>
        ZEOF = 11,

        /// <summary>Fatal read error on the receiver.</summary>
        ZFERR = 12,

        /// <summary>Request/report the file CRC.</summary>
        ZCRC = 13,

        /// <summary>Security challenge.</summary>
        ZCHALLENGE = 14,

        /// <summary>Transfer complete.</summary>
        ZCOMPL = 15,

        /// <summary>Session canceled.</summary>
        ZCAN = 16,

        /// <summary>Request free bytes on the receiver's disk.</summary>
        ZFREECNT = 17,

        /// <summary>Execute a command (unsupported; always refused).</summary>
        ZCOMMAND = 18
    }

    /// <summary>
    /// Reads and writes ZMODEM frames over an <see cref="ITransferLink"/>, handling ZDLE
    /// escaping, hex and binary headers, and CRC-16/CRC-32 data subpackets exactly as the
    /// classic <c>rz</c>/<c>sz</c> implementations expect.
    /// </summary>
    internal sealed class ZmodemFraming
    {
        /// <summary>Padding character that precedes every header.</summary>
        public const byte ZPad = (byte)'*';

        /// <summary>The ZMODEM data link escape (also the ASCII CAN character).</summary>
        public const byte Zdle = 0x18;

        /// <summary>Binary header with CRC-16 marker.</summary>
        public const byte ZBin = (byte)'A';

        /// <summary>Hex header marker.</summary>
        public const byte ZHex = (byte)'B';

        /// <summary>Binary header with CRC-32 marker.</summary>
        public const byte ZBin32 = (byte)'C';

        /// <summary>Subpacket terminator: end of frame, header expected next.</summary>
        public const byte ZCrcE = (byte)'h';

        /// <summary>Subpacket terminator: frame continues without acknowledgement.</summary>
        public const byte ZCrcG = (byte)'i';

        /// <summary>Subpacket terminator: frame continues, ZACK requested.</summary>
        public const byte ZCrcQ = (byte)'j';

        /// <summary>Subpacket terminator: end of frame, ZACK required.</summary>
        public const byte ZCrcW = (byte)'k';

        /// <summary>ZRINIT flag: the receiver can operate full duplex.</summary>
        public const uint CanFdx = 0x01;

        /// <summary>ZRINIT flag: the receiver can overlap disk and serial I/O.</summary>
        public const uint CanOvio = 0x02;

        /// <summary>ZRINIT flag: the receiver accepts CRC-32 binary frames.</summary>
        public const uint CanFc32 = 0x20;

        /// <summary>The residue left in a CRC-32 accumulator after a valid frame.</summary>
        private const uint Crc32Residue = 0xDEBB20E3;

        /// <summary>Result code representing a receive timeout.</summary>
        public const int Timeout = -1;

        /// <summary>Result code representing a garbled frame.</summary>
        public const int Error = -2;

        private const byte ZRub0 = (byte)'l';
        private const byte ZRub1 = (byte)'m';

        private readonly ITransferLink _link;
        private readonly TransferLinkReader _reader;
        private readonly List<byte> _txBuffer = new(2200);
        private int _cancelCount;
        private byte _lastSent;

        /// <summary>
        /// Initializes framing over the specified link.
        /// </summary>
        /// <param name="link">The byte channel to the remote system.</param>
        public ZmodemFraming(ITransferLink link)
        {
            _link = link;
            _reader = new TransferLinkReader(link);
        }

        /// <summary>
        /// Gets whether the most recently received binary header used CRC-32 framing. Data
        /// subpackets that follow a header always use the same CRC width.
        /// </summary>
        public bool ReceivedCrc32 { get; private set; }

        /// <summary>
        /// Gets the buffered reader so callers can perform raw reads (for example the
        /// trailing "OO" of a session).
        /// </summary>
        public TransferLinkReader Reader => _reader;

        /// <summary>
        /// Discards any unread input.
        /// </summary>
        public void Purge() => _reader.Purge();

        #region Writing

        /// <summary>
        /// Sends raw, unescaped bytes.
        /// </summary>
        public ValueTask WriteRawAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            return _link.WriteAsync(data, cancellationToken);
        }

        /// <summary>
        /// Sends a hex header, the format every ZMODEM session starts with because it
        /// survives 7-bit and XON/XOFF hostile paths.
        /// </summary>
        /// <param name="type">The frame type.</param>
        /// <param name="data">The position or flag payload (byte 0 low / byte 3 high).</param>
        /// <param name="cancellationToken">A token that aborts the write.</param>
        public async ValueTask WriteHexHeaderAsync(ZFrameType type, uint data, CancellationToken cancellationToken)
        {
            _txBuffer.Clear();
            _txBuffer.Add(ZPad);
            _txBuffer.Add(ZPad);
            _txBuffer.Add(Zdle);
            _txBuffer.Add(ZHex);

            ushort crc = 0;
            AppendHexByte((byte)type, ref crc);

            for (int i = 0; i < 4; i++)
            {
                AppendHexByte((byte)(data >> (8 * i)), ref crc);
            }

            AppendHexDigits((byte)(crc >> 8));
            AppendHexDigits((byte)crc);
            _txBuffer.Add(0x0D);
            _txBuffer.Add(0x0A);

            if (type != ZFrameType.ZFIN && type != ZFrameType.ZACK)
            {
                // XON releases a receiver that stopped the line with XOFF.
                _txBuffer.Add(0x11);
            }

            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a binary header using CRC-16 or CRC-32 framing.
        /// </summary>
        /// <param name="type">The frame type.</param>
        /// <param name="data">The position or flag payload (byte 0 low / byte 3 high).</param>
        /// <param name="crc32">Whether to use CRC-32 framing.</param>
        /// <param name="cancellationToken">A token that aborts the write.</param>
        public async ValueTask WriteBinaryHeaderAsync(ZFrameType type, uint data, bool crc32, CancellationToken cancellationToken)
        {
            _txBuffer.Clear();
            _txBuffer.Add(ZPad);
            _txBuffer.Add(Zdle);
            _txBuffer.Add(crc32 ? ZBin32 : ZBin);
            _lastSent = crc32 ? ZBin32 : ZBin;

            if (crc32)
            {
                uint crc = 0xFFFFFFFF;
                AppendEscaped((byte)type, ref crc);

                for (int i = 0; i < 4; i++)
                {
                    AppendEscaped((byte)(data >> (8 * i)), ref crc);
                }

                crc = ~crc;

                for (int i = 0; i < 4; i++)
                {
                    AppendEscapedNoCrc((byte)(crc >> (8 * i)));
                }
            }
            else
            {
                ushort crc = 0;
                AppendEscaped((byte)type, ref crc);

                for (int i = 0; i < 4; i++)
                {
                    AppendEscaped((byte)(data >> (8 * i)), ref crc);
                }

                AppendEscapedNoCrc((byte)(crc >> 8));
                AppendEscapedNoCrc((byte)crc);
            }

            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends one data subpacket terminated by the specified frame-end marker.
        /// </summary>
        /// <param name="data">The subpacket payload.</param>
        /// <param name="frameEnd">One of <see cref="ZCrcE"/>, <see cref="ZCrcG"/>, <see cref="ZCrcQ"/>, or <see cref="ZCrcW"/>.</param>
        /// <param name="crc32">Whether to use CRC-32 framing.</param>
        /// <param name="cancellationToken">A token that aborts the write.</param>
        public async ValueTask WriteDataSubpacketAsync(ReadOnlyMemory<byte> data, byte frameEnd, bool crc32, CancellationToken cancellationToken)
        {
            _txBuffer.Clear();

            if (crc32)
            {
                uint crc = 0xFFFFFFFF;

                foreach (byte value in data.Span)
                {
                    AppendEscaped(value, ref crc);
                }

                crc = TransferCrc.UpdateCrc32(crc, frameEnd);
                _txBuffer.Add(Zdle);
                _txBuffer.Add(frameEnd);
                _lastSent = frameEnd;
                crc = ~crc;

                for (int i = 0; i < 4; i++)
                {
                    AppendEscapedNoCrc((byte)(crc >> (8 * i)));
                }
            }
            else
            {
                ushort crc = 0;

                foreach (byte value in data.Span)
                {
                    AppendEscaped(value, ref crc);
                }

                crc = TransferCrc.UpdateCrc16(crc, frameEnd);
                _txBuffer.Add(Zdle);
                _txBuffer.Add(frameEnd);
                _lastSent = frameEnd;
                AppendEscapedNoCrc((byte)(crc >> 8));
                AppendEscapedNoCrc((byte)crc);
            }

            await FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private void AppendHexByte(byte value, ref ushort crc)
        {
            crc = TransferCrc.UpdateCrc16(crc, value);
            AppendHexDigits(value);
        }

        private void AppendHexDigits(byte value)
        {
            const string digits = "0123456789abcdef";
            _txBuffer.Add((byte)digits[value >> 4]);
            _txBuffer.Add((byte)digits[value & 0x0F]);
        }

        private void AppendEscaped(byte value, ref ushort crc)
        {
            crc = TransferCrc.UpdateCrc16(crc, value);
            AppendEscapedNoCrc(value);
        }

        private void AppendEscaped(byte value, ref uint crc)
        {
            crc = TransferCrc.UpdateCrc32(crc, value);
            AppendEscapedNoCrc(value);
        }

        private void AppendEscapedNoCrc(byte value)
        {
            bool escape = value switch
            {
                Zdle or 0x10 or 0x11 or 0x13 or 0x90 or 0x91 or 0x93 => true,
                // Escape CR after '@' so "@...CR" can never look like a Telnet command.
                0x0D or 0x8D when (_lastSent & 0x7F) == '@' => true,
                _ => false
            };

            if (escape)
            {
                _txBuffer.Add(Zdle);
                value ^= 0x40;
            }

            _txBuffer.Add(value);
            _lastSent = value;
        }

        private async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            await _link.WriteAsync(_txBuffer.ToArray(), cancellationToken).ConfigureAwait(false);
            _txBuffer.Clear();
        }

        #endregion

        #region Reading

        /// <summary>
        /// Scans the input for the next well formed header.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for a header.</param>
        /// <param name="cancellationToken">A token that aborts the transfer.</param>
        /// <returns>
        /// The header type and payload, or a null type with <see cref="Timeout"/>/<see cref="Error"/>
        /// semantics conveyed via <c>Status</c>.
        /// </returns>
        /// <exception cref="TransferException">The remote side sent a cancel sequence.</exception>
        public async Task<(int Status, ZFrameType Type, uint Data)> ReadHeaderAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            int padCount = 0;

            while (true)
            {
                int value = await _reader.ReadByteAsync(timeout, cancellationToken).ConfigureAwait(false);

                if (value < 0)
                {
                    return (Timeout, default, 0);
                }

                if (value == Zdle)
                {
                    if (++_cancelCount >= 5)
                    {
                        throw new TransferException("The remote system canceled the transfer.");
                    }

                    if (padCount > 0)
                    {
                        int format = await _reader.ReadByteAsync(timeout, cancellationToken).ConfigureAwait(false);
                        _cancelCount = 0;

                        switch (format)
                        {
                            case ZHex:
                                return await ReadHexHeaderBodyAsync(cancellationToken).ConfigureAwait(false);
                            case ZBin:
                                return await ReadBinaryHeaderBodyAsync(crc32: false, cancellationToken).ConfigureAwait(false);
                            case ZBin32:
                                return await ReadBinaryHeaderBodyAsync(crc32: true, cancellationToken).ConfigureAwait(false);
                            case Zdle:
                                _cancelCount = 2;
                                padCount = 0;
                                continue;
                            default:
                                padCount = 0;
                                continue;
                        }
                    }

                    padCount = 0;
                    continue;
                }

                _cancelCount = 0;
                padCount = value == ZPad ? padCount + 1 : 0;
            }
        }

        private async Task<(int Status, ZFrameType Type, uint Data)> ReadHexHeaderBodyAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[7];

            for (int i = 0; i < 7; i++)
            {
                int high = await ReadHexDigitAsync(cancellationToken).ConfigureAwait(false);
                int low = await ReadHexDigitAsync(cancellationToken).ConfigureAwait(false);

                if (high < 0 || low < 0)
                {
                    return (Error, default, 0);
                }

                buffer[i] = (byte)((high << 4) | low);
            }

            ushort crc = 0;

            for (int i = 0; i < 7; i++)
            {
                crc = TransferCrc.UpdateCrc16(crc, buffer[i]);
            }

            if (crc != 0)
            {
                return (Error, default, 0);
            }

            // Consume the line ending (CR LF, possibly with the high bit set) and XON.
            for (int i = 0; i < 3; i++)
            {
                int trailing = await _reader.ReadByteAsync(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);

                if (trailing < 0 || (trailing & 0x7F) != 0x0D && (trailing & 0x7F) != 0x0A && trailing != 0x11)
                {
                    break;
                }
            }

            ReceivedCrc32 = false;
            return (0, (ZFrameType)buffer[0], ReadUInt32(buffer.AsSpan(1)));
        }

        private async Task<(int Status, ZFrameType Type, uint Data)> ReadBinaryHeaderBodyAsync(bool crc32, CancellationToken cancellationToken)
        {
            int length = crc32 ? 9 : 7;
            var buffer = new byte[length];

            for (int i = 0; i < length; i++)
            {
                int value = await ReadZdleByteAsync(cancellationToken).ConfigureAwait(false);

                if (value < 0)
                {
                    return (Error, default, 0);
                }

                buffer[i] = (byte)value;
            }

            if (crc32)
            {
                uint crc = 0xFFFFFFFF;

                foreach (byte value in buffer)
                {
                    crc = TransferCrc.UpdateCrc32(crc, value);
                }

                if (crc != Crc32Residue)
                {
                    return (Error, default, 0);
                }
            }
            else
            {
                ushort crc = 0;

                foreach (byte value in buffer)
                {
                    crc = TransferCrc.UpdateCrc16(crc, value);
                }

                if (crc != 0)
                {
                    return (Error, default, 0);
                }
            }

            ReceivedCrc32 = crc32;
            return (0, (ZFrameType)buffer[0], ReadUInt32(buffer.AsSpan(1)));
        }

        /// <summary>
        /// Reads one data subpacket that follows a binary header.
        /// </summary>
        /// <param name="destination">The buffer that receives the payload (at least 1024 bytes).</param>
        /// <param name="cancellationToken">A token that aborts the transfer.</param>
        /// <returns>
        /// The payload length and the frame-end marker, or a negative status
        /// (<see cref="Timeout"/> or <see cref="Error"/>).
        /// </returns>
        /// <exception cref="TransferException">The remote side sent a cancel sequence.</exception>
        public async Task<(int Length, byte FrameEnd)> ReadDataSubpacketAsync(byte[] destination, CancellationToken cancellationToken)
        {
            bool crc32 = ReceivedCrc32;
            uint crc32Value = 0xFFFFFFFF;
            ushort crc16Value = 0;
            int length = 0;

            while (true)
            {
                int value = await ReadZdleByteAsync(cancellationToken).ConfigureAwait(false);

                if (value == Timeout)
                {
                    return (Timeout, 0);
                }

                if (value == Error)
                {
                    return (Error, 0);
                }

                if (value > 0xFF)
                {
                    byte frameEnd = (byte)(value & 0xFF);

                    if (crc32)
                    {
                        crc32Value = TransferCrc.UpdateCrc32(crc32Value, frameEnd);

                        for (int i = 0; i < 4; i++)
                        {
                            int crcByte = await ReadZdleByteAsync(cancellationToken).ConfigureAwait(false);

                            if (crcByte < 0 || crcByte > 0xFF)
                            {
                                return (Error, 0);
                            }

                            crc32Value = TransferCrc.UpdateCrc32(crc32Value, (byte)crcByte);
                        }

                        return crc32Value == Crc32Residue ? (length, frameEnd) : (Error, (byte)0);
                    }

                    crc16Value = TransferCrc.UpdateCrc16(crc16Value, frameEnd);

                    for (int i = 0; i < 2; i++)
                    {
                        int crcByte = await ReadZdleByteAsync(cancellationToken).ConfigureAwait(false);

                        if (crcByte < 0 || crcByte > 0xFF)
                        {
                            return (Error, 0);
                        }

                        crc16Value = TransferCrc.UpdateCrc16(crc16Value, (byte)crcByte);
                    }

                    return crc16Value == 0 ? (length, frameEnd) : (Error, (byte)0);
                }

                if (length >= destination.Length)
                {
                    return (Error, 0);
                }

                destination[length++] = (byte)value;

                if (crc32)
                {
                    crc32Value = TransferCrc.UpdateCrc32(crc32Value, (byte)value);
                }
                else
                {
                    crc16Value = TransferCrc.UpdateCrc16(crc16Value, (byte)value);
                }
            }
        }

        /// <summary>
        /// Reads one ZDLE-decoded byte. Returns the byte value, a frame-end marker encoded
        /// as <c>0x100 | marker</c>, or a negative status code.
        /// </summary>
        private async ValueTask<int> ReadZdleByteAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                int value = await _reader.ReadByteAsync(XymodemConstants.ByteTimeout, cancellationToken).ConfigureAwait(false);

                switch (value)
                {
                    case -1:
                        return Timeout;
                    case 0x11 or 0x13 or 0x91 or 0x93:
                        // XON/XOFF noise injected by modems or Telnet stacks is ignored.
                        continue;
                    case Zdle:
                        break;
                    default:
                        _cancelCount = 0;
                        return value;
                }

                // A ZDLE escape: interpret the next character.
                while (true)
                {
                    value = await _reader.ReadByteAsync(XymodemConstants.ByteTimeout, cancellationToken).ConfigureAwait(false);

                    switch (value)
                    {
                        case -1:
                            return Timeout;
                        case 0x11 or 0x13 or 0x91 or 0x93:
                            continue;
                        case Zdle:
                            if (++_cancelCount >= 5)
                            {
                                throw new TransferException("The remote system canceled the transfer.");
                            }

                            continue;
                        case ZCrcE or ZCrcG or ZCrcQ or ZCrcW:
                            _cancelCount = 0;
                            return 0x100 | value;
                        case ZRub0:
                            _cancelCount = 0;
                            return 0x7F;
                        case ZRub1:
                            _cancelCount = 0;
                            return 0xFF;
                        default:
                            _cancelCount = 0;
                            return (value & 0x60) == 0x40 ? value ^ 0x40 : Error;
                    }
                }
            }
        }

        private async ValueTask<int> ReadHexDigitAsync(CancellationToken cancellationToken)
        {
            int value = await _reader.ReadByteAsync(XymodemConstants.ByteTimeout, cancellationToken).ConfigureAwait(false);

            return value switch
            {
                >= '0' and <= '9' => value - '0',
                >= 'a' and <= 'f' => value - 'a' + 10,
                >= 'A' and <= 'F' => value - 'A' + 10,
                _ => -1
            };
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> bytes)
        {
            return bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24);
        }

        #endregion
    }
}
