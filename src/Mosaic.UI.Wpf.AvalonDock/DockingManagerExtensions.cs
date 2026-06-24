using Mosaic.UI.Wpf.AvalonDock.Layout;
using System;
using System.Linq;
using System.Windows.Controls;
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
        public static LayoutDocument Add(this DockingManager dock, Control ctrl, string title, Color? tabColor = null, bool activate = true, bool canClose = true, bool moveToLast = true)
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
    }
}
