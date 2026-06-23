using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Represents the tab Control Ex.
    /// </summary>
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]
    public class TabControlEx : TabControl
    {
        private Panel ItemsHolderPanel;
        private readonly bool _IsVirtualizing;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabControlEx"/> class.
        /// </summary>
        /// <param name="isVirtualizing">The is Virtualizing.</param>
        public TabControlEx(bool isVirtualizing)
            : this()
        {
            _IsVirtualizing = isVirtualizing;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabControlEx"/> class.
        /// </summary>
        protected TabControlEx()
        {
            _IsVirtualizing = true;

            // This is necessary so that we get the initial databound selected item
            ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        /// <summary>
        /// Gets a value indicating whether is Virtualiting.
        /// </summary>
        [Bindable(false)]
        [Description("Gets whether the control and its inheriting classes are virtualizing their items or not.")]
        [Category("Other")]
        public bool IsVirtualiting => _IsVirtualizing;

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Code below is required only if virtualization is turned ON
            if (_IsVirtualizing)
            {
                return;
            }

            ItemsHolderPanel = CreateGrid();
            // exchange ContentPresenter for Grid
            var topGrid = (Grid)GetVisualChild(0);

            if (topGrid is { Children.Count: > 2 })
            {
                if (topGrid.Children[1] is Border)
                {
                    var border = (Border)topGrid.Children[1];
                    border.Child = ItemsHolderPanel;
                }
                else if (topGrid.Children[2] is Border)
                {
                    var border = (Border)topGrid.Children[2];
                    border.Child = ItemsHolderPanel;
                }
            }

            UpdateSelectedItem();
        }

        /// <inheritdoc/>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            // Code below is required only if virtualization is turned ON
            if (_IsVirtualizing)
            {
                return;
            }

            if (ItemsHolderPanel == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    ItemsHolderPanel.Children.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            ContentPresenter cp = FindChildContentPresenter(item);
                            if (cp != null)
                            {
                                ItemsHolderPanel.Children.Remove(cp);
                            }
                        }
                    }

                    // Don't do anything with new items because we don't want to
                    // create visuals that aren't being shown
                    UpdateSelectedItem();
                    break;

                case NotifyCollectionChangedAction.Move:
                    UpdateSelectedItem();
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            // Code below is required only if virtualization is turned ON
            if (_IsVirtualizing)
            {
                return;
            }

            UpdateSelectedItem();
        }

        /// <summary>
        /// Gets the get Selected Tab Item.
        /// </summary>
        /// <returns>The requested value.</returns>
        protected TabItem GetSelectedTabItem()
        {
            object selectedItem = SelectedItem;

            // Code below is required only if virtualization is turned ON
            if (_IsVirtualizing)
            {
                return selectedItem as TabItem;
            }

            if (selectedItem == null)
            {
                return null;
            }

            if (selectedItem is not TabItem item)
            {
                item = ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as TabItem;
            }

            return item;
        }

        /// <summary>
        /// Executes the item Container Generator Status Changed operation.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                UpdateSelectedItem();
            }
        }

        private Grid CreateGrid()
        {
            var grid = new Grid();
            Binding binding = new Binding(PaddingProperty.Name);
            binding.Source = this;  // view model?
            grid.SetBinding(MarginProperty, binding);

            binding = new Binding(SnapsToDevicePixelsProperty.Name);
            binding.Source = this;  // view model?
            grid.SetBinding(SnapsToDevicePixelsProperty, binding);

            return grid;
        }

        private void UpdateSelectedItem()
        {
            if (ItemsHolderPanel == null)
            {
                return;
            }

            // Generate a ContentPresenter if necessary
            TabItem item = GetSelectedTabItem();
            if (item != null)
            {
                CreateChildContentPresenter(item);
            }

            // show the right child
            foreach (ContentPresenter child in ItemsHolderPanel.Children)
            {
                child.Visibility = (child.Tag as TabItem).IsSelected ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private ContentPresenter CreateChildContentPresenter(object? item)
        {
            if (item == null)
            {
                return null;
            }

            ContentPresenter cp = FindChildContentPresenter(item);

            if (cp != null)
            {
                return cp;
            }

            // the actual child to be added.  cp.Tag is a reference to the TabItem
            cp = new ContentPresenter();
            cp.Content = (item is TabItem tabItem) ? tabItem.Content : item;
            cp.ContentTemplate = SelectedContentTemplate;
            cp.ContentTemplateSelector = SelectedContentTemplateSelector;
            cp.ContentStringFormat = SelectedContentStringFormat;
            cp.Visibility = Visibility.Collapsed;
            cp.Tag = (item is TabItem) ? item : ItemContainerGenerator.ContainerFromItem(item);
            ItemsHolderPanel.Children.Add(cp);
            return cp;
        }

        private ContentPresenter FindChildContentPresenter(object data)
        {
            if (data is TabItem item)
            {
                data = item.Content;
            }

            if (data == null)
            {
                return null;
            }

            if (ItemsHolderPanel == null)
            {
                return null;
            }

            foreach (ContentPresenter cp in ItemsHolderPanel.Children)
            {
                if (cp.Content == data)
                {
                    return cp;
                }
            }

            return null;
        }
    }
}
