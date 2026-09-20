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
    /// Maintains the most-recently-connected BBS list persisted in the application settings.
    /// </summary>
    internal static class RecentConnections
    {
        /// <summary>
        /// The number of connections retained in the recent list.
        /// </summary>
        public const int MaxEntries = 10;

        /// <summary>
        /// Records a connection, moving the profile to the front of the recent list.
        /// </summary>
        /// <param name="settings">The settings that store the recent list.</param>
        /// <param name="profile">The BBS that was connected to.</param>
        public static void Record(AppSettings settings, BbsProfile profile)
        {
            var recent = settings.RecentConnections;

            // A profile is never listed twice, so an existing entry is moved rather than added.
            recent.Remove(profile.Id);
            recent.Insert(0, profile.Id);

            while (recent.Count > MaxEntries)
            {
                recent.RemoveAt(recent.Count - 1);
            }
        }

        /// <summary>
        /// Drops a profile from the recent list, used when the BBS leaves the directory.
        /// </summary>
        /// <param name="settings">The settings that store the recent list.</param>
        /// <param name="profile">The BBS being removed.</param>
        public static void Remove(AppSettings settings, BbsProfile profile)
        {
            settings.RecentConnections.Remove(profile.Id);
        }

        /// <summary>
        /// Resolves the recent list to the profiles that still exist in the directory.
        /// </summary>
        /// <param name="settings">The settings that store the recent list.</param>
        /// <returns>The recent profiles, most recently connected first.</returns>
        public static List<BbsProfile> Resolve(AppSettings settings)
        {
            var profiles = new Dictionary<Guid, BbsProfile>();

            foreach (BbsProfile profile in settings.BbsProfiles)
            {
                profiles[profile.Id] = profile;
            }

            var matches = new List<BbsProfile>();

            foreach (Guid id in settings.RecentConnections)
            {
                // Identifiers with no profile are left in place: the directory loads in
                // batches at startup so a miss does not necessarily mean the BBS is gone.
                if (profiles.TryGetValue(id, out BbsProfile? profile))
                {
                    matches.Add(profile);
                }
            }

            return matches;
        }
    }
}
