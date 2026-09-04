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

using System.Collections.ObjectModel;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents the method that handles the <see cref="MarkdownViewer.EventRaised"/> routed event.
    /// </summary>
    /// <param name="sender">The <see cref="MarkdownViewer"/> that raised the event.</param>
    /// <param name="e">The event data.</param>
    public delegate void MarkdownEventRaisedEventHandler(object sender, MarkdownEventRaisedEventArgs e);

    /// <summary>
    /// Provides data for the <see cref="MarkdownViewer.EventRaised"/> routed event, which fires when
    /// the user clicks an event link: a Markdown link whose destination starts with <c>@</c>, for
    /// example <c>[Blake's Articles](@ShowArticle?keyword=bpell)</c>. The text after the <c>@</c>
    /// and before the optional <c>?</c> becomes <see cref="EventName"/>; the query string is parsed
    /// into <see cref="Parameters"/>.
    /// </summary>
    public class MarkdownEventRaisedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownEventRaisedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier.</param>
        /// <param name="source">The source that raised the event.</param>
        /// <param name="eventName">The event name taken from the link, without the leading <c>@</c>.</param>
        /// <param name="parameters">The parsed query-string parameters. May be empty.</param>
        /// <param name="link">The original link destination, including the leading <c>@</c>.</param>
        public MarkdownEventRaisedEventArgs(RoutedEvent routedEvent, object source, string eventName, IReadOnlyDictionary<string, string> parameters, string link)
            : base(routedEvent, source)
        {
            EventName = eventName;
            Parameters = parameters;
            Link = link;
        }

        /// <summary>
        /// Gets the event name taken from the link destination, without the leading <c>@</c> or the
        /// query string. For <c>@ShowArticle?keyword=bpell</c> this is <c>ShowArticle</c>.
        /// </summary>
        public string EventName { get; }

        /// <summary>
        /// Gets the query-string parameters supplied with the event link, URL-decoded. For
        /// <c>@ShowArticle?keyword=bpell</c> this contains a single <c>keyword</c> entry. Keys are
        /// compared case-insensitively and a repeated key keeps its last value.
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters { get; }

        /// <summary>
        /// Gets the original link destination as written in the Markdown, including the leading <c>@</c>.
        /// </summary>
        public string Link { get; }

        /// <summary>
        /// Parses an event link destination such as <c>@ShowArticle?keyword=bpell&amp;page=2</c>
        /// into its event name and URL-decoded parameters.
        /// </summary>
        /// <param name="link">The link destination, including the leading <c>@</c>.</param>
        /// <param name="eventName">Receives the event name (the text between <c>@</c> and <c>?</c>).</param>
        /// <param name="parameters">Receives the parsed parameters; empty when there is no query string.</param>
        /// <returns><c>true</c> when the link is a well-formed event link with a non-empty name; otherwise <c>false</c>.</returns>
        public static bool TryParse(string? link, out string eventName, out IReadOnlyDictionary<string, string> parameters)
        {
            eventName = string.Empty;
            parameters = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            if (!MarkdownFlowDocumentRenderer.IsEventLink(link))
            {
                return false;
            }

            string body = link!.Substring(MarkdownFlowDocumentRenderer.EventLinkPrefix.Length);
            int queryIndex = body.IndexOf('?');
            string name = queryIndex < 0 ? body : body.Substring(0, queryIndex);
            string query = queryIndex < 0 ? string.Empty : body.Substring(queryIndex + 1);

            name = Uri.UnescapeDataString(name).Trim();

            if (name.Length == 0)
            {
                return false;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int equals = pair.IndexOf('=');
                string rawKey = equals < 0 ? pair : pair.Substring(0, equals);
                string rawValue = equals < 0 ? string.Empty : pair.Substring(equals + 1);

                string key = Uri.UnescapeDataString(rawKey.Replace('+', ' ')).Trim();

                if (key.Length == 0)
                {
                    continue;
                }

                values[key] = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
            }

            eventName = name;
            parameters = new ReadOnlyDictionary<string, string>(values);
            return true;
        }
    }
}
