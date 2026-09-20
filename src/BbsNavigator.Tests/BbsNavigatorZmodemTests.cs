/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Transfers;
using System.IO;
using System.Text;
using System.Threading.Channels;
using Xunit;

namespace BbsNavigator.Tests;

public class BbsNavigatorZmodemTests
{
    private sealed class Link : ITransferLink
    {
        private readonly Channel<byte> _incoming = Channel.CreateUnbounded<byte>();
        internal Link Peer { get; set; } = null!;
        public bool DataAvailable => _incoming.Reader.TryPeek(out _);
        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token)
        {
            byte first = await _incoming.Reader.ReadAsync(token);
            buffer.Span[0] = first;
            int count = 1;
            while (count < buffer.Length && _incoming.Reader.TryRead(out byte value)) buffer.Span[count++] = value;
            return count;
        }
        public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (byte value in bytes.Span) Peer._incoming.Writer.TryWrite(value);
            return ValueTask.CompletedTask;
        }
        public void Purge() { while (_incoming.Reader.TryRead(out _)) { } }
        internal static (Link, Link) Pair()
        {
            var a = new Link(); var b = new Link(); a.Peer = b; b.Peer = a; return (a, b);
        }
    }

    private sealed class ProgressRecorder : IProgress<TransferSnapshot>
    {
        internal readonly List<TransferSnapshot> Values = new();
        public void Report(TransferSnapshot value) => Values.Add(value);
    }

    [Theory]
    [InlineData(false, DownloadResumeAction.Resume)]
    [InlineData(true, DownloadResumeAction.Resume)]
    [InlineData(false, DownloadResumeAction.Restart)]
    public async Task InterruptedDownloadIsVerifiedAndResumedOrRestarted(bool corrupt, DownloadResumeAction action)
    {
        string root = Path.Combine(Path.GetTempPath(), "bbs-zmodem-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        CancellationToken token = deadline.Token;
        try
        {
            byte[] data = Enumerable.Range(0, 32768).Select(i => (byte)(i * 17)).ToArray();
            string source = Path.Combine(root, "sample.bin");
            await File.WriteAllBytesAsync(source, data, token);
            string destination = Path.Combine(root, "received");
            byte[] info = Encoding.UTF8.GetBytes("sample.bin\0" + data.Length + "\0");
            var (remote, local) = Link.Pair();
            var framing = new ZmodemFraming(remote);
            Task first = new ZmodemReceiver().ReceiveAsync(local, destination, null, token, "board-A");
            await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            await framing.WriteBinaryHeaderAsync(ZFrameType.ZFILE, 0, true, token);
            await framing.WriteDataSubpacketAsync(info, ZmodemFraming.ZCrcW, true, token);
            var (_, type, start) = await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            Assert.Equal(ZFrameType.ZRPOS, type);
            Assert.Equal(0u, start);
            await framing.WriteBinaryHeaderAsync(ZFrameType.ZDATA, 0, true, token);
            await framing.WriteDataSubpacketAsync(data.AsMemory(0, 1024), ZmodemFraming.ZCrcW, true, token);
            await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            await framing.WriteHexHeaderAsync(ZFrameType.ZCAN, 0, token);
            await Assert.ThrowsAsync<TransferException>(() => first);
            Assert.Empty(Directory.GetFiles(destination));
            string partialPath = Assert.Single(Directory.GetFiles(Path.Combine(destination, ".bbs-partials"), "*.part"));
            Assert.Equal(1024, new FileInfo(partialPath).Length);
            if (corrupt)
            {
                byte[] prefix = await File.ReadAllBytesAsync(partialPath, token);
                prefix[0] ^= 0xFF;
                await File.WriteAllBytesAsync(partialPath, prefix, token);
            }

            var progress = new ProgressRecorder();
            var (senderLink, receiverLink) = Link.Pair();
            Task<TransferResult> receiving = new ZmodemReceiver().ReceiveAsync(receiverLink, destination, progress, token,
                "board-A", (_, _) => Task.FromResult(action));
            Task<TransferResult> sending = new ZmodemSender().SendAsync(senderLink, new[] { source }, null, token);
            await Task.WhenAll(receiving, sending);
            Assert.Equal(data, await File.ReadAllBytesAsync(Path.Combine(destination, "sample.bin"), token));
            Assert.Empty(Directory.GetFiles(Path.Combine(destination, ".bbs-partials")));
            if (!corrupt && action == DownloadResumeAction.Resume)
                Assert.Contains(progress.Values, p => p.Message == "Resuming");
            else
                Assert.DoesNotContain(progress.Values, p => p.Message == "Resuming");
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task PartialFilesAreScopedToBoardAndAnnouncement()
    {
        string root = Path.Combine(Path.GetTempPath(), "bbs-partial-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using (var first = PartialDownload.Open(root, "board-A", "same.bin", 10, "same.bin\0"u8.ToArray()))
                await first.Stream.WriteAsync(new byte[] { 1, 2, 3 });
            await using var other = PartialDownload.Open(root, "board-B", "same.bin", 10, "same.bin\0"u8.ToArray());
            await using var changed = PartialDownload.Open(root, "board-A", "same.bin", 20, "different announcement"u8.ToArray());
            Assert.Equal(0, other.Stream.Length);
            Assert.Equal(0, changed.Stream.Length);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResumeCancellationPreservesPartialAndUnsupportedCrcRestarts(bool cancel)
    {
        string root = Path.Combine(Path.GetTempPath(), "bbs-resume-choice-" + Guid.NewGuid().ToString("N"));
        byte[] data = Enumerable.Range(0, 1024).Select(i => (byte)i).ToArray();
        byte[] announcement = Encoding.UTF8.GetBytes("file.bin\0" + data.Length + "\0");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = deadline.Token;
        try
        {
            await using (var partial = PartialDownload.Open(root, "board", "file.bin", data.Length, announcement))
                await partial.Stream.WriteAsync(data.AsMemory(0, 100), token);
            var (remote, local) = Link.Pair();
            var framing = new ZmodemFraming(remote);
            Task<TransferResult> receive = new ZmodemReceiver().ReceiveAsync(local, root, null, token, "board",
                (_, _) => Task.FromResult(cancel ? DownloadResumeAction.Cancel : DownloadResumeAction.Resume));
            await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            await framing.WriteBinaryHeaderAsync(ZFrameType.ZFILE, 0, true, token);
            await framing.WriteDataSubpacketAsync(announcement, ZmodemFraming.ZCrcW, true, token);
            if (cancel)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receive);
                string path = Assert.Single(Directory.GetFiles(Path.Combine(root, ".bbs-partials"), "*.part"));
                Assert.Equal(data[..100], await File.ReadAllBytesAsync(path, token));
                return;
            }
            var (_, query, prefix) = await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            Assert.Equal(ZFrameType.ZCRC, query);
            Assert.Equal(100u, prefix);
            await framing.WriteHexHeaderAsync(ZFrameType.ZNAK, 0, token);
            var (_, restart, position) = await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            Assert.Equal(ZFrameType.ZRPOS, restart);
            Assert.Equal(0u, position);
            await framing.WriteBinaryHeaderAsync(ZFrameType.ZDATA, 0, true, token);
            await framing.WriteDataSubpacketAsync(data, ZmodemFraming.ZCrcE, true, token);
            await framing.WriteHexHeaderAsync(ZFrameType.ZEOF, (uint)data.Length, token);
            await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            await framing.WriteHexHeaderAsync(ZFrameType.ZFIN, 0, token);
            await framing.ReadHeaderAsync(TimeSpan.FromSeconds(2), token);
            await framing.WriteRawAsync("OO"u8.ToArray(), token);
            await receive;
            Assert.Equal(data, await File.ReadAllBytesAsync(Path.Combine(root, "file.bin"), token));
        }
        finally { Directory.Delete(root, true); }
    }
}
