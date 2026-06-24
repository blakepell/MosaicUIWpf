using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Mosaic.UI.Wpf.AvalonDock.Layout;

namespace Mosaic.UI.Wpf.AvalonDock.Converters
{
	/// <summary>
	/// Represents the anchor Side To Orientation Converter.
	/// </summary>
	[ValueConversion(typeof(AnchorSide), typeof(Orientation))]
	public class AnchorSideToOrientationConverter : MarkupExtension, IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is not AnchorSide side)
            {
                return Binding.DoNothing;
            }

			if (side == AnchorSide.Left || side == AnchorSide.Right)
            {
                return Orientation.Vertical;
            }

            return Orientation.Horizontal;
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;

		/// <inheritdoc/>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return ConverterCreater.Get<AnchorSideToOrientationConverter>();
		}
	}
}
