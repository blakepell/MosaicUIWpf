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

namespace WindowCue.Services
{
    /// <summary>
    /// Fuzzy string matching helpers used to score how closely a live window title
    /// matches the title that was recorded when a window was pinned. This lets WindowCue
    /// re-bind to the right window even when its process ID has changed (for example, when
    /// Visual Studio is closed and re-opened on the same solution).
    /// </summary>
    public static class WindowTitleMatcher
    {
        /// <summary>
        /// Returns a similarity score in the range <c>0.0</c> (no resemblance) to
        /// <c>1.0</c> (identical) describing how close two window titles are. The score
        /// blends an edit-distance ratio with a word-set (Jaccard) ratio and rewards
        /// prefix / containment relationships, which are common for titles such as
        /// <c>"MosaicWpfUI - Microsoft Visual Studio"</c>.
        /// </summary>
        public static double Similarity(string? a, string? b)
        {
            string x = Normalize(a);
            string y = Normalize(b);

            if (x.Length == 0 || y.Length == 0)
            {
                return 0d;
            }

            if (x == y)
            {
                return 1d;
            }

            double levenshtein = LevenshteinRatio(x, y);
            double tokenSet = TokenSetRatio(x, y);
            double score = Math.Max(levenshtein, tokenSet);

            // A pinned title is frequently a prefix of the live title (the document /
            // solution name leads, the application name trails), so reward that strongly.
            if (x.StartsWith(y, StringComparison.Ordinal) || y.StartsWith(x, StringComparison.Ordinal))
            {
                score = Math.Max(score, 0.9d);
            }
            else if (x.Contains(y, StringComparison.Ordinal) || y.Contains(x, StringComparison.Ordinal))
            {
                score = Math.Max(score, 0.75d);
            }

            return score;
        }

        /// <summary>
        /// Lower-cases, trims, collapses internal whitespace, and strips common
        /// "modified document" markers so cosmetic differences do not affect the score.
        /// </summary>
        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length);
            bool lastWasWhitespace = false;

            foreach (char c in value.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasWhitespace)
                    {
                        sb.Append(' ');
                        lastWasWhitespace = true;
                    }

                    continue;
                }

                // Drop "unsaved" / bullet markers that some editors prepend to titles.
                if (c is '●' or '•' or '*' or '★')
                {
                    continue;
                }

                sb.Append(char.ToLowerInvariant(c));
                lastWasWhitespace = false;
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Normalized Levenshtein similarity: <c>1 - distance / max(len)</c>.
        /// </summary>
        private static double LevenshteinRatio(string a, string b)
        {
            int distance = Levenshtein(a, b);
            int max = Math.Max(a.Length, b.Length);
            return max == 0 ? 1d : 1d - ((double)distance / max);
        }

        /// <summary>
        /// Computes the Levenshtein edit distance between two strings using two rolling rows.
        /// </summary>
        private static int Levenshtein(string a, string b)
        {
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];

            for (int j = 0; j <= b.Length; j++)
            {
                previous[j] = j;
            }

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;

                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1), previous[j - 1] + cost);
                }

                (previous, current) = (current, previous);
            }

            return previous[b.Length];
        }

        /// <summary>
        /// Jaccard similarity over the set of whitespace-delimited tokens. This is order
        /// independent, so it tolerates re-arranged title segments.
        /// </summary>
        private static double TokenSetRatio(string a, string b)
        {
            var setA = new HashSet<string>(a.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var setB = new HashSet<string>(b.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (setA.Count == 0 || setB.Count == 0)
            {
                return 0d;
            }

            int intersection = setA.Count(setB.Contains);
            int union = setA.Count + setB.Count - intersection;
            return union == 0 ? 0d : (double)intersection / union;
        }
    }
}
