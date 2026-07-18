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

namespace BbsNavigator.Transfers
{
    /// <summary>
    /// Sends a batch of files to a remote ZMODEM receiver (the classic <c>rz</c> flow),
    /// streaming ZCRCG subpackets and honoring ZRPOS rewind requests.
    /// </summary>
    public sealed class ZmodemSender
    {
        private const int SubpacketSize = 1024;
        private const int MaxErrors = 20;
        private static readonly TimeSpan _headerTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _interruptTimeout = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Sends the specified files and completes the session handshake.
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

            var framing = new ZmodemFraming(link);
            var stopwatch = Stopwatch.StartNew();
            var sent = new List<string>();
            long totalBytes = 0;

            try
            {
                uint receiverFlags = await StartSessionAsync(framing, cancellationToken).ConfigureAwait(false);
                bool useCrc32 = (receiverFlags & ZmodemFraming.CanFc32) != 0;

                for (int i = 0; i < filePaths.Count; i++)
                {
                    long bytes = await SendFileAsync(framing, filePaths[i], i + 1, useCrc32, progress, cancellationToken).ConfigureAwait(false);

                    if (bytes >= 0)
                    {
                        totalBytes += bytes;
                        sent.Add(Path.GetFileName(filePaths[i]));
                    }
                }

                await FinishSessionAsync(framing, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await TrySendCancelAsync(link).ConfigureAwait(false);
                throw;
            }

            return new TransferResult(sent, totalBytes, stopwatch.Elapsed);
        }

        /// <summary>
        /// Announces the session and waits for the receiver's ZRINIT capabilities.
        /// </summary>
        /// <returns>The receiver's ZRINIT flag byte (ZF0).</returns>
        private static async Task<uint> StartSessionAsync(ZmodemFraming framing, CancellationToken cancellationToken)
        {
            // "rz\r" nudges a remote shell into starting its receiver; a BBS that is
            // already waiting in receive mode simply discards it.
            await framing.WriteRawAsync(Encoding.ASCII.GetBytes("rz\r"), cancellationToken).ConfigureAwait(false);
            await framing.WriteHexHeaderAsync(ZFrameType.ZRQINIT, 0, cancellationToken).ConfigureAwait(false);

            for (int attempt = 0; attempt < MaxErrors; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                (int status, ZFrameType type, uint data) = await framing.ReadHeaderAsync(_headerTimeout, cancellationToken).ConfigureAwait(false);

                if (status < 0)
                {
                    await framing.WriteHexHeaderAsync(ZFrameType.ZRQINIT, 0, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                switch (type)
                {
                    case ZFrameType.ZRINIT:
                        // Drain duplicate ZRINITs (the receiver answers both its own
                        // startup and this side's ZRQINIT) so the first ZFILE is not
                        // answered by a stale header.
                        while (framing.Reader.DataAvailable)
                        {
                            (int drainStatus, ZFrameType drained, _) = await framing.ReadHeaderAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);

                            if (drainStatus < 0 || drained != ZFrameType.ZRINIT)
                            {
                                break;
                            }
                        }

                        return data >> 24;
                    case ZFrameType.ZCHALLENGE:
                        await framing.WriteHexHeaderAsync(ZFrameType.ZACK, data, cancellationToken).ConfigureAwait(false);
                        continue;
                    case ZFrameType.ZRQINIT:
                        // Both sides think they are sending; that cannot succeed.
                        throw new TransferException("The remote system is also trying to send.");
                    case ZFrameType.ZFIN or ZFrameType.ZABORT or ZFrameType.ZCAN:
                        throw new TransferException("The remote system refused the transfer.");
                    default:
                        continue;
                }
            }

            throw new TransferException("Timed out waiting for the receiver to answer ZRQINIT.");
        }

        /// <summary>
        /// Sends one file, negotiating the start position and honoring rewind requests.
        /// </summary>
        /// <returns>The number of bytes sent, or -1 when the receiver skipped the file.</returns>
        private static async Task<long> SendFileAsync(
            ZmodemFraming framing,
            string filePath,
            int fileIndex,
            bool useCrc32,
            IProgress<TransferSnapshot>? progress,
            CancellationToken cancellationToken)
        {
            var info = new FileInfo(filePath);

            if (!info.Exists)
            {
                throw new TransferException($"The file '{filePath}' was not found.");
            }

            string fileName = Path.GetFileName(filePath);
            byte[] fileInfoPacket = BuildFileInfoPacket(fileName, info.Length);
            long startPosition = -1;

            for (int attempt = 0; attempt < MaxErrors && startPosition < 0; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await framing.WriteBinaryHeaderAsync(ZFrameType.ZFILE, 0, useCrc32, cancellationToken).ConfigureAwait(false);
                await framing.WriteDataSubpacketAsync(fileInfoPacket, ZmodemFraming.ZCrcW, useCrc32, cancellationToken).ConfigureAwait(false);

                (int status, ZFrameType type, uint data) = await framing.ReadHeaderAsync(_headerTimeout, cancellationToken).ConfigureAwait(false);

                if (status < 0)
                {
                    continue;
                }

                switch (type)
                {
                    case ZFrameType.ZRPOS:
                        startPosition = data;
                        break;
                    case ZFrameType.ZSKIP:
                        return -1;
                    case ZFrameType.ZRINIT:
                    case ZFrameType.ZNAK:
                        continue;
                    case ZFrameType.ZFIN or ZFrameType.ZABORT or ZFrameType.ZCAN:
                        throw new TransferException("The remote system aborted the transfer.");
                    default:
                        continue;
                }
            }

            if (startPosition < 0)
            {
                throw new TransferException($"The receiver never accepted the file '{fileName}'.");
            }

            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
            long position = Math.Min(startPosition, info.Length);
            var buffer = new byte[SubpacketSize];
            int errors = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                stream.Seek(position, SeekOrigin.Begin);
                await framing.WriteBinaryHeaderAsync(ZFrameType.ZDATA, (uint)position, useCrc32, cancellationToken).ConfigureAwait(false);

                long? rewind = null;
                bool skipped = false;

                while (rewind == null && !skipped)
                {
                    int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    bool last = position + read >= info.Length || read == 0;
                    byte frameEnd = last ? ZmodemFraming.ZCrcE : ZmodemFraming.ZCrcG;

                    await framing.WriteDataSubpacketAsync(buffer.AsMemory(0, read), frameEnd, useCrc32, cancellationToken).ConfigureAwait(false);
                    position += read;
                    progress?.Report(new TransferSnapshot(fileName, position, info.Length, fileIndex, "Sending"));

                    // A waiting header mid-stream is a receiver interrupt (usually ZRPOS
                    // after a CRC error); react before wasting more bandwidth.
                    while (framing.Reader.DataAvailable && rewind == null && !skipped)
                    {
                        (int status, ZFrameType type, uint data) = await framing.ReadHeaderAsync(_interruptTimeout, cancellationToken).ConfigureAwait(false);

                        if (status < 0)
                        {
                            break;
                        }

                        switch (type)
                        {
                            case ZFrameType.ZRPOS:
                                rewind = data;
                                break;
                            case ZFrameType.ZSKIP:
                                skipped = true;
                                break;
                            case ZFrameType.ZACK:
                                continue;
                            case ZFrameType.ZFIN or ZFrameType.ZABORT or ZFrameType.ZCAN:
                                throw new TransferException("The remote system aborted the transfer.");
                        }
                    }

                    if (last && rewind == null && !skipped)
                    {
                        break;
                    }
                }

                if (skipped)
                {
                    return -1;
                }

                if (rewind != null)
                {
                    if (++errors > MaxErrors)
                    {
                        throw new TransferException("Too many rewind requests while sending.");
                    }

                    position = Math.Min(rewind.Value, info.Length);
                    continue;
                }

                // The whole file went out; exchange ZEOF for the next-file ZRINIT.
                bool eofAcknowledged = false;

                for (int attempt = 0; attempt < MaxErrors && !eofAcknowledged; attempt++)
                {
                    await framing.WriteBinaryHeaderAsync(ZFrameType.ZEOF, (uint)position, useCrc32, cancellationToken).ConfigureAwait(false);

                    (int status, ZFrameType type, uint data) = await framing.ReadHeaderAsync(_headerTimeout, cancellationToken).ConfigureAwait(false);

                    if (status < 0)
                    {
                        continue;
                    }

                    switch (type)
                    {
                        case ZFrameType.ZRINIT:
                            eofAcknowledged = true;
                            break;
                        case ZFrameType.ZRPOS:
                            rewind = data;
                            break;
                        case ZFrameType.ZACK:
                            continue;
                        case ZFrameType.ZFIN or ZFrameType.ZABORT or ZFrameType.ZCAN:
                            throw new TransferException("The remote system aborted the transfer.");
                        default:
                            continue;
                    }

                    if (rewind != null)
                    {
                        break;
                    }
                }

                if (rewind != null)
                {
                    if (++errors > MaxErrors)
                    {
                        throw new TransferException("Too many rewind requests while sending.");
                    }

                    position = Math.Min(rewind.Value, info.Length);
                    continue;
                }

                if (!eofAcknowledged)
                {
                    throw new TransferException("The receiver did not acknowledge the end of the file.");
                }

                progress?.Report(new TransferSnapshot(fileName, position, info.Length, fileIndex, "Complete"));
                return position;
            }
        }

        /// <summary>
        /// Completes the ZFIN handshake and sends the trailing "OO".
        /// </summary>
        private static async Task FinishSessionAsync(ZmodemFraming framing, CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                await framing.WriteHexHeaderAsync(ZFrameType.ZFIN, 0, cancellationToken).ConfigureAwait(false);

                (int status, ZFrameType type, _) = await framing.ReadHeaderAsync(_headerTimeout, cancellationToken).ConfigureAwait(false);

                if (status == 0 && type == ZFrameType.ZFIN)
                {
                    await framing.WriteRawAsync("OO"u8.ToArray(), cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            // The files all transferred; a missed ZFIN handshake is not worth failing over.
        }

        /// <summary>
        /// Builds the ZFILE information subpacket ("name NUL size").
        /// </summary>
        private static byte[] BuildFileInfoPacket(string fileName, long size)
        {
            byte[] name = Encoding.UTF8.GetBytes(fileName);
            byte[] sizeText = Encoding.ASCII.GetBytes(size.ToString());
            var packet = new byte[name.Length + 1 + sizeText.Length + 1];
            name.CopyTo(packet, 0);
            sizeText.CopyTo(packet, name.Length + 1);
            return packet;
        }

        private static async Task TrySendCancelAsync(ITransferLink link)
        {
            try
            {
                byte[] abort = [0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08];
                await link.WriteAsync(abort, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // The link is already unusable; the cancel is best effort only.
            }
        }
    }
}
