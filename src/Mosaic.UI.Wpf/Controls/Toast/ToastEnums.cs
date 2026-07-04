/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The severity of a <see cref="ToastMessage"/>, which determines its color scheme and icon.
    /// </summary>
    public enum ToastSeverity
    {
        /// <summary>
        /// A successful or positive notification.
        /// </summary>
        Success,

        /// <summary>
        /// A neutral, informational notification.
        /// </summary>
        Info,

        /// <summary>
        /// A cautionary notification.
        /// </summary>
        Warning,

        /// <summary>
        /// An error or critical notification.
        /// </summary>
        Error
    }

    /// <summary>
    /// The quadrant of the host surface a toast is displayed in.  Toasts are aligned
    /// to the corner of their quadrant (the closest sides of the screen).
    /// </summary>
    public enum ToastQuadrant
    {
        /// <summary>
        /// The upper left corner.
        /// </summary>
        TopLeft,

        /// <summary>
        /// The upper right corner.
        /// </summary>
        TopRight,

        /// <summary>
        /// The lower left corner.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// The lower right corner.
        /// </summary>
        BottomRight
    }

    /// <summary>
    /// Describes why a <see cref="ToastMessage"/> was dismissed.
    /// </summary>
    public enum ToastDismissReason
    {
        /// <summary>
        /// The toast's display duration elapsed.
        /// </summary>
        Timeout,

        /// <summary>
        /// The user clicked the close button.
        /// </summary>
        ClosedByUser,

        /// <summary>
        /// The toast was dismissed from code.
        /// </summary>
        Programmatic
    }

    /// <summary>
    /// Event data for <see cref="ToastMessage.Dismissed"/>.
    /// </summary>
    public class ToastDismissedEventArgs : EventArgs
    {
        /// <summary>
        /// Event data for <see cref="ToastMessage.Dismissed"/>.
        /// </summary>
        /// <param name="reason">Why the toast was dismissed.</param>
        public ToastDismissedEventArgs(ToastDismissReason reason)
        {
            this.Reason = reason;
        }

        /// <summary>
        /// Why the toast was dismissed.
        /// </summary>
        public ToastDismissReason Reason { get; }
    }
}
