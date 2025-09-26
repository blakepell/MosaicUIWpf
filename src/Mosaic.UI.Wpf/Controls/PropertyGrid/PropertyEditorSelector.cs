using System.Collections.ObjectModel;

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

            // Enum
            if (pi.PropertyType.IsEnum)
            {
                return fe.FindResource("EnumEditor") as DataTemplate;
            }

            // Bool
            if (pi.PropertyType == typeof(bool))
            {
                return fe.FindResource("BoolEditor") as DataTemplate;
            }

            // ObservableCollection<string>
            if (pi.PropertyType == typeof(ObservableCollection<string>))
            {
                return fe.FindResource("StringListEditor") as DataTemplate;
            }

            if (pi.PropertyType == typeof(DateOnly) || pi.PropertyType == typeof(DateOnly?))
            {
                return fe.FindResource("DateOnlyEditor") as DataTemplate;
            }

            //  Numeric types non decimal types.
            if (pi.PropertyType == typeof(int) ||
                pi.PropertyType == typeof(long) ||
                pi.PropertyType == typeof(uint) ||
                pi.PropertyType == typeof(ulong))
            {
                return fe.FindResource("NumberEditor") as DataTemplate;
            }

            // Numeric types decimal types.
            if (pi.PropertyType == typeof(double) ||
                pi.PropertyType == typeof(float) ||
                pi.PropertyType == typeof(decimal))
            {
                return fe.FindResource("DecimalEditor") as DataTemplate;
            }

            // Fallback to text editor
            return fe.FindResource("TextEditor") as DataTemplate;
        }
    }
}