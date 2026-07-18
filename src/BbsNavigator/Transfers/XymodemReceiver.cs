/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Diagnostics;
using System.IO;
using System.Text;
using static BbsNavigator.Transfers.XymodemConstants;

namespace BbsNavigator.Transfers
{
    /// <summary>
    /// Receives one file via XMODEM or a batch of files via YMODEM.
    /// </summary>
    public sealed class XymodemReceiver
    {
        private readonly TransferProtocol _protocol;

        /// <summary>
        /// Initializes a receiver for the specified protocol variant.
        /// </summary>
        /// <param name="protocol">An XMODEM or YMODEM protocol variant.</param>
        /// <exception cref="ArgumentOutOfRangeException">The protocol is not an XMODEM/YMODEM variant.</exception>
        public XymodemReceiver(TransferProtocol protocol)
        {
            if (protocol == TransferProtocol.Zmodem)
            {
                throw new ArgumentOutOfRangeException(nameof(protocol), "Use ZmodemReceiver for ZMODEM transfers.");
            }

            _protocol = protocol;
        }

        private bool UseCrc => _protocol != TransferProtocol.Xmodem;

        private enum PacketKind
        {
            Block,
            Eot,
            Timeout,
            Cancel,
            Junk
        }

        /// <summary>
        /// Receives one file (XMODEM) or a batch of files (YMODEM) into a folder.
        /// </summary>
        /// <param name="link">The byte channel to the remote system.</param>
        /// <param name="destinationFolder">The folder that receives the files.</param>
        /// <param name="fallbackFileName">The file name used for XMODEM, which does not transmit one.</param>
        /// <param name="progress">An optional progress sink.</param>
        /// <param name="cancellationToken">A token that aborts the transfer.</param>
        /// <returns>A summary of the completed session.</returns>
        /// <exception cref="TransferException">The remote side canceled or the protocol failed.</exception>
        /// <exception cref="OperationCanceledException">The transfer was canceled locally.</exception>
        public async Task<TransferResult> ReceiveAsync(
            ITransferLink link,
            string destinationFolder,
            string fallbackFileName,
            IProgress<TransferSnapshot>? progress,
            CancellationToken cancellationToken)
        {
            var reader = new TransferLinkReader(link);
            var stopwatch = Stopwatch.StartNew();
            var files = new List<string>();
            long totalBytes = 0;

            Directory.CreateDirectory(destinationFolder);

            try
            {
                if (_protocol == TransferProtocol.Ymodem)
                {
                    int fileIndex = 0;

                    while (true)
                    {
                        (string? name, long size) = await ReceiveBlockZeroAsync(link, reader, cancellationToken).ConfigureAwait(false);

                        if (string.IsNullOrEmpty(name))
                        {
                            break;
                        }

                        fileIndex++;
                        string path = MakeUniquePath(destinationFolder, name);
                        long written = await ReceiveFileAsync(link, reader, path, size, fileIndex, progress, cancellationToken).ConfigureAwait(false);
                        files.Add(Path.GetFileName(path));
                        totalBytes += written;
                    }
                }
                else
                {
                    string path = MakeUniquePath(destinationFolder, fallbackFileName);
                    totalBytes = await ReceiveFileAsync(link, reader, path, -1, 1, progress, cancellationToken).ConfigureAwait(false);
                    files.Add(Path.GetFileName(path));
                }
            }
            catch
            {
                await TrySendCancelAsync(link).ConfigureAwait(false);
                throw;
            }

            return new TransferResult(files, totalBytes, stopwatch.Elapsed);
        }

        /// <summary>
        /// Performs the YMODEM header-block handshake and returns the announced file
        /// name and size, or an empty name when the batch is complete.
        /// </summary>
        private async Task<(string? Name, long Size)> ReceiveBlockZeroAsync(
            ITransferLink link, TransferLinkReader reader, CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SendByteAsync(link, CrcStart, cancellationToken).ConfigureAwait(false);

                (PacketKind kind, byte number, byte[] data) = await ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);

                if (kind == PacketKind.Cancel)
                {
                    throw new TransferException("The remote system canceled the transfer.");
                }

                if (kind != PacketKind.Block || number != 0)
                {
                    reader.Purge();
                    continue;
                }

                await SendByteAsync(link, Ack, cancellationToken).ConfigureAwait(false);

                int nameEnd = Array.IndexOf(data, (byte)0);

                if (nameEnd <= 0)
                {
                    // An empty header block terminates the YMODEM batch.
                    return (null, 0);
                }

                string name = Path.GetFileName(Encoding.ASCII.GetString(data, 0, nameEnd));
                long size = -1;
                int infoStart = nameEnd + 1;
                int infoEnd = Array.IndexOf(data, (byte)0, infoStart);

                if (infoEnd > infoStart)
                {
                    string info = Encoding.ASCII.GetString(data, infoStart, infoEnd - infoStart);
                    string[] parts = info.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 0 && long.TryParse(parts[0], out long parsedSize))
                    {
                        size = parsedSize;
                    }
                }

                return (name, size);
            }

            throw new TransferException("Timed out waiting for the YMODEM file header.");
        }

        /// <summary>
        /// Receives the data phase of one file, honoring the announced size when known.
        /// </summary>
        private async Task<long> ReceiveFileAsync(
            ITransferLink link,
            TransferLinkReader reader,
            string path,
            long announcedSize,
            int fileIndex,
            IProgress<TransferSnapshot>? progress,
            CancellationToken cancellationToken)
        {
            string fileName = Path.GetFileName(path);
            long written = 0;
            byte expectedBlock = 1;
            bool receivedFirstBlock = false;
            bool sawEot = false;
            int retries = 0;

            await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);

            try
            {
                // Ask the sender to start (or continue after the YMODEM header ACK).
                await SendByteAsync(link, UseCrc ? CrcStart : Nak, cancellationToken).ConfigureAwait(false);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    (PacketKind kind, byte number, byte[] data) = await ReadPacketAsync(reader, cancellationToken).ConfigureAwait(false);

                    switch (kind)
                    {
                        case PacketKind.Cancel:
                            throw new TransferException("The remote system canceled the transfer.");

                        case PacketKind.Eot:
                            // Per the YMODEM spec the first EOT is NAKed and the retransmit
                            // is acknowledged, which guards against a corrupted final block.
                            if (!sawEot)
                            {
                                sawEot = true;
                                await SendByteAsync(link, Nak, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            await SendByteAsync(link, Ack, cancellationToken).ConfigureAwait(false);
                            return written;

                        case PacketKind.Block when number == unchecked((byte)(expectedBlock - 1)):
                            // A retransmitted block that was already stored (or a re-sent YMODEM
                            // header whose ACK was lost); acknowledge it and move on.
                            await SendByteAsync(link, Ack, cancellationToken).ConfigureAwait(false);
                            continue;

                        case PacketKind.Block when number == expectedBlock:
                            sawEot = false;
                            receivedFirstBlock = true;
                            retries = 0;
                            int writeLength = data.Length;

                            if (announcedSize >= 0)
                            {
                                writeLength = (int)Math.Min(writeLength, announcedSize - written);
                            }

                            if (writeLength > 0)
                            {
                                await stream.WriteAsync(data.AsMemory(0, writeLength), cancellationToken).ConfigureAwait(false);
                                written += writeLength;
                            }

                            expectedBlock = unchecked((byte)(expectedBlock + 1));
                            await SendByteAsync(link, Ack, cancellationToken).ConfigureAwait(false);
                            progress?.Report(new TransferSnapshot(fileName, written, announcedSize, fileIndex, "Receiving"));
                            continue;

                        case PacketKind.Block:
                            // A block far out of sequence is unrecoverable in this protocol.
                            throw new TransferException($"Block sequence error (expected {expectedBlock}, received {number}).");

                        default:
                            if (++retries >= MaxRetries)
                            {
                                throw new TransferException("Too many consecutive errors while receiving.");
                            }

                            reader.Purge();
                            await SendByteAsync(link, receivedFirstBlock ? Nak : (UseCrc ? CrcStart : Nak), cancellationToken).ConfigureAwait(false);
                            continue;
                    }
                }
            }
            finally
            {
                await stream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads one packet (block, EOT, or cancel) and validates its integrity.
        /// </summary>
        private async Task<(PacketKind Kind, byte Number, byte[] Data)> ReadPacketAsync(
            TransferLinkReader reader, CancellationToken cancellationToken)
        {
            int first = await reader.ReadByteAsync(HandshakeTimeout, cancellationToken).ConfigureAwait(false);

            switch (first)
            {
                case -1:
                    return (PacketKind.Timeout, 0, []);
                case Eot:
                    return (PacketKind.Eot, 0, []);
                case Can:
                    int second = await reader.ReadByteAsync(ByteTimeout, cancellationToken).ConfigureAwait(false);
                    return second == Can ? (PacketKind.Cancel, 0, []) : (PacketKind.Junk, 0, []);
                case Soh:
                case Stx:
                    break;
                default:
                    return (PacketKind.Junk, 0, []);
            }

            int blockSize = first == Stx ? 1024 : 128;
            int number = await reader.ReadByteAsync(ByteTimeout, cancellationToken).ConfigureAwait(false);
            int complement = await reader.ReadByteAsync(ByteTimeout, cancellationToken).ConfigureAwait(false);

            if (number < 0 || complement < 0 || number != (byte)~complement)
            {
                return (PacketKind.Junk, 0, []);
            }

            var data = new byte[blockSize];

            for (int i = 0; i < blockSize; i++)
            {
                int value = await reader.ReadByteAsync(ByteTimeout, cancellationToken).ConfigureAwait(false);

                if (value < 0)
                {
                    return (PacketKind.Junk, 0, []);
                }

                data[i] = (byte)value;
            }

            if (UseCrc)
            {
                int crcHigh = await reader.ReadByteAsync(ByteTimeout, cancellationToken).ConfigureAwait(false);
                int crcLow = await reader.ReadByteAsync(ByteTimeout, cancellationToken).ConfigureAwait(false);

                if (crcHigh < 0 || crcLow < 0 ||
                    TransferCrc.ComputeCrc16(data) != (ushort)((crcHigh << 8) | crcLow))
                {
                    return (PacketKind.Junk, 0, []);
                }
            }
            else
            {
                int checksum = await reader.ReadByteAsync(ByteTimeout, cancellationToken).ConfigureAwait(false);

                if (checksum < 0 || TransferCrc.ComputeChecksum(data) != checksum)
                {
                    return (PacketKind.Junk, 0, []);
                }
            }

            return (PacketKind.Block, (byte)number, data);
        }

        private static async ValueTask SendByteAsync(ITransferLink link, byte value, CancellationToken cancellationToken)
        {
            await link.WriteAsync(new[] { value }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TrySendCancelAsync(ITransferLink link)
        {
            try
            {
                await link.WriteAsync(new byte[] { Can, Can, Can }, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // The link is already unusable; the cancel is best effort only.
            }
        }

        /// <summary>
        /// Returns a path in <paramref name="folder"/> that does not collide with an existing file.
        /// </summary>
        internal static string MakeUniquePath(string folder, string fileName)
        {
            fileName = string.IsNullOrWhiteSpace(fileName) ? "download.bin" : Path.GetFileName(fileName);

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalid, '_');
            }

            string candidate = Path.Combine(folder, fileName);
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            for (int i = 1; File.Exists(candidate); i++)
            {
                candidate = Path.Combine(folder, $"{baseName} ({i}){extension}");
            }

            return candidate;
        }
    }
}
