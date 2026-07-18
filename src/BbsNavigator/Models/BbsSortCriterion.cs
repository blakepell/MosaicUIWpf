/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;

namespace BbsNavigator.Models
{
    /// <summary>
    /// Identifies a BBS profile property that can participate in directory sorting.
    /// </summary>
    public enum BbsSortField
    {
        /// <summary>
        /// Sort by the BBS display name.
        /// </summary>
        [Description("Display Name")]
        DisplayName,

        /// <summary>
        /// Sort by favorite status.
        /// </summary>
        Favorite,

        /// <summary>
        /// Sort by the most recent successful connection.
        /// </summary>
        [Description("Last Connected")]
        LastConnected,

        /// <summary>
        /// Sort by host name.
        /// </summary>
        Host
    }

    /// <summary>
    /// Identifies the direction of one BBS directory sort level.
    /// </summary>
    public enum BbsSortDirection
    {
        /// <summary>
        /// Sort in ascending order.
        /// </summary>
        [Description("ASC")]
        Ascending,

        /// <summary>
        /// Sort in descending order.
        /// </summary>
        [Description("DESC")]
        Descending
    }

    /// <summary>
    /// Defines one editable level in a BBS directory sort.
    /// </summary>
    public partial class BbsSortCriterion : ObservableObject
    {
        /// <summary>
        /// Gets or sets the profile field used by this sort level.
        /// </summary>
        [ObservableProperty]
        private BbsSortField _field;

        /// <summary>
        /// Gets or sets the direction used by this sort level.
        /// </summary>
        [ObservableProperty]
        private BbsSortDirection _direction;
    }
}
