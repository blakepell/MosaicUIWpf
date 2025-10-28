using System.Globalization;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts a boolean compact mode value to the appropriate width for the menu item.
    /// </summary>
    public class CompactWidthConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to width (80 for compact mode, Auto for normal mode).
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCompact)
            {
                return isCompact ? 64d : double.NaN; // NaN = Auto
            }

            return double.NaN;
        }

        /// <summary>
        /// Not implemented for this converter.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}