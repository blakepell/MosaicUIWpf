namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides attached property support for connecting PropertyItem validation to UI elements.
    /// </summary>
    public static class PropertyGridValidation
    {
        /// <summary>
        /// Identifies the PropertyItem attached property.
        /// </summary>
        public static readonly DependencyProperty PropertyItemProperty =
            DependencyProperty.RegisterAttached(
                "PropertyItem",
                typeof(PropertyItem),
                typeof(PropertyGridValidation),
                new PropertyMetadata(null, OnPropertyItemChanged));

        /// <summary>
        /// Gets the PropertyItem attached to the specified element.
        /// </summary>
        public static PropertyItem? GetPropertyItem(DependencyObject obj)
        {
            return (PropertyItem?)obj.GetValue(PropertyItemProperty);
        }

        /// <summary>
        /// Sets the PropertyItem attached to the specified element.
        /// </summary>
        public static void SetPropertyItem(DependencyObject obj, PropertyItem? value)
        {
            obj.SetValue(PropertyItemProperty, value);
        }

        /// <summary>
        /// Called when the PropertyItem attached property changes.
        /// </summary>
        private static void OnPropertyItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
            {
                return;
            }

            // Detach old PropertyItem
            if (e.OldValue is PropertyItem oldItem)
            {
                oldItem.DetachValidationHandler(element);
            }

            // Attach new PropertyItem
            if (e.NewValue is PropertyItem newItem)
            {
                newItem.AttachValidationHandler(element);
            }
        }
    }
}
