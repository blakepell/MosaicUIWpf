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
    /// Carries the text a <see cref="CommandBox"/> is dispatching for
    /// <see cref="CommandBox.PreviewCommandExecuted"/> and <see cref="CommandBox.CommandExecuted"/>.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="RoutedEventArgs.Handled"/> on the tunneling
    /// <see cref="CommandBox.PreviewCommandExecuted"/> event vetoes the command entirely: the commit
    /// behavior is not applied, nothing is added to the history and neither
    /// <see cref="CommandBox.CommandExecuted"/> nor <see cref="CommandBox.Command"/> fires.
    /// </remarks>
    public class CommandExecutedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Backing field for <see cref="Command"/>.
        /// </summary>
        private string _command;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The <see cref="CommandBox"/> raising the event.</param>
        /// <param name="command">The normalized, single-line command text.</param>
        public CommandExecutedEventArgs(RoutedEvent routedEvent, object source, string command)
            : base(routedEvent, source)
        {
            _command = Normalize(command);
        }

        /// <summary>
        /// The command text being dispatched, already collapsed to a single line and (unless
        /// <see cref="CommandBox.TrimCommand"/> is disabled) trimmed.
        /// </summary>
        /// <remarks>
        /// A handler of the tunneling <see cref="CommandBox.PreviewCommandExecuted"/> event may
        /// assign this property to rewrite the command before it is dispatched, recorded in the
        /// history or handed to <see cref="CommandBox.Command"/>. Any line breaks in the assigned
        /// value are collapsed to spaces. Assigning it from a <see cref="CommandBox.CommandExecuted"/>
        /// handler has no effect on what was already recorded.
        /// </remarks>
        public string Command
        {
            get => _command;
            set => _command = Normalize(value);
        }

        /// <summary>
        /// Whether the command should be recorded in the <see cref="CommandBox.History"/>. Defaults
        /// to <see langword="true"/>; a handler can set it to <see langword="false"/> to keep a
        /// one-off command (a password prompt response, for instance) out of the history.
        /// </summary>
        /// <remarks>
        /// The value is read after <see cref="CommandBox.CommandExecuted"/> has been raised, so
        /// either event handler can opt the command out.
        /// </remarks>
        public bool AddToHistory { get; set; } = true;

        /// <summary>
        /// Collapses line breaks to single spaces so the command is always one line.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        private static string Normalize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
