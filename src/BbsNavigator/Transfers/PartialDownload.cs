/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BbsNavigator.Transfers;

/// <summary>
/// Identifies the choice made when an interrupted ZMODEM download is found.
/// </summary>
public enum DownloadResumeAction
{
    /// <summary>
    /// Verifies the partial prefix before resuming.
    /// </summary>
    Resume,
    /// <summary>
    /// Restarts the partial download from its first byte.
    /// </summary>
    Restart,
    /// <summary>
    /// Cancels this transfer without changing the partial download.
    /// </summary>
    Cancel
}

/// <summary>
/// Describes an interrupted download offered for recovery.
/// </summary>
/// <param name="FileName">The remote file name.</param>
/// <param name="BytesReceived">The locally retained byte count.</param>
/// <param name="TotalBytes">The advertised file size.</param>
public sealed record PartialDownloadInfo(string FileName, long BytesReceived, long TotalBytes);

/// <summary>
/// Keeps partial data separate from completed files and scoped to the board and file announcement.
/// </summary>
internal sealed class PartialDownload : IAsyncDisposable
{
    private readonly string _folder;
    private readonly string _name;
    private readonly string _path;
    private readonly string _metadataPath;

    private PartialDownload(string folder, string name, string path, FileStream stream)
    {
        _folder = folder;
        _name = name;
        _path = path;
        _metadataPath = path + ".json";
        Stream = stream;
    }

    internal FileStream Stream { get; }

    internal static PartialDownload Open(string folder, string source, string name, long size, byte[] announcement)
    {
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source + "\0" + Convert.ToBase64String(announcement))));
        string partialFolder = Path.Combine(folder, ".bbs-partials");
        Directory.CreateDirectory(partialFolder);
        string path = Path.Combine(partialFolder, key + ".part");
        var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 65536, useAsync: true);
        try
        {
            File.WriteAllText(path + ".json", JsonSerializer.Serialize(new { Source = source, FileName = name, Size = size }));
            return new PartialDownload(folder, name, path, stream);
        }
        catch { stream.Dispose(); throw; }
    }

    internal async Task<string> CompleteAsync(CancellationToken token)
    {
        await Stream.FlushAsync(token).ConfigureAwait(false);
        await Stream.DisposeAsync().ConfigureAwait(false);
        string destination = XymodemReceiver.MakeUniquePath(_folder, _name);
        File.Move(_path, destination);
        try { File.Delete(_metadataPath); }
        catch (IOException) { /* The completed file is already safely published. */ }
        catch (UnauthorizedAccessException) { }
        return Path.GetFileName(destination);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}
