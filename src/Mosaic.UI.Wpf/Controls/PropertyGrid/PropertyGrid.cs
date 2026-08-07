/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Common;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A control that displays the properties of an object in a grid format.
    /// </summary>
    [DefaultProperty(nameof(Object))]
    public class PropertyGrid : Control
    {
        static PropertyGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid),
                new FrameworkPropertyMetadata(typeof(PropertyGrid)));
        }

        #region Public API

        /// <summary>
        /// Identifies the Object dependency property.
        /// </summary>
        public static readonly DependencyProperty ObjectProperty =
            DependencyProperty.Register(nameof(Object), typeof(object), typeof(PropertyGrid),
                new PropertyMetadata(null, OnObjectChanged));

        /// <summary>
        /// Gets or sets the object whose properties are displayed in the grid.
        /// </summary>
        public object? Object
        {
            get => GetValue(ObjectProperty);
            set => SetValue(ObjectProperty, value);
        }

        /// <summary>
        /// Identifies the Properties dependency property.
        /// </summary>
        public static readonly DependencyProperty PropertiesProperty = DependencyProperty.Register(
            nameof(Properties), 
            typeof(ObservableCollection<PropertyItem>), 
            typeof(PropertyGrid), 
            new PropertyMetadata(null, OnPropertiesChanged));

        /// <summary>
        /// Gets or sets the collection of properties displayed in the grid.
        /// </summary>
        public ObservableCollection<PropertyItem> Properties
        {
            get
            {
                var collection = (ObservableCollection<PropertyItem>?)GetValue(PropertiesProperty);
                if (collection == null)
                {
                    collection = new ObservableCollection<PropertyItem>();
                    SetValue(PropertiesProperty, collection);
                }
                return collection;
            }
            set => SetValue(PropertiesProperty, value);
        }

        /// <summary>
        /// Identifies the RevertInvalidValues dependency property.
        /// </summary>
        public static readonly DependencyProperty RevertInvalidValuesProperty =
            DependencyProperty.Register(nameof(RevertInvalidValues), typeof(bool), typeof(PropertyGrid),
                new PropertyMetadata(false, OnRevertInvalidValuesChanged));

        /// <summary>
        /// Gets or sets whether invalid values should be reverted to the previous valid value on lost focus.
        /// When true, if a value cannot be converted to the underlying type, it will revert to the previous value.
        /// </summary>
        public bool RevertInvalidValues
        {
            get => (bool)GetValue(RevertInvalidValuesProperty);
            set => SetValue(RevertInvalidValuesProperty, value);
        }

        /// <summary>
        /// Identifies the SelectedProperty dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedPropertyProperty =
            DependencyProperty.Register(nameof(SelectedProperty), typeof(PropertyItem), typeof(PropertyGrid),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedPropertyChanged));

        /// <summary>
        /// Gets or sets the currently selected <see cref="PropertyItem"/>.  The selected property is what
        /// drives the name/description panel anchored at the bottom of the grid.
        /// </summary>
        [Category("Behavior")]
        [Description("The currently selected property in the grid.")]
        public PropertyItem? SelectedProperty
        {
            get => (PropertyItem?)GetValue(SelectedPropertyProperty);
            set => SetValue(SelectedPropertyProperty, value);
        }

        /// <summary>
        /// Identifies the ShowDescriptionPanel dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowDescriptionPanelProperty =
            DependencyProperty.Register(nameof(ShowDescriptionPanel), typeof(bool), typeof(PropertyGrid),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the description panel anchored to the bottom of the grid is displayed.  The
        /// panel shows the display name and the <see cref="DescriptionAttribute"/> text (when one exists) of
        /// the selected property.  Defaults to true.
        /// </summary>
        [Category("Appearance")]
        [Description("Shows the name and description of the selected property anchored to the bottom of the grid.")]
        public bool ShowDescriptionPanel
        {
            get => (bool)GetValue(ShowDescriptionPanelProperty);
            set => SetValue(ShowDescriptionPanelProperty, value);
        }

        /// <summary>
        /// Identifies the SelectedPropertyChanged routed event.
        /// </summary>
        public static readonly RoutedEvent SelectedPropertyChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SelectedPropertyChanged), RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<PropertyItem?>), typeof(PropertyGrid));

        /// <summary>
        /// Occurs when the selected property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<PropertyItem?> SelectedPropertyChanged
        {
            add => AddHandler(SelectedPropertyChangedEvent, value);
            remove => RemoveHandler(SelectedPropertyChangedEvent, value);
        }

        #endregion

        #region Selection

        /// <summary>
        /// Identifies the SelectOnClick attached property.  When set to true on an element inside the
        /// property grid's item template, clicking (or focusing) that element selects the
        /// <see cref="PropertyItem"/> it is bound to.
        /// </summary>
        public static readonly DependencyProperty SelectOnClickProperty =
            DependencyProperty.RegisterAttached("SelectOnClick", typeof(bool), typeof(PropertyGrid),
                new PropertyMetadata(false, OnSelectOnClickChanged));

        /// <summary>
        /// Sets the value of the SelectOnClick attached property.
        /// </summary>
        public static void SetSelectOnClick(DependencyObject element, bool value) => element.SetValue(SelectOnClickProperty, value);

        /// <summary>
        /// Gets the value of the SelectOnClick attached property.
        /// </summary>
        public static bool GetSelectOnClick(DependencyObject element) => (bool)element.GetValue(SelectOnClickProperty);

        /// <summary>
        /// Wires (or unwires) the input handlers used to select a property when its name is clicked.
        /// </summary>
        private static void OnSelectOnClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
            {
                return;
            }

            element.PreviewMouseLeftButtonDown -= OnSelectOnClickMouseDown;
            element.GotKeyboardFocus -= OnSelectOnClickGotFocus;

            if (e.NewValue is true)
            {
                element.PreviewMouseLeftButtonDown += OnSelectOnClickMouseDown;
                element.GotKeyboardFocus += OnSelectOnClickGotFocus;
            }
        }

        /// <summary>
        /// Selects the clicked property and moves keyboard focus to it.
        /// </summary>
        private static void OnSelectOnClickMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            SelectFrom(element);

            if (element.Focusable)
            {
                element.Focus();
            }
        }

        /// <summary>
        /// Keeps the selection in sync when the name cell receives keyboard focus (tab / arrow navigation).
        /// </summary>
        private static void OnSelectOnClickGotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                SelectFrom(element);
            }
        }

        /// <summary>
        /// Resolves the owning <see cref="PropertyGrid"/> and the bound <see cref="PropertyItem"/> and applies
        /// the selection.
        /// </summary>
        private static void SelectFrom(FrameworkElement element)
        {
            if (element.DataContext is not PropertyItem item)
            {
                return;
            }

            var grid = ControlsHelper.FindParent<PropertyGrid>(element);
            if (grid != null)
            {
                grid.SelectedProperty = item;
            }
        }

        /// <summary>
        /// Called when the SelectedProperty property changes.  Keeps <see cref="PropertyItem.IsSelected"/> in
        /// sync so the template can highlight the name cell.
        /// </summary>
        private static void OnSelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pg = (PropertyGrid)d;

            if (e.OldValue is PropertyItem oldItem)
            {
                oldItem.IsSelected = false;
            }

            if (e.NewValue is PropertyItem newItem)
            {
                newItem.IsSelected = true;
            }

            pg.RaiseEvent(new RoutedPropertyChangedEventArgs<PropertyItem?>(
                e.OldValue as PropertyItem,
                e.NewValue as PropertyItem,
                SelectedPropertyChangedEvent));
        }

        #endregion

        #region Dependency Property Change

        /// <summary>
        /// Called when the Properties dependency property changes.
        /// </summary>
        private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // This callback is optional but can be used for additional logic if needed
        }

        /// <summary>
        /// Called when the Object property changes.
        /// </summary>
        /// <param name="d">The PropertyGrid instance.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pg = (PropertyGrid)d;
            pg.UpdateProperties();
        }

        /// <summary>
        /// Called when the RevertInvalidValues property changes.
        /// </summary>
        /// <param name="d">The PropertyGrid instance.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnRevertInvalidValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pg = (PropertyGrid)d;
            var revertInvalidValues = (bool)e.NewValue;

            // Update all existing property items
            foreach (var propertyItem in pg.Properties)
            {
                propertyItem.RevertInvalidValues = revertInvalidValues;
            }
        }

        /// <summary>
        /// Updates the properties displayed in the grid based on the current Object.
        /// </summary>
        private void UpdateProperties()
        {
            // The items backing the selection are about to be discarded.
            SelectedProperty = null;

            // Cleanup any events that were wired up.
            foreach (var propertyItem in Properties)
            {
                propertyItem.Detach();
            }

            Properties.Clear();

            if (Object == null)
            {
                return;
            }

            var props = TypeDescriptor.GetProperties(Object);

            foreach (PropertyDescriptor pd in props)
            {
                var backingFieldAttributes = GetBackingFieldAttributes(Object, pd).ToArray();
                var attr = GetAttribute<PropertyGridAttribute>(pd, backingFieldAttributes);
                
                if (attr is { Ignore: true })
                {
                    continue;
                }

                if (!IsBrowsable(pd, backingFieldAttributes))
                {
                    continue;
                }

                var propertyItem = new PropertyItem(pd, Object, attr, backingFieldAttributes)
                {
                    RevertInvalidValues = RevertInvalidValues
                };
                Properties.Add(propertyItem);
            }

            if (Properties.Any())
            {
                var view = CollectionViewSource.GetDefaultView(Properties);
                if (view is { GroupDescriptions.Count: 0 })
                {
                    view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                }

                // Clear any existing sort descriptions
                view.SortDescriptions.Clear();
                // Sort by Category (alphabetically), then by DisplayName (alphabetically)
                view.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
            }
        }

        /// <summary>
        /// Gets attributes from a matching backing field when source generators place metadata there.
        /// </summary>
        private static IEnumerable<Attribute> GetBackingFieldAttributes(object owner, PropertyDescriptor propertyDescriptor)
        {
            var ownerType = owner.GetType();
            var fields = ownerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (IsBackingFieldForProperty(field.Name, propertyDescriptor.Name))
                {
                    return field.GetCustomAttributes().OfType<Attribute>();
                }
            }

            return Enumerable.Empty<Attribute>();
        }

        /// <summary>
        /// Determines whether the property should be visible in the grid.
        /// </summary>
        private static bool IsBrowsable(PropertyDescriptor propertyDescriptor, IEnumerable<Attribute> additionalAttributes)
        {
            if (!propertyDescriptor.IsBrowsable)
            {
                return false;
            }

            return !additionalAttributes.OfType<BrowsableAttribute>().Any(static attr => !attr.Browsable);
        }

        /// <summary>
        /// Finds an attribute on the descriptor first, then on source-generator backing fields.
        /// </summary>
        private static T? GetAttribute<T>(PropertyDescriptor propertyDescriptor, IEnumerable<Attribute> additionalAttributes)
            where T : Attribute
        {
            return propertyDescriptor.Attributes.OfType<T>().FirstOrDefault()
                ?? additionalAttributes.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Matches normal, CommunityToolkit.Mvvm, and compiler generated backing-field names.
        /// </summary>
        private static bool IsBackingFieldForProperty(string fieldName, string propertyName)
        {
            if (string.Equals(fieldName, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fieldName, "_" + propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fieldName, "m_" + propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(fieldName, $"<{propertyName}>k__BackingField", StringComparison.Ordinal);
        }

        #endregion
    }
}
