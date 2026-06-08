/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;

namespace Mosaic.UI.Wpf.Controls
{

    /// <summary>
    /// Represents a side menu control that displays a collection of menu items and allows item selection.
    /// </summary>
    [ContentProperty(nameof(MenuItems))]
    public partial class SideMenu
    {
        /// <summary>
        /// Gets or sets a value indicating whether the search box should automatically receive focus when the
        /// loaded event fires for this control.
        /// </summary>
        public bool FocusSearchBoxOnLoad { get; set; } = false;

        /// <summary>
        /// Represents the method that will handle the event when the selected item in a side menu changes.
        /// </summary>
        /// <param name="sender">The source of the event, typically the side menu control.</param>
        /// <param name="selectedItem">The newly selected item in the side menu, or <see langword="null"/> if no item is selected.</param>
        public delegate void SelectedItemChangedEventHandler(object sender, SideMenuItem? selectedItem);

        /// <summary>
        /// Occurs when the selected item in the collection changes.
        /// </summary>
        /// <remarks>This event is triggered whenever the selection is updated, either programmatically or
        /// through user interaction. Subscribers can use this event to respond to changes in the selected
        /// item.</remarks>
        public event SelectedItemChangedEventHandler? SelectedItemChanged;

        /// <summary>
        /// Occurs when any <see cref="SideMenuItem"/> in this menu is activated (selected).
        /// The event arguments expose the clicked item and a read-only snapshot of its parameters,
        /// making this a convenient single-subscription point for handling any menu navigation.
        /// </summary>
        public event EventHandler<SideMenuItemClickEventArgs>? ItemClicked;

        /// <summary>
        /// Identifies the <see cref="SearchBoxVisibility"/> dependency property, which controls the visibility of the
        /// search box in the side menu.
        /// </summary>
        public static readonly DependencyProperty SearchBoxVisibilityProperty = DependencyProperty.Register(nameof(SearchBoxVisibility), typeof(Visibility), typeof(SideMenu), new PropertyMetadata(default(Visibility)));

        /// <summary>
        /// 
        /// </summary>
        public Visibility SearchBoxVisibility
        {
            get => (Visibility)GetValue(SearchBoxVisibilityProperty);
            set => SetValue(SearchBoxVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MenuItems"/> dependency property, which holds the collection of menu items
        /// displayed in the side menu.
        /// </summary>
        public static readonly DependencyProperty MenuItemsProperty = DependencyProperty.Register(nameof(MenuItems), typeof(ObservableCollection<SideMenuItem>), typeof(SideMenu), new PropertyMetadata(null)); // Changed default to null

        /// <summary>
        /// Gets or sets the collection of side menu items displayed in the user interface.
        /// </summary>
        public ObservableCollection<SideMenuItem> MenuItems
        {
            get => (ObservableCollection<SideMenuItem>)GetValue(MenuItemsProperty);
            set => SetValue(MenuItemsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedItem"/> dependency property.
        /// </summary>

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(SideMenuItem), typeof(SideMenu), new PropertyMetadata(null, OnSelectedItemChanged));

        /// <summary>
        /// Gets or sets the currently selected item in the side menu.
        /// </summary>
        public SideMenuItem? SelectedItem
        {
            get => (SideMenuItem?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ContentPresenter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentPresenterProperty = DependencyProperty.Register(nameof(ContentPresenter), typeof(ContentPresenter), typeof(SideMenu), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="ContentPresenter"/> associated with the control.
        /// </summary>
        public ContentPresenter? ContentPresenter
        {
            get => (ContentPresenter?)GetValue(ContentPresenterProperty);
            set => SetValue(ContentPresenterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Compact"/> dependency property, which controls whether the side menu displays items in compact mode.
        /// </summary>
        public static readonly DependencyProperty CompactProperty = DependencyProperty.Register(nameof(Compact), typeof(bool), typeof(SideMenu), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the side menu displays items in compact mode (icon above text).
        /// </summary>
        public bool Compact
        {
            get => (bool)GetValue(CompactProperty);
            set => SetValue(CompactProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VerticalScrollViewerVisibility"/> dependency property, which controls the visibility of the
        /// vertical scroll bar in the side menu's scroll viewer.
        /// </summary>
        public static readonly DependencyProperty VerticalScrollViewerVisibilityProperty = DependencyProperty.Register(nameof(VerticalScrollViewerVisibility), typeof(ScrollBarVisibility), typeof(SideMenu), new PropertyMetadata(ScrollBarVisibility.Auto));

        /// <summary>
        /// Gets or sets a value indicating the visibility of the vertical scroll bar in the side menu.
        /// </summary>
        public ScrollBarVisibility VerticalScrollViewerVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollViewerVisibilityProperty);
            set => SetValue(VerticalScrollViewerVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableDragAndDropReordering"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableDragAndDropReorderingProperty =
            DependencyProperty.Register(nameof(EnableDragAndDropReordering), typeof(bool), typeof(SideMenu),
                new PropertyMetadata(false, OnEnableDragAndDropReorderingChanged));

        /// <summary>
        /// Gets or sets a value indicating whether items in the menu can be reordered by dragging and
        /// dropping them. When <see langword="true"/>, item selection requires a full click (mouse-up)
        /// so that the mouse-down event is available for drag initiation. Defaults to <see langword="false"/>.
        /// </summary>
        public bool EnableDragAndDropReordering
        {
            get => (bool)GetValue(EnableDragAndDropReorderingProperty);
            set => SetValue(EnableDragAndDropReorderingProperty, value);
        }

        private static void OnEnableDragAndDropReorderingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SideMenu sideMenu)
            {
                sideMenu.ApplyDragAndDropReordering((bool)e.NewValue);
            }
        }

        private void ApplyDragAndDropReordering(bool enable)
        {
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(MenuOptions, enable);
            GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(MenuOptions, enable);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SideMenu"/> class.
        /// </summary>
        public SideMenu()
        {
            InitializeComponent();
            // Ensure MenuItems is initialized for this instance.
            // This provides a unique collection for each SideMenu, preventing items
            // from being added to a shared static collection when defined in XAML.
            SetValue(MenuItemsProperty, new ObservableCollection<SideMenuItem>());
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> event for the side menu instance.
        /// </summary>
        /// <param name="sender">The source of the event, typically the side menu instance.</param>
        /// <param name="e">The event data associated with the <see cref="FrameworkElement.Loaded"/> event.</param>
        private void SideMenuInstance_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.FocusSearchBoxOnLoad)
            {
                SearchBox.Focus();
            }
        }

        /// <summary>
        /// OnSelectedItemChanged is called when the SelectedItem property changes.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SideMenu sideMenu)
            {
                SideMenuItem? item = null;

                if (e.OldValue is SideMenuItem oldItem)
                {
                    oldItem.IsSelected = false;
                }

                if (e.NewValue is SideMenuItem newItem)
                {
                    newItem.IsSelected = true;
                    item = newItem;
                }

                // Raise the SelectedItemChanged event
                sideMenu.SelectedItemChanged?.Invoke(sideMenu, e.NewValue as SideMenuItem);

                // Raise the Click event on the item, then bubble it up through ItemClicked on the menu
                if (item != null)
                {
                    var clickArgs = item.RaiseClick();
                    sideMenu.ItemClicked?.Invoke(sideMenu, clickArgs);
                }

                if (sideMenu.ContentPresenter != null && item?.ContentType != null)
                {
                    object? contentInstance = sideMenu.GetContentInstance(item, out bool refreshRecipient);

                    if (refreshRecipient && contentInstance is ISideMenuRecipient sideMenuRecipient)
                    {
                        // TODO: add image source.
                        sideMenuRecipient.ImageSource = item.ImageSource;
                        sideMenuRecipient.Parameters = new Dictionary<string, object?>(item.Parameters);
                        sideMenuRecipient.Refresh();
                    }

                    sideMenu.ContentPresenter.Content = contentInstance;
                }
            }
        }

        private object? GetContentInstance(SideMenuItem item, out bool refreshRecipient)
        {
            refreshRecipient = true;

            if (item.ContentType == null)
            {
                return null;
            }

            if (item.ReuseContentInstance)
            {
                if (item.CachedContentInstance != null)
                {
                    refreshRecipient = false;
                    return item.CachedContentInstance;
                }

                item.CachedContentInstance = AppServices.CreateInstance(item.ContentType);
                return item.CachedContentInstance;
            }

            if (item.ContentTypeIsSingleton && !AppServices.IsRegistered(item.ContentType))
            {
                var ctrl = Activator.CreateInstance(item.ContentType);

                if (ctrl != null && AppServices.ServiceCollection != null)
                {
                    // TODO: Transition to Argus.Core version
                    AppServices.ServiceCollection.AddSingleton(item.ContentType, ctrl);
                    AppServices.BuildServiceProvider();
                }
            }

            if (item.ContentTypeIsSingleton && AppServices.IsRegistered(item.ContentType))
            {
                return AppServices.GetService(item.ContentType);
            }

            return AppServices.CreateInstance(item.ContentType);
        }

        /// <summary>
        /// Selects the specified item in the side menu and executes its associated command, if available.
        /// <see cref="SideMenuHeader"/> items are silently ignored.
        /// </summary>
        public void SelectItem(SideMenuItem item)
        {
            if (item is SideMenuHeader)
            {
                return;
            }

            // Only update the selection (and therefore the content view) when this item has content.
            // A dialog-only item shows its dialog but leaves the current selection highlighted so the
            // menu selection keeps matching what is displayed in the content area.
            if (item.ContentType != null || item.DialogContentType == null)
            {
                SelectedItem = item;
            }

            if (item.DialogContentType != null)
            {
                ShowItemDialog(item);
            }

            // Execute command if available
            if (item.Command?.CanExecute(item.CommandParameter) == true)
            {
                item.Command.Execute(item.CommandParameter);
            }
        }

        /// <summary>
        /// Shows the <see cref="SideMenuItem.DialogContentType"/> as a dialog (modal) or modeless window.
        /// When shown modally the dialog's owner is set to the window hosting this menu.
        /// </summary>
        private void ShowItemDialog(SideMenuItem item)
        {
            if (item.DialogContentType == null || !typeof(Window).IsAssignableFrom(item.DialogContentType))
            {
                return;
            }

            if (Activator.CreateInstance(item.DialogContentType) is not Window window)
            {
                return;
            }

            if (item.DialogIsModal)
            {
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
            else
            {
                window.Show();
            }
        }

        /// <summary>
        /// Selects a menu item by its zero-based index, counting only selectable items
        /// (i.e. <see cref="SideMenuHeader"/> entries are skipped).
        /// </summary>
        public void SelectByIndex(int index)
        {
            int nonHeaderIndex = 0;

            foreach (var item in MenuItems)
            {
                if (item is SideMenuHeader)
                {
                    continue;
                }

                if (nonHeaderIndex == index)
                {
                    SelectItem(item);
                    return;
                }

                nonHeaderIndex++;
            }
        }

        /// <summary>
        /// Expands all menu items in the collection.
        /// </summary>
        public void ExpandAll()
        {
            if (MenuItems == null)
            {
                return;
            }

            ExpandItems(MenuItems);
        }

        /// <summary>
        /// Expands all items in the specified collection by setting their <see cref="SideMenuItem.IsExpanded"/>
        /// property to <see langword="true"/>.
        /// </summary>
        private void ExpandItems(ObservableCollection<SideMenuItem> items)
        {
            foreach (var item in items)
            {
                item.IsExpanded = true;

                if (item.Children is { Count: > 0 })
                {
                    ExpandItems(item.Children);
                }
            }
        }

        /// <summary>
        /// Collapses all items in the specified collection by setting their <see cref="SideMenuItem.IsExpanded"/>
        /// property to <see langword="false"/>.
        /// </summary>
        private void CollapseItems(ObservableCollection<SideMenuItem> items)
        {
            foreach (var item in items)
            {
                item.IsExpanded = false;

                if (item.Children is { Count: > 0 })
                {
                    CollapseItems(item.Children);
                }
            }
        }

        /// <summary>
        /// Collapses all menu items in the collection.
        /// </summary>
        /// <remarks>This method iterates through the <see cref="MenuItems"/> collection and collapses
        /// each item. If <see cref="MenuItems"/> is null, the method performs no action.</remarks>
        public void CollapseAll()
        {
            if (MenuItems == null)
            {
                return;
            }

            CollapseItems(MenuItems);
        }
    }
}
