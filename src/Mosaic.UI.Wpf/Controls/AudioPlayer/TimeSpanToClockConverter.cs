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
using System.Windows.Markup;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts a <see cref="TimeSpan"/> into a friendly clock string in the form <c>h:mm:ss</c>.
    /// </summary>
    /// <remarks>
    /// Used by the <see cref="AudioPlayer"/> control to render the current playback position and
    /// the total duration of the active track on either side of the seek slider.
    /// </remarks>
    public class TimeSpanToClockConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Gets a singleton instance of the converter.
        /// </summary>
        public static readonly TimeSpanToClockConverter Instance = new();

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> to a <c>h:mm:ss</c> string.
        /// </summary>
        /// <param name="value">The <see cref="TimeSpan"/> value to format.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A formatted clock string such as <c>0:03:27</c>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var time = value switch
            {
                TimeSpan ts => ts,
                double seconds => TimeSpan.FromSeconds(seconds),
                _ => TimeSpan.Zero
            };

            if (time < TimeSpan.Zero)
            {
                time = TimeSpan.Zero;
            }

            // Display hours unpadded with zero-padded minutes/seconds (e.g. 0:03:27, 1:12:05).
            int hours = (int)time.TotalHours;
            return $"{hours}:{time.Minutes:D2}:{time.Seconds:D2}";
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
