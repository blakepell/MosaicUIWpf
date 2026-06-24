using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosaic.UI.Wpf.AvalonDock.Converters
{
	/// <summary>
	/// Represents the anchorable Context Menu Hide Visibility Converter.
	/// </summary>
	public class AnchorableContextMenuHideVisibilityConverter : MarkupExtension, IMultiValueConverter
	{
		/// <summary>
		/// Converts the supplied value.
		/// </summary>
		/// <param name="values">The values.</param>
		/// <param name="targetType">The target type.</param>
		/// <param name="parameter">The converter parameter.</param>
		/// <param name="culture">The culture information.</param>
		/// <returns>The result of the operation.</returns>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((values.Length == 2)
                && (values[0] != DependencyProperty.UnsetValue)
                && (values[1] != DependencyProperty.UnsetValue)
                && (values[1] is bool boolean))
			{
				var canClose = boolean;

				return canClose ? Visibility.Collapsed : values[0];
			}

            return values.Length > 0 ? values[0] : DependencyProperty.UnsetValue;
        }

		/// <summary>
		/// Converts the supplied value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="targetTypes">The target Types.</param>
		/// <param name="parameter">The converter parameter.</param>
		/// <param name="culture">The culture information.</param>
		/// <returns>The result of the operation.</returns>
		public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
		{
            return targetTypes.Select(_ => Binding.DoNothing).ToArray();
		}

		/// <inheritdoc/>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return ConverterCreater.Get<AnchorableContextMenuHideVisibilityConverter>();
		}
	}
}
