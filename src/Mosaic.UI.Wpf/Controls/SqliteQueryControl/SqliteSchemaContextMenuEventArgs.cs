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
    /// Identifies which kind of node in the database explorer was right clicked.
    /// </summary>
    public enum SqliteSchemaNodeKind
    {
        /// <summary>
        /// No recognizable node was hit, or the click landed on empty space in the tree.
        /// </summary>
        None,

        /// <summary>
        /// The database root node.
        /// </summary>
        Database,

        /// <summary>
        /// The folder node that groups the tables.
        /// </summary>
        TablesFolder,

        /// <summary>
        /// The folder node that groups the views.
        /// </summary>
        ViewsFolder,

        /// <summary>
        /// An individual table.
        /// </summary>
        Table,

        /// <summary>
        /// An individual view.
        /// </summary>
        View,

        /// <summary>
        /// An individual column of a table or view.
        /// </summary>
        Field
    }

    /// <summary>
    /// Event data for <see cref="SqliteQueryControl.SchemaContextMenuRequested"/>. Raised after the
    /// control has populated the standard items for the clicked node but before the menu is shown,
    /// allowing consumers to append, remove, or reorder items.
    /// </summary>
    public class SqliteSchemaContextMenuEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteSchemaContextMenuEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The source of the event.</param>
        /// <param name="contextMenu">The context menu about to be displayed.</param>
        /// <param name="nodeKind">The kind of node that was right clicked.</param>
        /// <param name="node">The bound data item for the clicked node, when there is one.</param>
        /// <param name="objectName">The name of the table or view the click resolved to.</param>
        public SqliteSchemaContextMenuEventArgs(
            RoutedEvent routedEvent,
            object source,
            ContextMenu contextMenu,
            SqliteSchemaNodeKind nodeKind,
            object? node,
            string? objectName)
            : base(routedEvent, source)
        {
            this.ContextMenu = contextMenu;
            this.NodeKind = nodeKind;
            this.Node = node;
            this.ObjectName = objectName;
        }

        /// <summary>
        /// The context menu about to be displayed. Add or remove <see cref="MenuItem"/> /
        /// <see cref="Separator"/> entries from <see cref="ItemsControl.Items"/> to customize it.
        /// </summary>
        public ContextMenu ContextMenu { get; }

        /// <summary>
        /// The kind of node that was right clicked, useful for deciding which custom items to add.
        /// </summary>
        public SqliteSchemaNodeKind NodeKind { get; }

        /// <summary>
        /// The bound data item for the clicked node. This is a <see cref="SqliteTable"/>,
        /// <see cref="SqliteView"/> or <see cref="SqliteField"/> depending on <see cref="NodeKind"/>,
        /// and <see langword="null"/> for the database and folder nodes.
        /// </summary>
        public object? Node { get; }

        /// <summary>
        /// The name of the table or view the click resolved to. For a field this is the name of the
        /// table or view that owns it.
        /// </summary>
        public string? ObjectName { get; }

        /// <summary>
        /// Set this to <see langword="true"/> to suppress the menu entirely.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Invokes the strongly-typed handler. Required because this event uses a generic
        /// <see cref="EventHandler{TEventArgs}"/> delegate rather than the default
        /// <see cref="RoutedEventHandler"/>.
        /// </summary>
        /// <param name="genericHandler">The handler to invoke.</param>
        /// <param name="genericTarget">The target the handler is attached to.</param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            var handler = (EventHandler<SqliteSchemaContextMenuEventArgs>)genericHandler;
            handler(genericTarget, this);
        }
    }
}
