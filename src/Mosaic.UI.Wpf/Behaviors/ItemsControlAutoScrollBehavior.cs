/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Specialized;
using Microsoft.Xaml.Behaviors;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Scrolls the <see cref="ItemsControl"/> to the last item when the collection changes.
    /// </summary>
    public class ItemsControlAutoScrollBehavior : Behavior<ItemsControl>
    {
        private INotifyCollectionChanged? _itemsCollection;

        /// <summary>
        /// Event for when the behavior is attached where we can set up events and tracking code.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            // Subscribe to the CollectionChanged event of the ItemsControl's items
            _itemsCollection = AssociatedObject.Items as INotifyCollectionChanged;

            if (_itemsCollection != null)
            {
                _itemsCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        /// <summary>
        /// Event for When the behavior is detached where we will cleanup resources.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            // Unsubscribe from the CollectionChanged event
            if (_itemsCollection != null)
            {
                _itemsCollection.CollectionChanged -= OnCollectionChanged;
                _itemsCollection = null;
            }
        }

        /// <summary>
        /// Event for when the collection change.s
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ScrollToLastItem();
        }

        /// <summary>
        /// Scrolls the last item into view.
        /// </summary>
        private void ScrollToLastItem()
        {
            try
            {
                // Ensure the last item is scrolled into view after the UI updates
                AssociatedObject.Dispatcher.BeginInvoke(() =>
                {
                    // Each type of control has to be handled here.
                    if (AssociatedObject is ListView { Items.Count: > 0 } lv)
                    {
                        lv.ScrollToLastItem();
                    }
                    else if (AssociatedObject is ListBox { Items.Count: > 0 } lb)
                    {
                        lb.ScrollIntoView(lb.Items[^1]);
                    }
                    else if (AssociatedObject is ItemsControl { Items.Count: > 0 } ic)
                    {
                        VirtualizedScrollIntoView(ic, ic.Items[^1]);
                    }
                    else if (AssociatedObject.Items.Count > 0)
                    {
                        var lastItem = AssociatedObject.Items[^1];

                        if (AssociatedObject.ItemContainerGenerator.ContainerFromItem(lastItem) is FrameworkElement container)
                        {
                            container.BringIntoView();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        public static void VirtualizedScrollIntoView(ItemsControl control, object item)
        {
            // this is basically getting a reference to the ScrollViewer defined in the ItemsControl's style (identified above).
            // you *could* enumerate over the ItemsControl's children until you hit a scroll viewer, but this is quick and
            // dirty!
            // First 0 in the GetChild returns the Border from the ControlTemplate, and the second 0 gets the ScrollViewer from
            // the Border.
            try
            {
                if (control == null || !control.HasItems || item == null)
                {
                    return;
                }

                if (VisualTreeHelper.GetChildrenCount(control) == 0)
                {
                    return;
                }

                ScrollViewer? sv = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild((DependencyObject)control, 0), 0) as ScrollViewer;

                if (sv == null)
                {
                    return;
                }

                // now get the index of the item your passing in
                int index = control.Items.IndexOf(item);
                if (index != -1)
                {
                    // since the scroll viewer is using content scrolling not pixel based scrolling we just tell it to scroll to the index of the item
                    // and viola!  we scroll there!
                    sv.ScrollToVerticalOffset(index);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
    }
}