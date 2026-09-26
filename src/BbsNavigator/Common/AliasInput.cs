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
using System.Text.RegularExpressions;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Splits command box input into an alias name and its arguments, and expands those
    /// arguments into a copy of the alias script.
    /// </summary>
    /// <remarks>
    /// Arguments are separated by whitespace; text inside double quotes is one argument with
    /// the quotes removed. In the script, <c>%1</c> through <c>%9</c> are replaced with the
    /// arguments, <c>%0</c> with everything typed after the alias name, and <c>%%</c> with a
    /// literal percent sign. Substituted values are escaped so they are safe inside a
    /// JavaScript string literal such as <c>let name = "%1";</c>.
    /// </remarks>
    public static class AliasInput
    {
        private static readonly Regex Placeholders = new(@"%(%|[0-9])", RegexOptions.Compiled);

        /// <summary>
        /// Splits <paramref name="input"/> into the alias name and its arguments.
        /// </summary>
        /// <param name="input">The text entered in the command box.</param>
        /// <param name="name">The first word.</param>
        /// <param name="arguments">The remaining words, with quoted text kept together.</param>
        /// <param name="remainder">Everything after the first word, trimmed.</param>
        /// <returns><see langword="false"/> when the input is blank.</returns>
        public static bool TryParse(string? input, out string name, out IReadOnlyList<string> arguments, out string remainder)
        {
            name = string.Empty;
            arguments = [];
            remainder = string.Empty;

            string text = input?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                return false;
            }

            int end = 0;
            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            name = text[..end];
            remainder = text[end..].Trim();
            arguments = Split(remainder);
            return true;
        }

        /// <summary>
        /// Splits text into whitespace-separated arguments. A double-quoted run is one argument
        /// without its quotes; an unterminated quote runs to the end of the text.
        /// </summary>
        /// <param name="text">The argument text.</param>
        /// <returns>The arguments in order.</returns>
        public static IReadOnlyList<string> Split(string text)
        {
            var arguments = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            bool hasArgument = false;

            foreach (char c in text)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasArgument = true;
                }
                else if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (hasArgument)
                    {
                        arguments.Add(current.ToString());
                        current.Clear();
                        hasArgument = false;
                    }
                }
                else
                {
                    current.Append(c);
                    hasArgument = true;
                }
            }

            if (hasArgument)
            {
                arguments.Add(current.ToString());
            }

            return arguments;
        }

        /// <summary>
        /// Returns a copy of <paramref name="script"/> with its placeholders replaced.
        /// </summary>
        /// <param name="script">The alias script.</param>
        /// <param name="arguments">The values for <c>%1</c> through <c>%9</c>; missing values become empty.</param>
        /// <param name="remainder">The value for <c>%0</c>.</param>
        /// <returns>The script to execute.</returns>
        public static string Expand(string script, IReadOnlyList<string> arguments, string remainder)
        {
            ArgumentNullException.ThrowIfNull(script);
            ArgumentNullException.ThrowIfNull(arguments);

            return Placeholders.Replace(script, match =>
            {
                char token = match.Groups[1].Value[0];
                if (token == '%')
                {
                    return "%";
                }

                int index = token - '0';
                string value = index == 0 ? remainder : index <= arguments.Count ? arguments[index - 1] : string.Empty;
                return EscapeForString(value ?? string.Empty);
            });
        }

        /// <summary>
        /// Escapes characters that would end or corrupt a JavaScript string or template literal.
        /// </summary>
        private static string EscapeForString(string value)
        {
            if (value.AsSpan().IndexOfAny("\\\"'`\r\n\t") < 0)
            {
                return value;
            }

            var sb = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\'': sb.Append("\\'"); break;
                    case '`': sb.Append("\\`"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }

            return sb.ToString();
        }
    }
}
