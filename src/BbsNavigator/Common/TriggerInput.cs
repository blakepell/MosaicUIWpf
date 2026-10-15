/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BbsNavigator.Common;

/// <summary>
/// Expands trigger captures in one pass without reinterpreting inserted text as placeholders.
/// </summary>
internal static class TriggerInput
{
    /// <summary>
    /// Replaces numbered and named capture placeholders with escaped string contents suitable for quoted script strings.
    /// </summary>
    /// <param name="script">The script containing %0, capture numbers or names, or %% for a literal percent.</param>
    /// <param name="line">The entire received line, used for %0 even for partial regex matches.</param>
    /// <param name="match">The successful regular expression match.</param>
    /// <returns>The expanded script; missing captures become empty strings.</returns>
    internal static string Expand(string script, string line, Match match)
    {
        int start = script.IndexOf('%');
        if (start < 0) return script;

        var result = new StringBuilder(script.Length + 32);
        result.Append(script.AsSpan(0, start));
        for (int i = start; i < script.Length; i++)
        {
            if (script[i] != '%' || i + 1 == script.Length)
            {
                result.Append(script[i]);
                continue;
            }

            if (script[i + 1] == '%')
            {
                result.Append('%');
                i++;
                continue;
            }

            int end = i + 1;
            string value = string.Empty;
            if (char.IsAsciiDigit(script[end]))
            {
                while (end < script.Length && char.IsAsciiDigit(script[end]))
                {
                    end++;
                }

                if (int.TryParse(script.AsSpan(i + 1, end - i - 1), NumberStyles.None,
                        CultureInfo.InvariantCulture, out int group))
                {
                    value = group == 0 ? line : match.Groups[group].Value;
                }
            }
            else if (IsGroupNameCharacter(script[end]))
            {
                while (end < script.Length && IsGroupNameCharacter(script[end]))
                {
                    end++;
                }

                value = match.Groups[script[(i + 1)..end]].Value;
            }
            else
            {
                result.Append('%');
                continue;
            }

            // JSON escaping protects control characters, quotes, backslashes, and Unicode line
            // separators. Also protect single quotes and template interpolation.
            string escaped = JsonSerializer.Serialize(value);
            result.Append(escaped.AsSpan(1, escaped.Length - 2).ToString()
                .Replace("'", "\\u0027").Replace("`", "\\u0060").Replace("$", "\\u0024"));
            i = end - 1;
        }

        return result.ToString();
    }

    /// <summary>
    /// Identifies the word characters accepted in .NET regular expression group names.
    /// </summary>
    /// <param name="value">The character to inspect.</param>
    /// <returns><see langword="true"/> for a group name character; otherwise, <see langword="false"/>.</returns>
    private static bool IsGroupNameCharacter(char value) =>
        char.IsLetterOrDigit(value)
        || char.GetUnicodeCategory(value) is UnicodeCategory.NonSpacingMark or UnicodeCategory.ConnectorPunctuation;

}
