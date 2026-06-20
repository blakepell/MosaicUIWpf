using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts a <see cref="System.Windows.Controls.ProgressBar"/> value into the corresponding arc
    /// angle, in degrees, used to render a <see cref="RadialProgressBar"/>.
    /// </summary>
    public class ProgressToAngleConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a progress value and its owning progress bar into an arc angle in degrees.
        /// </summary>
        /// <param name="values">An array whose first element is the progress value and whose second element is the <see cref="System.Windows.Controls.ProgressBar"/> that supplies the minimum and maximum.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use. This parameter is not used.</param>
        /// <param name="culture">The culture to use in the converter. This parameter is not used.</param>
        /// <returns>The angle, in degrees, between 0 and 359.999 that represents the progress relative to the bar's range.</returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var progress = (double)values[0];
            var bar = values[1] as ProgressBar;

            return 359.999 * (progress / (bar.Maximum - bar.Minimum));
        }

        /// <summary>
        /// Not supported. This converter is one-way only.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetTypes">The array of types to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Always <see langword="null"/>.</returns>
        public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
