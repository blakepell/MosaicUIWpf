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
    /// Represents the method that handles the <see cref="MarkdownViewer.LinkClicked"/> routed event.
    /// </summary>
    /// <param name="sender">The <see cref="MarkdownViewer"/> that raised the event.</param>
    /// <param name="e">The event data.</param>
    public delegate void MarkdownLinkClickedEventHandler(object sender, MarkdownLinkClickedEventArgs e);

    /// <summary>
    /// Provides data for the <see cref="MarkdownViewer.LinkClicked"/> routed event. Set
    /// <see cref="RoutedEventArgs.Handled"/> to <c>true</c> to suppress the viewer's default
    /// navigation (opening external links in the browser or navigating to another Markdown
    /// resource), for example to route custom schemes such as <c>app:settings</c> to pages
    /// within the application.
    /// </summary>
    public class MarkdownLinkClickedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownLinkClickedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier.</param>
        /// <param name="source">The source that raised the event.</param>
        /// <param name="uri">The link target. May be relative when no base document is loaded.</param>
        public MarkdownLinkClickedEventArgs(RoutedEvent routedEvent, object source, Uri uri)
            : base(routedEvent, source)
        {
            Uri = uri;
        }

        /// <summary>
        /// Gets the link target. Relative links are resolved against the viewer's
        /// <see cref="MarkdownViewer.Source"/> when one is set; otherwise the URI is relative.
        /// </summary>
        public Uri Uri { get; }
    }
}
