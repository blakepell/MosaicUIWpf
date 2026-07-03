/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Logging
{
    /// <summary>
    /// Converts a log severity to a brush color.
    /// </summary>
    public class LogSeverityToBrushConverter : IValueConverter
    {
        private static readonly Brush DebugBrush = CreateBrush("#00A99D");
        private static readonly Brush InfoBrush = CreateBrush("#007ACC");
        private static readonly Brush SuccessBrush = CreateBrush("#2E7D32");
        private static readonly Brush WarningBrush = CreateBrush("#D9822B");
        private static readonly Brush ErrorBrush = CreateBrush("#C62828");

        /// <inheritdoc/>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not LogSeverity logSeverity)
            {
                return Brushes.Black;
            }

            return logSeverity switch
            {
                LogSeverity.Debug => DebugBrush,
                LogSeverity.Info => InfoBrush,
                LogSeverity.Success => SuccessBrush,
                LogSeverity.Warning => WarningBrush,
                LogSeverity.Error => ErrorBrush,
                _ => Brushes.Black
            };
        }

        /// <inheritdoc/>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static Brush CreateBrush(string color)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
            brush.Freeze();
            return brush;
        }
    }
}
