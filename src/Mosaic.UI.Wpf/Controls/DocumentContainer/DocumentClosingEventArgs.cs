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
    /// Provides data for a document closing event.
    /// </summary>
    public sealed class DocumentClosingEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClosingEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with the event data.</param>
        /// <param name="source">The source that raised the event.</param>
        /// <param name="document">The document that is being closed.</param>
        public DocumentClosingEventArgs(RoutedEvent routedEvent, object source, Document document)
            : base(routedEvent, source)
        {
            Document = document;
        }

        /// <summary>
        /// Gets the document that is being closed.
        /// </summary>
        /// <value>The document associated with the close request.</value>
        public Document Document { get; }

        /// <summary>
        /// Gets or sets a value that indicates whether the close operation should be canceled.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to cancel the close operation; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool Cancel { get; set; }
    }
}
