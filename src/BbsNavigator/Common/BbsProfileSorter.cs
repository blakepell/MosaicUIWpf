/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Sorts BBS profiles using an ordered collection of sort rules.
    /// </summary>
    internal static class BbsProfileSorter
    {
        /// <summary>
        /// Returns a stable, sorted snapshot of the supplied profiles.
        /// </summary>
        public static BbsProfile[] Sort(IEnumerable<BbsProfile> profiles, IReadOnlyList<BbsSortRule> rules)
        {
            return profiles
                .OrderBy(profile => profile, new BbsProfileComparer(rules))
                .ToArray();
        }

        private sealed class BbsProfileComparer : IComparer<BbsProfile>
        {
            private readonly IReadOnlyList<BbsSortRule> _rules;

            public BbsProfileComparer(IReadOnlyList<BbsSortRule> rules)
            {
                _rules = rules;
            }

            public int Compare(BbsProfile? x, BbsProfile? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                foreach (BbsSortRule rule in _rules)
                {
                    int result = rule.Direction == BbsSortDirection.Ascending
                        ? CompareField(x, y, rule.Field)
                        : CompareField(y, x, rule.Field);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                return 0;
            }

            private static int CompareField(BbsProfile x, BbsProfile y, BbsSortField field)
            {
                return field switch
                {
                    BbsSortField.DisplayName => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name),
                    BbsSortField.Favorite => x.Favorite.CompareTo(y.Favorite),
                    BbsSortField.LastConnected => Nullable.Compare(x.LastConnected, y.LastConnected),
                    BbsSortField.Host => StringComparer.OrdinalIgnoreCase.Compare(x.Host, y.Host),
                    _ => 0
                };
            }
        }
    }

    /// <summary>
    /// Contains an immutable BBS directory sort rule used by the background sorter.
    /// </summary>
    internal readonly record struct BbsSortRule(BbsSortField Field, BbsSortDirection Direction);
}
