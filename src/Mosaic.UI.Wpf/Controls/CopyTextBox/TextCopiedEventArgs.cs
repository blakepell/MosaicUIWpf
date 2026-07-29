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
    /// Event data for <see cref="CopyTextBox.TextCopied"/> which reports the outcome of a
    /// clipboard copy attempt.
    /// </summary>
    public class TextCopiedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Event data for <see cref="CopyTextBox.TextCopied"/>.
        /// </summary>
        /// <param name="routedEvent">The routed event that is being raised.</param>
        /// <param name="source">The object raising the event.</param>
        /// <param name="text">The text that was copied (or attempted to be copied).</param>
        /// <param name="successful">Whether the clipboard operation succeeded.</param>
        /// <param name="exception">The exception thrown by the clipboard, if any.</param>
        public TextCopiedEventArgs(RoutedEvent routedEvent, object source, string text, bool successful, Exception? exception = null)
            : base(routedEvent, source)
        {
            this.Text = text;
            this.Successful = successful;
            this.Exception = exception;
        }

        /// <summary>
        /// The text that was copied, or attempted to be copied.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Whether the clipboard operation succeeded.
        /// </summary>
        public bool Successful { get; }

        /// <summary>
        /// The exception the operating system's clipboard threw when <see cref="Successful"/> is
        /// false, otherwise null.
        /// </summary>
        public Exception? Exception { get; }
    }
}
