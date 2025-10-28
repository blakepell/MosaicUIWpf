using System;
using System.Globalization;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts a boolean compact mode value to the appropriate height for the menu item.
    /// </summary>
    public class CompactHeightConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to height (48 for compact mode, 64 for normal mode).
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCompact)
            {
                return isCompact ? 64d : 38d;
            }

            return 48d;
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