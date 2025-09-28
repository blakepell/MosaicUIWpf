using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Converters
{
    /// <summary>
    /// Converts an integer value to a boolean indicating whether it is greater than zero.
    /// </summary>
    public class GreaterThanZeroConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Gets a singleton instance of the converter.
        /// </summary>
        public static readonly GreaterThanZeroConverter Instance = new();

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <summary>
        /// Converts an integer to a boolean.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>True if the value is an integer greater than zero; otherwise, false.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }

            return false;
        }

        /// <summary>
        /// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}