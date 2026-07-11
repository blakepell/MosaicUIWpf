/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.Collections.Specialized;
using ListBox = System.Windows.Controls.ListBox;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Automatically scrolls an <see cref="ItemsControl"/> (e.g. a ListView) to its last item whenever
    /// new items are added, so the newest log entries stay in view.
    /// </summary>
    /// <remarks>
    /// Note: the behavior coalesces rapid collection changes into one dispatcher operation so bulk
    /// additions do not repeatedly force layout.
    /// </remarks>
    public class ItemsControlAutoScrollBehavior : Behavior<ItemsControl>
    {
        /// <summary>
        /// Identifies the <see cref="IsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(ItemsControlAutoScrollBehavior),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value that indicates whether the control scrolls to the end as items arrive.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the behavior scrolls to the end when items are added or reset;
        /// otherwise, <see langword="false" />. The default is <see langword="true" />.
        /// </value>
        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        private INotifyCollectionChanged? _items;

        /// <summary>
        /// True when a scroll-to-end has already been queued on the dispatcher but has not yet run.
        /// Lets a burst of adds (e.g. a bulk load) coalesce into a single scroll at the end.
        /// </summary>
        private bool _scrollQueued;

        /// <summary>
        /// Attaches collection change tracking to the associated control.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            _items = AssociatedObject.Items;
            _items.CollectionChanged += OnItemsChanged;
        }

        /// <summary>
        /// Detaches collection change tracking from the associated control.
        /// </summary>
        protected override void OnDetaching()
        {
            if (_items != null)
            {
                _items.CollectionChanged -= OnItemsChanged;
                _items = null;
            }

            base.OnDetaching();
        }

        /// <summary>
        /// Handles item collection changes that may require scrolling.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data for the collection change.</param>
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

            QueueScrollToEnd();
        }

        /// <summary>
        /// Schedules a single scroll-to-end on the dispatcher. If one is already pending (because many
        /// items are being added in quick succession), this is a no-op so a bulk load scrolls only
        /// once, after the last item has been added, rather than once per item.
        /// </summary>
        private void QueueScrollToEnd()
        {
            if (_scrollQueued)
            {
                return;
            }

            _scrollQueued = true;

            // Background priority runs after the pending item additions and their layout have completed.
            AssociatedObject.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    _scrollQueued = false;
                    ScrollToEnd();
                }),
                DispatcherPriority.Background);
        }

        /// <summary>
        /// Scrolls the associated control to its last item.
        /// </summary>
        private void ScrollToEnd()
        {
            var items = AssociatedObject.Items;

            if (items.Count == 0)
            {
                return;
            }

            var lastItem = items[items.Count - 1];

            if (AssociatedObject is ListBox listBox)
            {
                listBox.ScrollIntoView(lastItem);
            }
            else
            {
                FindScrollViewer(AssociatedObject)?.ScrollToBottom();
            }
        }

        /// <summary>
        /// Finds the first <see cref="ScrollViewer"/> in the visual tree.
        /// </summary>
        /// <param name="root">The visual tree root to search.</param>
        /// <returns>
        /// The first <see cref="ScrollViewer"/> found under <paramref name="root"/>, or <see langword="null" /> when no scroll viewer exists.
        /// </returns>
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
