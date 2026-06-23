using AvalonDock.Layout;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Represents the document pane drop as anchorable target.
    /// </summary>
    internal class DocumentPaneDropAsAnchorableTarget : DropTarget<LayoutDocumentPaneControl>
    {
        private LayoutDocumentPaneControl _targetPane;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentPaneDropAsAnchorableTarget"/> class.
        /// </summary>
        /// <param name="paneControl">The pane control.</param>
        /// <param name="detectionRect">The detection rectangle.</param>
        /// <param name="type">The drop target type.</param>
        internal DocumentPaneDropAsAnchorableTarget(
            LayoutDocumentPaneControl paneControl,
            Rect detectionRect,
            DropTargetType type)
            : base(paneControl, detectionRect, type)
        {
            _targetPane = paneControl;
        }

        /// <inheritdoc/>
        protected override void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
            ILayoutDocumentPane? targetModel = _targetPane.Model as ILayoutDocumentPane;
            if (!FindParentLayoutDocumentPane(targetModel, out var parentGroup, out var parentGroupPanel))
            {
                return;
            }

            var targetChild = parentGroup ?? (ILayoutPanelElement?)targetModel;
            if (targetChild == null)
            {
                return;
            }

            switch (Type)
            {
                case DropTargetType.DocumentPaneDockAsAnchorableBottom:
                    {
                        if (parentGroupPanel is { ChildrenCount: 1 })
                        {
                            parentGroupPanel.Orientation = Orientation.Vertical;
                        }

                        if (parentGroupPanel is { Orientation: Orientation.Vertical })
                        {
                            parentGroupPanel.Children.Insert(
                                parentGroupPanel.IndexOfChild(targetChild) + 1,
                                floatingWindow.RootPanel);
                        }
                        else if (parentGroupPanel != null)
                        {
                            var newParentPanel = new LayoutPanel { Orientation = Orientation.Vertical };
                            parentGroupPanel.ReplaceChild(targetChild, newParentPanel);
                            newParentPanel.Children.Add(targetChild);
                            newParentPanel.Children.Add(floatingWindow.RootPanel);
                        }
                    }

                    break;

                case DropTargetType.DocumentPaneDockAsAnchorableTop:
                    {
                        if (parentGroupPanel is { ChildrenCount: 1 })
                        {
                            parentGroupPanel.Orientation = Orientation.Vertical;
                        }

                        if (parentGroupPanel is { Orientation: Orientation.Vertical })
                        {
                            parentGroupPanel.Children.Insert(
                                parentGroupPanel.IndexOfChild(targetChild),
                                floatingWindow.RootPanel);
                        }
                        else if (parentGroupPanel != null)
                        {
                            var newParentPanel = new LayoutPanel { Orientation = Orientation.Vertical };
                            parentGroupPanel.ReplaceChild(targetChild, newParentPanel);
                            newParentPanel.Children.Add(targetChild);
                            newParentPanel.Children.Insert(0, floatingWindow.RootPanel);
                        }
                    }

                    break;

                case DropTargetType.DocumentPaneDockAsAnchorableLeft:
                    {
                        if (parentGroupPanel is { ChildrenCount: 1 })
                        {
                            parentGroupPanel.Orientation = Orientation.Horizontal;
                        }

                        if (parentGroupPanel is { Orientation: Orientation.Horizontal })
                        {
                            parentGroupPanel.Children.Insert(
                                parentGroupPanel.IndexOfChild(targetChild),
                                floatingWindow.RootPanel);
                        }
                        else if (parentGroupPanel != null)
                        {
                            var newParentPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
                            parentGroupPanel.ReplaceChild(targetChild, newParentPanel);
                            newParentPanel.Children.Add(targetChild);
                            newParentPanel.Children.Insert(0, floatingWindow.RootPanel);
                        }
                    }

                    break;

                case DropTargetType.DocumentPaneDockAsAnchorableRight:
                    {
                        if (parentGroupPanel is { ChildrenCount: 1 })
                        {
                            parentGroupPanel.Orientation = Orientation.Horizontal;
                        }

                        if (parentGroupPanel is { Orientation: Orientation.Horizontal })
                        {
                            parentGroupPanel.Children.Insert(
                                parentGroupPanel.IndexOfChild(targetChild) + 1,
                                floatingWindow.RootPanel);
                        }
                        else if (parentGroupPanel != null)
                        {
                            var newParentPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
                            parentGroupPanel.ReplaceChild(targetChild, newParentPanel);
                            newParentPanel.Children.Add(targetChild);
                            newParentPanel.Children.Add(floatingWindow.RootPanel);
                        }
                    }

                    break;
            }

            base.Drop(floatingWindow);
        }

        /// <inheritdoc/>
        public override Geometry GetPreviewPath(OverlayWindow overlayWindow, LayoutFloatingWindow floatingWindowModel)
        {
            Rect targetScreenRect;
            ILayoutDocumentPane? targetModel = _targetPane.Model as ILayoutDocumentPane;
            var manager = targetModel?.Root?.Manager;
            if (manager == null)
            {
                return Geometry.Empty;
            }

            // ILayoutDocumentPane targetModel = _targetPane.Model as ILayoutDocumentPane;
            if (!FindParentLayoutDocumentPane(targetModel, out var parentGroup, out var parentGroupPanel))
            {
                return Geometry.Empty;
            }

            // if (targetModel.Parent is LayoutDocumentPaneGroup)
            // {
            //    var parentGroup = targetModel.Parent as LayoutDocumentPaneGroup;
            //    var documentPaneGroupControl = manager.FindLogicalChildren<LayoutDocumentPaneGroupControl>().First(d => d.Model == parentGroup);
            //    targetScreenRect = documentPaneGroupControl.GetScreenArea();
            // }
            // else
            // {
            //    var documentPaneControl = manager.FindLogicalChildren<LayoutDocumentPaneControl>().First(d => d.Model == targetModel);
            //    targetScreenRect = documentPaneControl.GetScreenArea();
            // }

            // var parentPanel = targetModel.FindParent<LayoutPanel>();
            var modelToFind = parentGroup ?? (object?)parentGroupPanel;
            var documentPaneControl = manager.FindLogicalChildren<FrameworkElement>()
                .OfType<ILayoutControl>()
                .FirstOrDefault(d => d.Model == modelToFind) as FrameworkElement;
            if (documentPaneControl == null)
            {
                return Geometry.Empty;
            }

            targetScreenRect = documentPaneControl.GetScreenArea();

            switch (Type)
            {
                case DropTargetType.DocumentPaneDockAsAnchorableBottom:
                    {
                        targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                        targetScreenRect.Offset(0.0, targetScreenRect.Height - targetScreenRect.Height / 3.0);
                        targetScreenRect.Height /= 3.0;
                        return new RectangleGeometry(targetScreenRect);
                    }

                case DropTargetType.DocumentPaneDockAsAnchorableTop:
                    {
                        targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                        targetScreenRect.Height /= 3.0;
                        return new RectangleGeometry(targetScreenRect);
                    }

                case DropTargetType.DocumentPaneDockAsAnchorableRight:
                    {
                        targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                        targetScreenRect.Offset(targetScreenRect.Width - targetScreenRect.Width / 3.0, 0.0);
                        targetScreenRect.Width /= 3.0;
                        return new RectangleGeometry(targetScreenRect);
                    }

                case DropTargetType.DocumentPaneDockAsAnchorableLeft:
                    {
                        targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                        targetScreenRect.Width /= 3.0;
                        return new RectangleGeometry(targetScreenRect);
                    }
            }

            return Geometry.Empty;
        }

        private bool FindParentLayoutDocumentPane(ILayoutDocumentPane? documentPane, out LayoutDocumentPaneGroup? containerPaneGroup, out LayoutPanel? containerPanel)
        {
            containerPaneGroup = null;
            containerPanel = null;

            if (documentPane == null)
            {
                return false;
            }

            if (documentPane.Parent is LayoutPanel panel)
            {
                containerPaneGroup = null;
                containerPanel = panel;
                return true;
            }

            if (documentPane.Parent is LayoutDocumentPaneGroup currentDocumentPaneGroup)
            {
                while (!(currentDocumentPaneGroup.Parent is LayoutPanel))
                {
                    currentDocumentPaneGroup = currentDocumentPaneGroup.Parent as LayoutDocumentPaneGroup;

                    if (currentDocumentPaneGroup == null)
                    {
                        break;
                    }
                }

                if (currentDocumentPaneGroup == null)
                {
                    return false;
                }

                containerPaneGroup = currentDocumentPaneGroup;
                containerPanel = currentDocumentPaneGroup.Parent as LayoutPanel;
                return containerPanel != null;
            }

            return false;
        }
    }
}
