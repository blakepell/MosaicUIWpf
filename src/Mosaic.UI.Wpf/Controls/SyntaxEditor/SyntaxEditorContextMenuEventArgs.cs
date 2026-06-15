/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Controls;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Event data for <see cref="SyntaxEditor.ContextMenuRequested"/>. Raised after the editor has
    /// populated the standard and language-specific menu items but before the context menu is shown,
    /// allowing consumers to append, remove, or otherwise customize the items.
    /// </summary>
    public class SyntaxEditorContextMenuEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxEditorContextMenuEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The source of the event.</param>
        /// <param name="contextMenu">The context menu about to be displayed.</param>
        /// <param name="language">The language currently active on the editor.</param>
        public SyntaxEditorContextMenuEventArgs(RoutedEvent routedEvent, object source, ContextMenu contextMenu, SyntaxLanguage language)
            : base(routedEvent, source)
        {
            this.ContextMenu = contextMenu;
            this.Language = language;
        }

        /// <summary>
        /// The context menu about to be displayed. Add or remove <see cref="MenuItem"/> /
        /// <see cref="Separator"/> entries from <see cref="ItemsControl.Items"/> to customize it.
        /// </summary>
        public ContextMenu ContextMenu { get; }

        /// <summary>
        /// The language currently active on the editor, useful for deciding which custom items to add.
        /// </summary>
        public SyntaxLanguage Language { get; }

        /// <summary>
        /// Invokes the strongly-typed handler. Required because this event uses a generic
        /// <see cref="EventHandler{TEventArgs}"/> delegate rather than the default
        /// <see cref="RoutedEventHandler"/>.
        /// </summary>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            var handler = (EventHandler<SyntaxEditorContextMenuEventArgs>)genericHandler;
            handler(genericTarget, this);
        }
    }
}
