using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a single property in the PropertyGrid, exposing metadata and value binding.
    /// </summary>
    public partial class PropertyItem : ObservableObject
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the display name of the property.
        /// </summary>
        public string? DisplayName { get; }

        /// <summary>
        /// Gets the category of the property.
        /// </summary>
        public string? Category { get; }

        /// <summary>
        /// Gets the description of the property.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the read-only status of the property.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        public Type? PropertyType { get; }

        /// <summary>
        /// Optional custom editor type (from PropertyGridAttribute).
        /// </summary>
        public Type? EditorType { get; }

        /// <summary>
        /// True when an editor is available.  Readonly properties are allowed where you want
        /// the editor to set the value.
        /// </summary>
        public bool HasEditor => EditorType != null;

        /// <summary>
        /// Command used by the "..." button to open the custom editor.
        /// </summary>
        public ICommand OpenEditorCommand { get; }

        /// <summary>
        /// Gets the maximum length for string properties.
        /// Returns 0 if no maximum length is specified.
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// Gets the enumerable values for enum properties.
        /// </summary>
        [ObservableProperty]
        private IEnumerable? _enumValues;

        [ObservableProperty]
        private object? _value;

        private readonly object _owner;
        private readonly PropertyDescriptor _propertyDescriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyItem"/> class.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <param name="owner">The owner object.</param>
        /// <param name="attr">Optional property grid attribute.</param>
        public PropertyItem(PropertyDescriptor pd, object owner, PropertyGridAttribute? attr = null)
        {
            _owner = owner;
            _propertyDescriptor = pd;
            Name = pd.Name;
            DisplayName = attr?.DisplayName ?? pd.DisplayName ?? pd.Name;
            IsReadOnly = attr?.IsReadOnly ?? pd?.IsReadOnly ?? false;
            Category = attr?.Category ?? pd.Category;
            Description = attr?.Description ?? pd.Description;
            PropertyType = pd.PropertyType;
            _value = pd.GetValue(owner);

            // EditorType from attribute (if any)
            EditorType = attr?.EditorType;

            // Determine MaxLength from multiple sources
            MaxLength = DetermineMaxLength(pd, attr);

            // Add event handler to listen for property changes on the owner object
            if (owner is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += OwnerPropertyChanged;
            }

            // Cache enum values once, if applicable
            if (pd.PropertyType.IsEnum)
            {
                EnumValues = Enum.GetValues(pd.PropertyType);
            }

            // Commands
            OpenEditorCommand = new DelegateCommand(OpenEditor, () => HasEditor);
        }

        /// <summary>
        /// Determines the maximum length for the property from various sources.
        /// </summary>
        /// <param name="pd">The property descriptor.</param>
        /// <param name="attr">The PropertyGrid attribute.</param>
        /// <returns>The maximum length, or 0 if no limit is specified.</returns>
        private int DetermineMaxLength(PropertyDescriptor pd, PropertyGridAttribute? attr)
        {
            // First check PropertyGridAttribute
            if (attr?.MaxLength > 0)
            {
                return attr.MaxLength;
            }

            // Check for StringLength attribute on the property
            var stringLengthAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
            if (stringLengthAttr?.MaximumLength > 0)
            {
                return stringLengthAttr.MaximumLength;
            }

            // Check for MaxLength attribute on the property
            var maxLengthAttr = pd.Attributes.OfType<MaxLengthAttribute>().FirstOrDefault();
            if (maxLengthAttr?.Length > 0)
            {
                return maxLengthAttr.Length;
            }

            // No maximum length specified
            return 0;
        }

        /// <summary>
        /// Called when the PropertyItem's Value property changes (UI → Owner direction).
        /// This method propagates changes from the PropertyGrid UI back to the owner object.
        /// Triggered when: User edits a value in the PropertyGrid control.
        /// </summary>
        partial void OnValueChanged(object? value)
        {
            SetOwnerPropertyValue(value);   // use custom method with type conversion
        }

        /// <summary>
        /// Called when the owner object's property changes (Owner → UI direction).
        /// This method updates the PropertyItem's Value to reflect changes made directly to the owner object.
        /// Triggered when: The owner object's property is modified programmatically outside the PropertyGrid.
        /// </summary>
        private void OwnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Update the value if the property name matches
            if (e.PropertyName == Name)
            {
                var newValue = _propertyDescriptor.GetValue(_owner);

                // Use SetProperty for built-in change detection and notification
                SetProperty(ref _value, newValue, nameof(Value));
            }
        }

        private void SetOwnerPropertyValue(object? value)
        {
            if (_owner == null)
            {
                return;
            }

            try
            {
                var propertyInfo = _owner.GetType().GetProperty(Name);
                if (propertyInfo == null || !propertyInfo.CanWrite)
                {
                    return;
                }

                // Convert the value to the target property type
                object? convertedValue = ConvertValue(value, propertyInfo.PropertyType);
                propertyInfo.SetValue(_owner, convertedValue);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                Debug.WriteLine($"Failed to set property '{Name}': {ex.Message}");
            }
        }

        private object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            if (value.GetType() == targetType)
            {
                return value;
            }

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                if (value is string str && string.IsNullOrEmpty(str))
                {
                    return null;
                }

                targetType = underlyingType;
            }

            // Use TypeConverter for conversion
            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(value.GetType()))
            {
                return converter.ConvertFrom(value);
            }

            // Fallback to Convert.ChangeType
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Open the custom editor (invoked by the "..." button).
        /// Supports:
        ///  - types implementing IPropertyEditor (preferred)
        ///  - Window types as a fallback (attempts to extract result via common properties)
        /// </summary>
        private void OpenEditor()
        {
            if (EditorType == null)
            {
                return;
            }

            try
            {
                var editorObj = Activator.CreateInstance(EditorType);
                if (editorObj == null)
                {
                    return;
                }

                // Required: IPropertyEditor
                if (editorObj is IPropertyEditor pe)
                {
                    var result = pe.Edit(Value, PropertyType ?? typeof(object), _owner);
                    if (result != null)
                    {
                        // Assigning to the generated property triggers OnValueChanged -> owner update
                        Value = result;
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open editor for '{Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Detaches event handlers to avoid memory leaks.
        /// </summary>
        public void Detach()
        {
            if (_owner is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= OwnerPropertyChanged;
            }
        }

        /// <summary>
        /// Simple ICommand implementation used for the editor command.
        /// </summary>
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public DelegateCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

            public void Execute(object? parameter) => _execute();
        }
    }
}
