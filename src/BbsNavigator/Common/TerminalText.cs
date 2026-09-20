/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text;

namespace BbsNavigator.Common;

/// <summary>
/// Prepares message text for terminal transmission without changing the saved draft.
/// </summary>
internal static class TerminalText
{
    internal static string Normalize(string text) => text.Replace("\r\n", "\r").Replace('\n', '\r');

    internal static string EncodingProblem(string text, Encoding encoding)
    {
        var strict = (Encoding)encoding.Clone();
        strict.EncoderFallback = EncoderFallback.ExceptionFallback;
        try { strict.GetByteCount(text); return string.Empty; }
        catch (EncoderFallbackException ex)
        {
            string character = ex.IsUnknownSurrogate()
                ? char.ConvertFromUtf32(char.ConvertToUtf32(ex.CharUnknownHigh, ex.CharUnknownLow))
                : ex.CharUnknown.ToString();
            return $"The selected encoding cannot send ‘{character}’. Edit the text or change the session encoding.";
        }
    }

    internal static string Wrap(string text, int columns)
    {
        if (columns <= 0) return text;
        var result = new List<string>();
        foreach (string original in Normalize(text).Split('\r'))
        {
            string line = original;
            while (line.EnumerateRunes().Count() > columns)
            {
                int boundary = 0;
                foreach (Rune rune in line.EnumerateRunes().Take(columns)) boundary += rune.Utf16SequenceLength;
                int split = line[boundary] == ' ' ? boundary : line.LastIndexOf(' ', boundary - 1, boundary);
                if (split <= 0) split = boundary;
                result.Add(line[..split]);
                line = line[split..];
                if (line.StartsWith(' ')) line = line[1..];
            }
            result.Add(line);
        }
        return string.Join(Environment.NewLine, result);
    }

    internal static async Task SendAsync(string text, int characterDelay, int lineDelay,
        Func<string, CancellationToken, Task> send, Action<int, int> progress, CancellationToken token)
    {
        text = Normalize(text);
        int sent = 0;
        int buffered = 0;
        var batch = new StringBuilder(256);
        characterDelay = Math.Clamp(characterDelay, 0, 1000);
        foreach (Rune rune in text.EnumerateRunes())
        {
            token.ThrowIfCancellationRequested();
            batch.Append(rune.ToString());
            buffered += rune.Utf16SequenceLength;
            if (characterDelay == 0 && rune.Value != '\r' && buffered < 256 && sent + buffered < text.Length) continue;
            await send(batch.ToString(), token);
            sent += buffered;
            batch.Clear();
            buffered = 0;
            if (characterDelay == 0 || sent % 32 == 0 || rune.Value == '\r' || sent == text.Length) progress(sent, text.Length);
            int delay = characterDelay + (rune.Value == '\r' ? Math.Clamp(lineDelay, 0, 10000) : 0);
            if (delay > 0) await Task.Delay(delay, token);
            else await Task.Yield();
        }
    }
}
