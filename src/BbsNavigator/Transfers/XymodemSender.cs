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
    /// Sends one file via XMODEM or a batch of files via YMODEM.
    /// </summary>
    public sealed class XymodemSender
    {
        private readonly TransferProtocol _protocol;

        /// <summary>
        /// Initializes a sender for the specified protocol variant.
        /// </summary>
        /// <param name="protocol">An XMODEM or YMODEM protocol variant.</param>
        /// <exception cref="ArgumentOutOfRangeException">The protocol is not an XMODEM/YMODEM variant.</exception>
        public XymodemSender(TransferProtocol protocol)
        {
            if (protocol == TransferProtocol.Zmodem)
            {
                throw new ArgumentOutOfRangeException(nameof(protocol), "Use ZmodemSender for ZMODEM transfers.");
            }

            _protocol = protocol;
        }

        private int BlockSize => _protocol is TransferProtocol.Ymodem or TransferProtocol.Xmodem1K ? 1024 : 128;

        /// <summary>
        /// Sends the specified files. XMODEM variants send only the first file because the
        /// protocol has no batch support.
        /// </summary>
        /// <param name="link">The byte channel to the remote system.</param>
        /// <param name="filePaths">The local files to send.</param>
        /// <param name="progress">An optional progress sink.</param>
        /// <param name="cancellationToken">A token that aborts the transfer.</param>
        /// <returns>A summary of the completed session.</returns>
        /// <exception cref="TransferException">The remote side canceled or the protocol failed.</exception>
        /// <exception cref="OperationCanceledException">The transfer was canceled locally.</exception>
        public async Task<TransferResult> SendAsync(
            ITransferLink link,
            IReadOnlyList<string> filePaths,
            IProgress<TransferSnapshot>? progress,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(filePaths);

            if (filePaths.Count == 0)
            {
                throw new ArgumentException("At least one file is required.", nameof(filePaths));
            }

            var reader = new TransferLinkReader(link);
            var stopwatch = Stopwatch.StartNew();
            var sent = new List<string>();
            long totalBytes = 0;

            try
            {
                if (_protocol == TransferProtocol.Ymodem)
                {
                    for (int i = 0; i < filePaths.Count; i++)
                    {
                        totalBytes += await SendYmodemFileAsync(link, reader, filePaths[i], i + 1, progress, cancellationToken).ConfigureAwait(false);
                        sent.Add(Path.GetFileName(filePaths[i]));
                    }

                    // The empty header block tells the receiver the batch is complete.
                    await WaitForStartAsync(reader, requireCrc: true, cancellationToken).ConfigureAwait(false);
                    await SendBlockWithRetryAsync(link, reader, 0, new byte[128], useCrc: true, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    bool useCrc = await WaitForStartAsync(reader, requireCrc: false, cancellationToken).ConfigureAwait(false);
                    totalBytes = await SendDataPhaseAsync(link, reader, filePaths[0], 1, useCrc, progress, cancellationToken).ConfigureAwait(false);
                    sent.Add(Path.GetFileName(filePaths[0]));
                }
            }
            catch
            {
                await TrySendCancelAsync(link).ConfigureAwait(false);
                throw;
            }

            return new TransferResult(sent, totalBytes, stopwatch.Elapsed);
        }

        /// <summary>
        /// Sends the YMODEM header block for one file and then its data phase.
        /// </summary>
        private async Task<long> SendYmodemFileAsync(
            ITransferLink link,
            TransferLinkReader reader,
            string filePath,
            int fileIndex,
            IProgress<TransferSnapshot>? progress,
            CancellationToken cancellationToken)
        {
            var info = new FileInfo(filePath);

            if (!info.Exists)
            {
                throw new TransferException($"The file '{filePath}' was not found.");
            }

            await WaitForStartAsync(reader, requireCrc: true, cancellationToken).ConfigureAwait(false);

            var header = new byte[128];
            byte[] name = Encoding.ASCII.GetBytes(Path.GetFileName(filePath));
            byte[] size = Encoding.ASCII.GetBytes(info.Length.ToString());

            if (name.Length + size.Length + 2 > header.Length)
            {
                header = new byte[1024];
            }

            name.CopyTo(header, 0);
            size.CopyTo(header, name.Length + 1);

            await SendBlockWithRetryAsync(link, reader, 0, header, useCrc: true, cancellationToken).ConfigureAwait(false);
            await WaitForStartAsync(reader, requireCrc: true, cancellationToken).ConfigureAwait(false);

            return await SendDataPhaseAsync(link, reader, filePath, fileIndex, useCrc: true, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Streams the file's data blocks and completes the EOT handshake.
        /// </summary>
        private async Task<long> SendDataPhaseAsync(
            ITransferLink link,
            TransferLinkReader reader,
            string filePath,
            int fileIndex,
            bool useCrc,
            IProgress<TransferSnapshot>? progress,
            CancellationToken cancellationToken)
        {
            string fileName = Path.GetFileName(filePath);
            long sentBytes = 0;
            byte blockNumber = 1;

            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
            long fileSize = stream.Length;
            var block = new byte[BlockSize];

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int read = await ReadFullBlockAsync(stream, block, cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                block.AsSpan(read).Fill(Sub);
                await SendBlockWithRetryAsync(link, reader, blockNumber, block, useCrc, cancellationToken).ConfigureAwait(false);
                blockNumber = unchecked((byte)(blockNumber + 1));
                sentBytes += read;
                progress?.Report(new TransferSnapshot(fileName, sentBytes, fileSize, fileIndex, "Sending"));
            }

            await CompleteEotAsync(link, reader, cancellationToken).ConfigureAwait(false);
            return sentBytes;
        }

        /// <summary>
        /// Waits for the receiver's start byte and returns whether CRC mode was requested.
        /// </summary>
        private static async Task<bool> WaitForStartAsync(
            TransferLinkReader reader, bool requireCrc, CancellationToken cancellationToken)
        {
            int cancelCount = 0;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                int value = await reader.ReadByteAsync(HandshakeTimeout, cancellationToken).ConfigureAwait(false);

                switch (value)
                {
                    case CrcStart:
                        return true;
                    case Nak when !requireCrc:
                        return false;
                    case Can when ++cancelCount >= 2:
                        throw new TransferException("The remote system canceled the transfer.");
                    case -1:
                        continue;
                }
            }

            throw new TransferException("Timed out waiting for the receiver to start the transfer.");
        }

        /// <summary>
        /// Sends one framed block and retries until it is acknowledged.
        /// </summary>
        private static async Task SendBlockWithRetryAsync(
            ITransferLink link,
            TransferLinkReader reader,
            byte blockNumber,
            byte[] data,
            bool useCrc,
            CancellationToken cancellationToken)
        {
            int frameLength = 3 + data.Length + (useCrc ? 2 : 1);
            var frame = new byte[frameLength];
            frame[0] = data.Length == 1024 ? Stx : Soh;
            frame[1] = blockNumber;
            frame[2] = unchecked((byte)~blockNumber);
            data.CopyTo(frame, 3);

            if (useCrc)
            {
                ushort crc = TransferCrc.ComputeCrc16(data);
                frame[^2] = (byte)(crc >> 8);
                frame[^1] = (byte)crc;
            }
            else
            {
                frame[^1] = TransferCrc.ComputeChecksum(data);
            }

            int cancelCount = 0;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await link.WriteAsync(frame, cancellationToken).ConfigureAwait(false);

                while (true)
                {
                    int response = await reader.ReadByteAsync(HandshakeTimeout, cancellationToken).ConfigureAwait(false);

                    switch (response)
                    {
                        case Ack:
                            return;
                        case Can when ++cancelCount >= 2:
                            throw new TransferException("The remote system canceled the transfer.");
                        case Can:
                            continue;
                        case CrcStart:
                            // A stray start byte can arrive if the receiver re-requested
                            // just as this block went out; ignore it and keep waiting.
                            continue;
                        default:
                            // NAK, garbage, or timeout: resend the block.
                            break;
                    }

                    break;
                }
            }

            throw new TransferException($"Block {blockNumber} was not acknowledged after {MaxRetries} attempts.");
        }

        /// <summary>
        /// Sends EOT until the receiver acknowledges the end of the file.
        /// </summary>
        private static async Task CompleteEotAsync(
            ITransferLink link, TransferLinkReader reader, CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await link.WriteAsync(new byte[] { Eot }, cancellationToken).ConfigureAwait(false);
                int response = await reader.ReadByteAsync(HandshakeTimeout, cancellationToken).ConfigureAwait(false);

                if (response == Ack)
                {
                    return;
                }
            }

            throw new TransferException("The receiver did not acknowledge the end of the file.");
        }

        private static async Task<int> ReadFullBlockAsync(FileStream stream, byte[] block, CancellationToken cancellationToken)
        {
            int total = 0;

            while (total < block.Length)
            {
                int read = await stream.ReadAsync(block.AsMemory(total), cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            return total;
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
    }
}
