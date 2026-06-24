using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Mosaic.UI.Wpf.AvalonDock.Layout;

namespace Mosaic.UI.Wpf.AvalonDock.Converters
{
	/// <summary>
	/// Represents the anchor Side To Angle Converter.
	/// </summary>
	[ValueConversion(typeof(AnchorSide), typeof(double))]
	public class AnchorSideToAngleConverter : MarkupExtension, IValueConverter
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
                return 90.0;
            }

            return Binding.DoNothing;
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;

		/// <inheritdoc/>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return ConverterCreater.Get<AnchorSideToAngleConverter>();
		}
	}
}
