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
    /// Receives a batch of files from a remote ZMODEM sender (the classic <c>sz</c> flow),
    /// including streaming ZCRCG frames, CRC-32 negotiation, and ZRPOS error recovery.
    /// </summary>
    public sealed class ZmodemReceiver
    {
        private const int MaxErrors = 20;
        private static readonly TimeSpan _headerTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Receives files into the specified folder until the sender finishes the session.
        /// </summary>
        /// <param name="link">The byte channel to the remote system.</param>
        /// <param name="destinationFolder">The folder that receives the files.</param>
        /// <param name="progress">An optional progress sink.</param>
        /// <param name="cancellationToken">A token that aborts the transfer.</param>
        /// <returns>A summary of the completed session.</returns>
        /// <exception cref="TransferException">The remote side canceled or the protocol failed.</exception>
        /// <exception cref="OperationCanceledException">The transfer was canceled locally.</exception>
        public async Task<TransferResult> ReceiveAsync(
            ITransferLink link,
            string destinationFolder,
            IProgress<TransferSnapshot>? progress,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(destinationFolder);

            var framing = new ZmodemFraming(link);
            var stopwatch = Stopwatch.StartNew();
            var files = new List<string>();
            var subpacket = new byte[8192];
            long totalBytes = 0;
            int errors = 0;

            FileStream? currentFile = null;
            byte[]? currentInfo = null;
            string currentName = string.Empty;
            long currentSize = -1;
            long position = 0;

            // ZRINIT flags travel in the ZF0 byte, which is the high byte of the payload.
            const uint rinitData = (ZmodemFraming.CanFdx | ZmodemFraming.CanOvio | ZmodemFraming.CanFc32) << 24;

            try
            {
                await framing.WriteHexHeaderAsync(ZFrameType.ZRINIT, rinitData, cancellationToken).ConfigureAwait(false);

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    (int status, ZFrameType type, uint data) = await framing.ReadHeaderAsync(_headerTimeout, cancellationToken).ConfigureAwait(false);

                    if (status < 0)
                    {
                        if (++errors > MaxErrors)
                        {
                            throw new TransferException("Too many errors while waiting for the sender.");
                        }

                        framing.Purge();

                        if (currentFile == null)
                        {
                            await framing.WriteHexHeaderAsync(ZFrameType.ZRINIT, rinitData, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await framing.WriteHexHeaderAsync(ZFrameType.ZRPOS, (uint)position, cancellationToken).ConfigureAwait(false);
                        }

                        continue;
                    }

                    switch (type)
                    {
                        case ZFrameType.ZRQINIT:
                            await framing.WriteHexHeaderAsync(ZFrameType.ZRINIT, rinitData, cancellationToken).ConfigureAwait(false);
                            continue;

                        case ZFrameType.ZSINIT:
                        {
                            (int length, _) = await framing.ReadDataSubpacketAsync(subpacket, cancellationToken).ConfigureAwait(false);

                            if (length < 0)
                            {
                                await framing.WriteHexHeaderAsync(ZFrameType.ZNAK, 0, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await framing.WriteHexHeaderAsync(ZFrameType.ZACK, 0, cancellationToken).ConfigureAwait(false);
                            }

                            continue;
                        }

                        case ZFrameType.ZFILE:
                        {
                            (int length, _) = await framing.ReadDataSubpacketAsync(subpacket, cancellationToken).ConfigureAwait(false);

                            if (length < 0)
                            {
                                framing.Purge();
                                await framing.WriteHexHeaderAsync(ZFrameType.ZNAK, 0, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            if (currentFile != null && currentInfo != null && subpacket.AsSpan(0, length).SequenceEqual(currentInfo))
                            {
                                // A retransmitted announcement for the file already in
                                // progress; just repeat where to (re)start.
                                await framing.WriteHexHeaderAsync(ZFrameType.ZRPOS, (uint)position, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            (string? name, long size) = ParseFileInfo(subpacket.AsSpan(0, length));

                            if (string.IsNullOrEmpty(name))
                            {
                                await framing.WriteHexHeaderAsync(ZFrameType.ZNAK, 0, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            if (currentFile != null)
                            {
                                await currentFile.DisposeAsync().ConfigureAwait(false);
                            }

                            currentInfo = subpacket.AsSpan(0, length).ToArray();

                            string path = XymodemReceiver.MakeUniquePath(destinationFolder, name);
                            currentFile = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true);
                            currentName = Path.GetFileName(path);
                            currentSize = size;
                            position = 0;
                            errors = 0;
                            progress?.Report(new TransferSnapshot(currentName, 0, currentSize, files.Count + 1, "Starting"));
                            await framing.WriteHexHeaderAsync(ZFrameType.ZRPOS, 0, cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        case ZFrameType.ZDATA:
                        {
                            if (currentFile == null)
                            {
                                await framing.WriteHexHeaderAsync(ZFrameType.ZNAK, 0, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            if (data != (uint)position)
                            {
                                framing.Purge();
                                await framing.WriteHexHeaderAsync(ZFrameType.ZRPOS, (uint)position, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            bool frameOpen = true;

                            while (frameOpen)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                (int length, byte frameEnd) = await framing.ReadDataSubpacketAsync(subpacket, cancellationToken).ConfigureAwait(false);

                                if (length < 0)
                                {
                                    if (++errors > MaxErrors)
                                    {
                                        throw new TransferException("Too many data errors while receiving.");
                                    }

                                    framing.Purge();
                                    await framing.WriteHexHeaderAsync(ZFrameType.ZRPOS, (uint)position, cancellationToken).ConfigureAwait(false);
                                    break;
                                }

                                await currentFile.WriteAsync(subpacket.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                                position += length;
                                totalBytes += length;
                                errors = 0;
                                progress?.Report(new TransferSnapshot(currentName, position, currentSize, files.Count + 1, "Receiving"));

                                switch (frameEnd)
                                {
                                    case ZmodemFraming.ZCrcG:
                                        continue;
                                    case ZmodemFraming.ZCrcQ:
                                        await framing.WriteHexHeaderAsync(ZFrameType.ZACK, (uint)position, cancellationToken).ConfigureAwait(false);
                                        continue;
                                    case ZmodemFraming.ZCrcW:
                                        await framing.WriteHexHeaderAsync(ZFrameType.ZACK, (uint)position, cancellationToken).ConfigureAwait(false);
                                        frameOpen = false;
                                        continue;
                                    default:
                                        frameOpen = false;
                                        continue;
                                }
                            }

                            continue;
                        }

                        case ZFrameType.ZEOF:
                            if (currentFile == null)
                            {
                                continue;
                            }

                            if (data != (uint)position)
                            {
                                // The end-of-file position disagrees; ask the sender to
                                // resume from where this side actually is.
                                await framing.WriteHexHeaderAsync(ZFrameType.ZRPOS, (uint)position, cancellationToken).ConfigureAwait(false);
                                continue;
                            }

                            await currentFile.DisposeAsync().ConfigureAwait(false);
                            currentFile = null;
                            currentInfo = null;
                            files.Add(currentName);
                            progress?.Report(new TransferSnapshot(currentName, position, currentSize, files.Count, "Complete"));
                            await framing.WriteHexHeaderAsync(ZFrameType.ZRINIT, rinitData, cancellationToken).ConfigureAwait(false);
                            continue;

                        case ZFrameType.ZFIN:
                            await framing.WriteHexHeaderAsync(ZFrameType.ZFIN, 0, cancellationToken).ConfigureAwait(false);
                            await ReadOverAndOutAsync(framing, cancellationToken).ConfigureAwait(false);
                            return new TransferResult(files, totalBytes, stopwatch.Elapsed);

                        case ZFrameType.ZFREECNT:
                            // Report "unknown/unlimited" free space.
                            await framing.WriteHexHeaderAsync(ZFrameType.ZACK, uint.MaxValue, cancellationToken).ConfigureAwait(false);
                            continue;

                        case ZFrameType.ZABORT:
                        case ZFrameType.ZCAN:
                            throw new TransferException("The remote system aborted the transfer.");

                        default:
                            // Unsupported frame (for example ZCOMMAND, which this client
                            // never executes): reject it and keep the session alive.
                            await framing.WriteHexHeaderAsync(ZFrameType.ZNAK, 0, cancellationToken).ConfigureAwait(false);
                            continue;
                    }
                }
            }
            catch
            {
                await TrySendCancelAsync(link).ConfigureAwait(false);
                throw;
            }
            finally
            {
                if (currentFile != null)
                {
                    await currentFile.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Parses the ZFILE information subpacket ("name NUL size mtime mode ...").
        /// </summary>
        private static (string? Name, long Size) ParseFileInfo(ReadOnlySpan<byte> data)
        {
            int nameEnd = data.IndexOf((byte)0);

            if (nameEnd <= 0)
            {
                return (null, -1);
            }

            string name = Encoding.UTF8.GetString(data[..nameEnd]);
            long size = -1;
            ReadOnlySpan<byte> rest = data[(nameEnd + 1)..];
            int restEnd = rest.IndexOf((byte)0);

            if (restEnd >= 0)
            {
                rest = rest[..restEnd];
            }

            string info = Encoding.ASCII.GetString(rest);
            string[] parts = info.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && long.TryParse(parts[0], out long parsedSize))
            {
                size = parsedSize;
            }

            return (name, size);
        }

        /// <summary>
        /// Consumes the sender's trailing "OO" (over and out), which is optional.
        /// </summary>
        private static async Task ReadOverAndOutAsync(ZmodemFraming framing, CancellationToken cancellationToken)
        {
            for (int i = 0; i < 2; i++)
            {
                int value = await framing.Reader.ReadByteAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

                if (value != 'O')
                {
                    break;
                }
            }
        }

        private static async Task TrySendCancelAsync(ITransferLink link)
        {
            try
            {
                // Eight CANs followed by backspaces is the canonical ZMODEM abort sequence.
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
