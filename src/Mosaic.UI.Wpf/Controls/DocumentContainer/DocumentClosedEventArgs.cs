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
    /// Provides data for a document closed event.
    /// </summary>
    public sealed class DocumentClosedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentClosedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with the event data.</param>
        /// <param name="source">The source that raised the event.</param>
        /// <param name="document">The document that was closed.</param>
        public DocumentClosedEventArgs(RoutedEvent routedEvent, object source, Document document)
            : base(routedEvent, source)
        {
            Document = document;
        }

        /// <summary>
        /// Gets the document that was closed.
        /// </summary>
        /// <value>The document associated with the completed close operation.</value>
        public Document Document { get; }
    }
}
