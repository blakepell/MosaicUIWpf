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
    /// Identifies one of the three date components a <see cref="DateSpinner"/> can display.
    /// </summary>
    public enum DateSpinnerField
    {
        /// <summary>
        /// The month component.
        /// </summary>
        Month,

        /// <summary>
        /// The day of month component.
        /// </summary>
        Day,

        /// <summary>
        /// The year component.
        /// </summary>
        Year
    }

    /// <summary>
    /// Determines when edits made inside a <see cref="DateSpinner"/> drop down are written back to
    /// <see cref="DateSpinner.SelectedDate"/>.
    /// </summary>
    public enum DateSpinnerCommitMode
    {
        /// <summary>
        /// The drop down shows Apply and Cancel buttons. The temporary date is only written to
        /// <see cref="DateSpinner.SelectedDate"/> when the user applies it. This is the default because it matches the
        /// accept/dismiss model used by the WinUI 3 date picker.
        /// </summary>
        Explicit,

        /// <summary>
        /// Every change made to a selector is written straight through to <see cref="DateSpinner.SelectedDate"/> and
        /// no Apply or Cancel buttons are shown. Closing the drop down keeps whatever the user last chose.
        /// </summary>
        Immediate
    }

    /// <summary>
    /// Determines how month values are rendered when <see cref="DateSpinner.MonthFormat"/> has not been set.
    /// </summary>
    public enum DateSpinnerMonthDisplayMode
    {
        /// <summary>
        /// The culture's full month name, for example <c>January</c>.
        /// </summary>
        FullName,

        /// <summary>
        /// The culture's abbreviated month name, for example <c>Jan</c>.
        /// </summary>
        AbbreviatedName,

        /// <summary>
        /// The month number without padding, for example <c>1</c>.
        /// </summary>
        Numeric,

        /// <summary>
        /// The month number padded to two digits, for example <c>01</c>.
        /// </summary>
        TwoDigitNumeric
    }

    /// <summary>
    /// Determines what happens to the temporary date when the user dismisses an open
    /// <see cref="DateSpinner"/> drop down by clicking outside of it. Only meaningful when
    /// <see cref="DateSpinner.CommitMode"/> is <see cref="DateSpinnerCommitMode.Explicit"/>, since
    /// <see cref="DateSpinnerCommitMode.Immediate"/> has already written every change through.
    /// </summary>
    public enum DateSpinnerLightDismissBehavior
    {
        /// <summary>
        /// Discard the temporary date and keep the previously selected date. This is the default and matches the
        /// behavior of a dismissed WinUI flyout.
        /// </summary>
        Cancel,

        /// <summary>
        /// Commit the temporary date exactly as if the user had pressed Apply.
        /// </summary>
        Apply
    }
}
