using System.Collections.ObjectModel;
using System.ComponentModel.Design;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides a mechanism to select an appropriate <see cref="DataTemplate"/> for editing a property  based on its
    /// type. This class extends <see cref="DataTemplateSelector"/> to enable dynamic  template selection for property
    /// editors.
    /// </summary>
    public class PropertyEditorSelector : DataTemplateSelector
    {
        /// <summary>
        /// Selects the DataTemplate to use for the given item and container.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            if (item is not PropertyItem pi)
            {
                return base.SelectTemplate(item, container);
            }

            if (!(container is FrameworkElement fe))
            {
                return base.SelectTemplate(item, container);
            }

            // Guard null and resolve underlying type for nullable support
            var propType = pi.PropertyType ?? typeof(object);
            var effectiveType = Nullable.GetUnderlyingType(propType) ?? propType;

            // Helper to find template either on the element or application resources
            static DataTemplate? FindTemplate(FrameworkElement element, string key)
            {
                var obj = element.TryFindResource(key) ?? (Application.Current?.TryFindResource(key) ?? null);
                return obj as DataTemplate;
            }

            // Enum
            if (effectiveType.IsEnum)
            {
                return FindTemplate(fe, "EnumEditor");
            }

            // Bool
            if (effectiveType == typeof(bool))
            {
                return FindTemplate(fe, "BoolEditor");
            }

            // ObservableCollection<string>
            if (effectiveType == typeof(ObservableCollection<string>))
            {
                return FindTemplate(fe, "StringListEditor");
            }

            if (effectiveType == typeof(DateOnly))
            {
                return FindTemplate(fe, "DateOnlyEditor");
            }

            //  Numeric types non decimal types.
            if (effectiveType == typeof(int) ||
                effectiveType == typeof(long) ||
                effectiveType == typeof(uint) ||
                effectiveType == typeof(ulong))
            {
                return FindTemplate(fe, "NumberEditor");
            }

            // Numeric types decimal types.
            if (effectiveType == typeof(double) ||
                effectiveType == typeof(float) ||
                effectiveType == typeof(decimal))
            {
                return FindTemplate(fe, "DecimalEditor");
            }

            // Color or Nullable<Color>
            if (effectiveType == typeof(Color))
            {
                return FindTemplate(fe, "ColorEditor");
            }

            // Fallback to text editor
            return FindTemplate(fe, "TextEditor");
        }
    }
}