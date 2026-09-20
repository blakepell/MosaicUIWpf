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

namespace BbsNavigator.Common;

/// <summary>
/// Stores independent per-board drafts using atomic replacement in the application data folder.
/// </summary>
internal sealed class MessageDraftStore(string folder, Guid profileId)
{
    private readonly string _path = Path.Combine(folder, "Drafts", $"{profileId:N}.txt");

    internal string Load() => File.Exists(_path) ? File.ReadAllText(_path) : string.Empty;

    internal void Save(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string temporary = _path + ".tmp";
        File.WriteAllText(temporary, text);
        File.Move(temporary, _path, overwrite: true);
    }
}
