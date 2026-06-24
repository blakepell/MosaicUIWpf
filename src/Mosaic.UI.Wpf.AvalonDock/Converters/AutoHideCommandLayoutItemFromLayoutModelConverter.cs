using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Mosaic.UI.Wpf.AvalonDock.Controls;
using Mosaic.UI.Wpf.AvalonDock.Layout;

namespace Mosaic.UI.Wpf.AvalonDock.Converters
{
	/// <summary>
	/// Represents the auto Hide Command Layout Item From Layout Model Converter.
	/// </summary>
	public class AutoHideCommandLayoutItemFromLayoutModelConverter : MarkupExtension, IValueConverter
	{
		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			// when this converter is called layout could be constructing so many properties here are potentially not valid
			if (value is not LayoutContent layoutModel)
            {
                return null;
            }

            if (layoutModel.Root == null)
            {
                return null;
            }

            if (layoutModel.Root.Manager == null)
            {
                return null;
            }

            if (layoutModel.Root.Manager.GetLayoutItemFromModel(layoutModel) is not LayoutAnchorableItem layoutItemModel)
            {
                return Binding.DoNothing;
            }

            return layoutItemModel.AutoHideCommand;
		}

		/// <inheritdoc/>
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;

		/// <inheritdoc/>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return ConverterCreater.Get<AutoHideCommandLayoutItemFromLayoutModelConverter>();
		}
	}
}
