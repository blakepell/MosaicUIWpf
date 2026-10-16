using Mosaic.UI.Wpf.AvalonDock.Layout;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Mosaic.UI.Wpf.AvalonDock
{
    /// <summary>
    /// Convenience extension methods for hosting plain WPF controls inside a
    /// <see cref="DockingManager"/> without hand-building the layout tree.
    /// </summary>
    public static class DockingManagerExtensions
    {
        /// <summary>
        /// Adds a control as a document tab. The control is hosted on a new
        /// <see cref="LayoutDocument"/> inside the manager's document area.
        /// </summary>
        /// <param name="dock">The parent docking manager.</param>
        /// <param name="ctrl">The control to be shown in the tab.</param>
        /// <param name="title">The title of the tab.</param>
        /// <param name="tabColor">Optional color for the tab header. When <see langword="null"/> the themed default is used.</param>
        /// <param name="activate">If the tab should receive the active focus.</param>
        /// <param name="canClose">If the tab can be closed by the user.</param>
        /// <param name="moveToLast">If <see langword="true"/> the tab is appended as the last tab; otherwise it is inserted first.</param>
        /// <returns>The <see cref="LayoutDocument"/> that hosts <paramref name="ctrl"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dock"/> or <paramref name="ctrl"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The manager has no layout, or the layout contains no <see cref="LayoutDocumentPane"/>.</exception>
        public static LayoutDocument Add(this DockingManager dock, FrameworkElement ctrl, string title, Color? tabColor = null, bool activate = true, bool canClose = true, bool moveToLast = true)
        {
            ArgumentNullException.ThrowIfNull(dock);
            ArgumentNullException.ThrowIfNull(ctrl);

            var layout = dock.Layout
                ?? throw new InvalidOperationException("The DockingManager has no Layout assigned.");

            // Prefer the pane holding the last focused document so new tabs land next to the
            // user's current context; fall back to the first document pane in the tree.
            var documentPane = layout.LastFocusedDocument?.Parent as LayoutDocumentPane
                ?? layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault()
                ?? throw new InvalidOperationException("The layout must contain at least one LayoutDocumentPane to host documents.");

            var document = new LayoutDocument
            {
                Title = title,
                Content = ctrl,
                CanClose = canClose,
            };

            if (tabColor.HasValue)
            {
                var brush = new SolidColorBrush(tabColor.Value);
                brush.Freeze();
                document.TabBackground = brush;
            }

            if (moveToLast)
            {
                documentPane.Children.Add(document);
            }
            else
            {
                documentPane.Children.Insert(0, document);
            }

            if (activate)
            {
                document.IsActive = true;
            }

            return document;
        }

        /// <summary>
        /// Adds a control as a tool window. The control is hosted on a new
        /// <see cref="LayoutAnchorable"/> docked to the requested side of the layout.
        /// </summary>
        /// <param name="dock">The parent docking manager.</param>
        /// <param name="ctrl">The control to be shown in the tool window.</param>
        /// <param name="title">The title of the tool window.</param>
        /// <param name="strategy">
        /// The side to dock to. The tool window joins an existing pane on that side when one exists;
        /// otherwise a new pane is created at the edge of the layout.
        /// </param>
        /// <param name="activate">If the tool window should receive the active focus.</param>
        /// <param name="canClose">If the tool window can be closed by the user.</param>
        /// <param name="canHide">
        /// If the tool window's close button hides it (so it can be shown again) instead of closing it.
        /// </param>
        /// <returns>The <see cref="LayoutAnchorable"/> that hosts <paramref name="ctrl"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dock"/> or <paramref name="ctrl"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The manager has no layout.</exception>
        public static LayoutAnchorable AddToolWindow(this DockingManager dock, FrameworkElement ctrl, string title, AnchorableShowStrategy strategy = AnchorableShowStrategy.Right, bool activate = true, bool canClose = true, bool canHide = false)
        {
            ArgumentNullException.ThrowIfNull(dock);
            ArgumentNullException.ThrowIfNull(ctrl);

            if (dock.Layout == null)
            {
                throw new InvalidOperationException("The DockingManager has no Layout assigned.");
            }

            var anchorable = new LayoutAnchorable
            {
                Title = title,
                Content = ctrl,
                CanClose = canClose,
                CanHide = canHide,
            };

            anchorable.AddToLayout(dock, strategy);

            if (activate)
            {
                anchorable.IsActive = true;
            }

            return anchorable;
        }

        /// <summary>
        /// Returns the document hosting <paramref name="contentId"/>, or adds a new document built by
        /// <paramref name="factory"/> under that id when none is in the layout.
        /// </summary>
        /// <param name="dock">The parent docking manager.</param>
        /// <param name="contentId">The id used to find the document again; stored on <see cref="LayoutContent.ContentId"/>.</param>
        /// <param name="factory">Builds the control to host. Only called when no document with <paramref name="contentId"/> exists.</param>
        /// <param name="title">The title of the tab. Only applied when the document is created.</param>
        /// <param name="activate">
        /// If the tab should receive the active focus, whether it was found or created. When <see langword="false"/>
        /// the tab is still brought to the front of its pane, but keyboard focus is left where it is.
        /// </param>
        /// <param name="canClose">If the tab can be closed by the user. Only applied when the document is created.</param>
        /// <returns>The existing or new <see cref="LayoutDocument"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dock"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentId"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="contentId"/> belongs to content that is not a document.</exception>
        public static LayoutDocument GetOrAdd(this DockingManager dock, string contentId, Func<FrameworkElement> factory, string title, bool activate = true, bool canClose = true)
        {
            ArgumentNullException.ThrowIfNull(factory);

            switch (dock.FindContent(contentId))
            {
                case LayoutDocument existing:
                    Reveal(existing, activate);
                    return existing;
                case { } other:
                    throw new InvalidOperationException($"The content id '{contentId}' is already used by a {other.GetType().Name}.");
            }

            var document = dock.Add(factory(), title, activate: activate, canClose: canClose);
            document.ContentId = contentId;
            if (!activate)
            {
                document.IsSelected = true;
            }

            return document;
        }

        /// <summary>
        /// Returns the tool window hosting <paramref name="contentId"/>, showing it again if it was hidden,
        /// or adds a new tool window built by <paramref name="factory"/> under that id when none is in the layout.
        /// </summary>
        /// <param name="dock">The parent docking manager.</param>
        /// <param name="contentId">The id used to find the tool window again; stored on <see cref="LayoutContent.ContentId"/>.</param>
        /// <param name="factory">Builds the control to host. Only called when no tool window with <paramref name="contentId"/> exists.</param>
        /// <param name="title">The title of the tool window. Only applied when the tool window is created.</param>
        /// <param name="strategy">The side to dock a new tool window to. An existing tool window stays where it is.</param>
        /// <param name="activate">
        /// If the tool window should receive the active focus. When <see langword="false"/> the tool window
        /// is still brought to the front of its pane, but keyboard focus is left where it is.
        /// </param>
        /// <param name="canClose">If the tool window can be closed by the user. Only applied when the tool window is created.</param>
        /// <param name="canHide">
        /// If the tool window's close button hides it instead of closing it. Only applied when the tool window is created.
        /// </param>
        /// <returns>The existing or new <see cref="LayoutAnchorable"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dock"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentId"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="contentId"/> belongs to content that is not a tool window.</exception>
        public static LayoutAnchorable GetOrAddToolWindow(this DockingManager dock, string contentId, Func<FrameworkElement> factory, string title, AnchorableShowStrategy strategy = AnchorableShowStrategy.Right, bool activate = true, bool canClose = true, bool canHide = false)
        {
            ArgumentNullException.ThrowIfNull(factory);

            switch (dock.FindContent(contentId))
            {
                case LayoutAnchorable existing:
                    Reveal(existing, activate);
                    return existing;
                case { } other:
                    throw new InvalidOperationException($"The content id '{contentId}' is already used by a {other.GetType().Name}.");
            }

            var anchorable = dock.AddToolWindow(factory(), title, strategy, activate: activate, canClose: canClose, canHide: canHide);
            anchorable.ContentId = contentId;
            if (!activate)
            {
                anchorable.IsSelected = true;
            }

            return anchorable;
        }

        /// <summary>
        /// Finds the document or tool window with the given <see cref="LayoutContent.ContentId"/>, including
        /// floating, auto-hidden, and hidden tool windows.
        /// </summary>
        /// <param name="dock">The parent docking manager.</param>
        /// <param name="contentId">The id to look for. The comparison is ordinal.</param>
        /// <returns>The matching content, or <see langword="null"/> when none is in the layout.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dock"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentId"/> is null or whitespace.</exception>
        public static LayoutContent? FindContent(this DockingManager dock, string contentId)
        {
            ArgumentNullException.ThrowIfNull(dock);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentId);

            return dock.Layout?.Descendents()
                .OfType<LayoutContent>()
                .FirstOrDefault(c => string.Equals(c.ContentId, contentId, StringComparison.Ordinal));
        }

        /// <summary>
        /// Brings the document or tool window with the given <see cref="LayoutContent.ContentId"/> into
        /// view, showing a hidden tool window again.
        /// </summary>
        /// <param name="dock">The parent docking manager.</param>
        /// <param name="contentId">The id to look for. The comparison is ordinal.</param>
        /// <param name="activate">
        /// If the content should receive the active focus. When <see langword="false"/> it is brought to
        /// the front of its pane, but keyboard focus is left where it is.
        /// </param>
        /// <returns><see langword="true"/> when the content was found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dock"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentId"/> is null or whitespace.</exception>
        public static bool ShowContent(this DockingManager dock, string contentId, bool activate = true)
        {
            var content = dock.FindContent(contentId);
            if (content == null)
            {
                return false;
            }

            Reveal(content, activate);
            return true;
        }

        /// <summary>
        /// Shows <paramref name="content"/> if it is a hidden tool window, then selects or activates it.
        /// </summary>
        private static void Reveal(LayoutContent content, bool activate)
        {
            if (content is LayoutAnchorable { IsHidden: true } anchorable)
            {
                anchorable.Show();
            }

            if (activate)
            {
                content.IsActive = true;
            }
            else
            {
                content.IsSelected = true;
            }
        }
    }
}
