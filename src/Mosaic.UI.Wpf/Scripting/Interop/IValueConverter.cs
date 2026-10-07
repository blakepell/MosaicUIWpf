using System;

namespace Mosaic.UI.Wpf.Scripting.Interop
{
    public interface IValueConverter
    {
        bool TryConvertValue(object value, Type targetType, out object convertedValue);

        bool IsValueAssignableTo(object value, Type targetType);
    }
}
