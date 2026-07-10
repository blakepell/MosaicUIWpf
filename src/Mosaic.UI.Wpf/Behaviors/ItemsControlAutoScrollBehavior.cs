/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Specialized;
using Microsoft.Xaml.Behaviors;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Automatically scrolls an <see cref="ItemsControl"/> (e.g. a ListView) to its last item whenever
    /// new items are added, so the newest log entries stay in view.
    /// </summary>
    public class ItemsControlAutoScrollBehavior : Behavior<ItemsControl>
    {
        /// <summary>
        /// When <c>true</c> (the default) the control auto-scrolls to the end as items arrive; when
        /// <c>false</c> the behavior does nothing.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(ItemsControlAutoScrollBehavior),
            new PropertyMetadata(true));

        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        private INotifyCollectionChanged? _items;

        protected override void OnAttached()
        {
            base.OnAttached();

            _items = AssociatedObject.Items;
            _items.CollectionChanged += OnItemsChanged;
        }

        protected override void OnDetaching()
        {
            if (_items != null)
            {
                _items.CollectionChanged -= OnItemsChanged;
                _items = null;
            }

            base.OnDetaching();
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Exit early when disabled, or when the change isn't one that adds items.
            if (!IsEnabled)
            {
                return;
            }

            if (e.Action != NotifyCollectionChangedAction.Add && e.Action != NotifyCollectionChangedAction.Reset)
            {
                return;
            }

            ScrollToEnd();
        }

        private void ScrollToEnd()
        {
            var items = AssociatedObject.Items;

            if (items.Count == 0)
            {
                return;
            }

            var lastItem = items[items.Count - 1];

            // Defer until after the new container has been generated and laid out.
            AssociatedObject.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (AssociatedObject is ListBox listBox)
                    {
                        listBox.ScrollIntoView(lastItem);
                    }
                    else
                    {
                        FindScrollViewer(AssociatedObject)?.ScrollToBottom();
                    }
                }),
                DispatcherPriority.Background);
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject root)
        {
            if (root is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                var result = FindScrollViewer(VisualTreeHelper.GetChild(root, i));

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}