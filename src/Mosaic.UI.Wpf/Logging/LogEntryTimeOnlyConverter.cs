/*
 * ApexGateUI
 *
 * @project lead      : Blake Pell
 * @company           : ApexGate
 * @website           : https://www.apexgate.net
 * @website           : https://www.blakepell.com
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : Closed Source
 */

using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Logging
{
    /// <summary>
    /// Converts a Date and time to only display a time.
    /// </summary>
    public class LogEntryTimeOnlyConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("hh:mm:ss tt");
            }

            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
