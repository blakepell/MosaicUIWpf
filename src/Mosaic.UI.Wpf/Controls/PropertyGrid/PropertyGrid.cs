using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A control that displays the properties of an object in a grid format.
    /// </summary>
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
