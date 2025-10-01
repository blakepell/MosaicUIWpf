using System;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Implement this interface for custom property editors used by the PropertyGrid.
    /// The <see cref="Edit"/> method is invoked with the current value, the property type, and the owner object.
    /// Return the edited value (or null to indicate no change).
    /// </summary>
    public interface IPropertyEditor
    {
        object? Edit(object? currentValue, Type propertyType, object owner);
    }
}