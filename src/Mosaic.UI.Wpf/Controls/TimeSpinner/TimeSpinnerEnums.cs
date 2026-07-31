/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Identifies one of the three components a <see cref="TimeSpinner"/> displays.
    /// </summary>
    public enum TimeSpinnerField
    {
        /// <summary>
        /// The hour component, expressed on a twelve hour clock.
        /// </summary>
        Hour,

        /// <summary>
        /// The minute component.
        /// </summary>
        Minute,

        /// <summary>
        /// The AM or PM component.
        /// </summary>
        Meridiem
    }

    /// <summary>
    /// The half of the day a <see cref="TimeSpinner"/> value falls in.
    /// </summary>
    /// <remarks>
    /// The underlying numbers are used as wheel entry values, so they are fixed rather than incidental.
    /// </remarks>
    public enum TimeSpinnerMeridiem
    {
        /// <summary>
        /// Midnight through 11:59, the first half of the day.
        /// </summary>
        Am = 0,

        /// <summary>
        /// Noon through 23:59, the second half of the day.
        /// </summary>
        Pm = 1
    }

    /// <summary>
    /// Determines when edits made inside a <see cref="TimeSpinner"/> drop down are written back to
    /// <see cref="TimeSpinner.SelectedTime"/>.
    /// </summary>
    public enum TimeSpinnerCommitMode
    {
        /// <summary>
        /// The drop down shows Apply and Cancel buttons. The temporary time is only written to
        /// <see cref="TimeSpinner.SelectedTime"/> when the user applies it. This is the default because it matches
        /// the accept/dismiss model used by the WinUI 3 time picker.
        /// </summary>
        Explicit,

        /// <summary>
        /// Every change made to a wheel is written straight through to <see cref="TimeSpinner.SelectedTime"/> and no
        /// Apply or Cancel buttons are shown. Closing the drop down keeps whatever the user last chose.
        /// </summary>
        Immediate
    }

    /// <summary>
    /// Determines what happens to the temporary time when the user dismisses an open <see cref="TimeSpinner"/> drop
    /// down by clicking outside of it. Only meaningful when <see cref="TimeSpinner.CommitMode"/> is
    /// <see cref="TimeSpinnerCommitMode.Explicit"/>, since <see cref="TimeSpinnerCommitMode.Immediate"/> has already
    /// written every change through.
    /// </summary>
    public enum TimeSpinnerLightDismissBehavior
    {
        /// <summary>
        /// Discard the temporary time and keep the previously selected time. This is the default and matches the
        /// behavior of a dismissed WinUI flyout.
        /// </summary>
        Cancel,

        /// <summary>
        /// Commit the temporary time exactly as if the user had pressed Apply.
        /// </summary>
        Apply
    }

    /// <summary>
    /// Determines how the AM and PM designators are rendered.
    /// </summary>
    public enum TimeSpinnerMeridiemDisplayMode
    {
        /// <summary>
        /// The culture's own designators, for example <c>AM</c> and <c>PM</c> in United States English. Cultures
        /// that write times on a twenty four hour clock publish empty designators, so those fall back to the
        /// invariant <c>AM</c> and <c>PM</c> rather than rendering a blank wheel.
        /// </summary>
        Culture,

        /// <summary>
        /// The culture's designators upper cased, for example <c>AM</c>.
        /// </summary>
        Uppercase,

        /// <summary>
        /// The culture's designators lower cased, for example <c>am</c>.
        /// </summary>
        Lowercase
    }
}
