using Mosaic.UI.Wpf.Controls;

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Provides attached properties and event handlers for preserving tree view scroll position.
    /// </summary>
    public class TreeViewHelper
    {
        /// <summary>
        /// Identifies the stored mouse-handler attached property.
        /// </summary>
        private static readonly DependencyProperty MouseHandlerProperty =
            DependencyProperty.RegisterAttached("MouseHandler", typeof(MouseButtonEventHandler), typeof(TreeViewHelper), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the stored expand-handler attached property.
        /// </summary>
        private static readonly DependencyProperty ExpandHandlerProperty =
            DependencyProperty.RegisterAttached("ExpandHandler", typeof(RoutedEventHandler), typeof(TreeViewHelper), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the attached property that enables scroll animation integration.
        /// </summary>
        public static readonly DependencyProperty IsScrollAnimationProperty =
            DependencyProperty.RegisterAttached("IsScrollAnimation", typeof(bool), typeof(TreeViewHelper),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets the scroll animation setting.
        /// </summary>
        /// <param name="obj">The object that owns the attached property.</param>
        /// <returns>The current attached property value.</returns>
        public static object GetIsScrollAnimation(DependencyObject obj)
        {
            return obj.GetValue(IsScrollAnimationProperty);
        }

        /// <summary>
        /// Sets the scroll animation setting.
        /// </summary>
        /// <param name="obj">The object that owns the attached property.</param>
        /// <param name="value">The value to store in the attached property.</param>
        public static void SetIsScrollAnimation(DependencyObject obj, object value)
        {
            obj.SetValue(IsScrollAnimationProperty, value);
        }

        /// <summary>
        /// Identifies the attached property that preserves the scroll position during expansion.
        /// </summary>
        public static readonly DependencyProperty PreserveScrollOnExpandProperty =
            DependencyProperty.RegisterAttached(
                "PreserveScrollOnExpand",
                typeof(bool),
                typeof(TreeViewHelper),
                new PropertyMetadata(false, OnPreserveScrollOnExpandChanged));

        /// <summary>
        /// Sets a value that indicates whether tree view expansion should preserve scroll position.
        /// </summary>
        /// <param name="element">The object that owns the attached property.</param>
        /// <param name="value">
        /// <see langword="true" /> to preserve the scroll position; otherwise, <see langword="false" />.
        /// </param>
        public static void SetPreserveScrollOnExpand(DependencyObject element, bool value) =>
           element.SetValue(PreserveScrollOnExpandProperty, value);

        /// <summary>
        /// Gets a value that indicates whether tree view expansion should preserve scroll position.
        /// </summary>
        /// <param name="element">The object that owns the attached property.</param>
        /// <returns>
        /// <see langword="true" /> if scroll position is preserved during expansion; otherwise, <see langword="false" />.
        /// </returns>
        public static bool GetPreserveScrollOnExpand(DependencyObject element) =>
            (bool)element.GetValue(PreserveScrollOnExpandProperty);


        /// <summary>
        /// Identifies the attached property that stores the most recent scroll offset.
        /// </summary>
        public static readonly DependencyProperty LastOffsetProperty =
            DependencyProperty.RegisterAttached("LastOffset", typeof(double), typeof(TreeViewHelper), new PropertyMetadata(0.0));

        /// <summary>
        /// Stores the most recent scroll offset for the specified object.
        /// </summary>
        /// <param name="obj">The object that owns the attached property.</param>
        /// <param name="value">The vertical offset to store.</param>
        internal static void SetLastOffset(DependencyObject obj, double value) => obj.SetValue(LastOffsetProperty, value);

        /// <summary>
        /// Gets the most recent scroll offset for the specified object.
        /// </summary>
        /// <param name="obj">The object that owns the attached property.</param>
        /// <returns>The stored vertical offset.</returns>
        public static double GetLastOffset(DependencyObject obj) => (double)obj.GetValue(LastOffsetProperty);

        /// <summary>
        /// Attaches or detaches tree view handlers that preserve scroll position across expansion.
        /// </summary>
        /// <param name="d">The dependency object that owns the attached property.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnPreserveScrollOnExpandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                if ((bool)e.NewValue)
                {
                    var mouseHandler = new MouseButtonEventHandler(OnMouseDown);
                    var expandHandler = new RoutedEventHandler(OnItemExpanded);

                    treeView.SetValue(MouseHandlerProperty, mouseHandler);
                    treeView.SetValue(ExpandHandlerProperty, expandHandler);

                    treeView.AddHandler(TreeViewItem.PreviewMouseLeftButtonDownEvent, mouseHandler, true);
                    treeView.AddHandler(TreeViewItem.ExpandedEvent, expandHandler, true);
                }
                else
                {
                    if (treeView.GetValue(MouseHandlerProperty) is MouseButtonEventHandler mouseHandler)
                    {
                        treeView.RemoveHandler(TreeViewItem.PreviewMouseLeftButtonDownEvent, mouseHandler);
                    }

                    if (treeView.GetValue(ExpandHandlerProperty) is RoutedEventHandler expandHandler)
                    {
                        treeView.RemoveHandler(TreeViewItem.ExpandedEvent, expandHandler);
                    }
                }
            }
        }

        /// <summary>
        /// Captures the current vertical scroll offset before a tree item expands.
        /// </summary>
        /// <param name="sender">The source tree view.</param>
        /// <param name="args">The event data for the mouse event.</param>
        private static void OnMouseDown(object sender, MouseButtonEventArgs args)
        {
            if (sender is TreeView treeView)
            {
                var viewer = ControlsHelper.FindVisualChild<ScrollViewer>(treeView);
                if (viewer != null)
                {
                    SetLastOffset(treeView, viewer.VerticalOffset);
                }
            }
        }

        /// <summary>
        /// Restores the saved vertical scroll offset after a tree item expands.
        /// </summary>
        /// <param name="sender">The source tree view.</param>
        /// <param name="args">The event data for the expansion event.</param>
        private static void OnItemExpanded(object sender, RoutedEventArgs args)
        {
            if (sender is TreeView treeView)
            {
                var scrollViewer = ControlsHelper.FindVisualChild<ScrollViewer>(treeView);
                if (scrollViewer == null)
                {
                    return;
                }

                var lastOffset = GetLastOffset(treeView);
                treeView.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    if (scrollViewer is WDScrollViewer wd)
                    {
                        wd.AnimateScroll(lastOffset);
                    }
                    else
                    {
                        scrollViewer.ScrollToVerticalOffset(lastOffset);
                    }
                }));
            }
        }
    }
}