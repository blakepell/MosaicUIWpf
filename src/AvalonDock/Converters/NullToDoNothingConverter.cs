using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace AvalonDock.Converters
{
    /// <summary>
    /// Represents the null To Do Nothing Converter.
    /// </summary>
    public class NullToDoNothingConverter : MarkupExtension, IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Binding.DoNothing;
            }

            return value;
        }

        /// <inheritdoc/>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value ?? Binding.DoNothing;

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return ConverterCreater.Get<NullToDoNothingConverter>();
        }
    }
}
