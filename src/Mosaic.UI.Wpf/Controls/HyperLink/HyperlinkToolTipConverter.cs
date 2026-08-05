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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides a mechanism to convert a <see cref="Hyperlink"/> object into a tooltip string that describes its
    /// behavior or destination. This converter is commonly used in XAML to dynamically generate tooltips for hyperlinks
    /// based on their properties.
    /// </summary>
    /// <remarks>The conversion logic determines the tooltip content based on the properties of the <see
    /// cref="Hyperlink"/>: <list type="bullet"> <item> If the <see cref="FrameworkElement.ToolTip"/> property is set, its
    /// value is returned. </item> <item> If the <see cref="Hyperlink.NavigateUri"/> property is set, its string
    /// representation is returned to indicate the destination URI. </item> <item> If the <see
    /// cref="Hyperlink.Command"/> property is set, a default message is returned to inform the user that the hyperlink
    /// will execute application-defined code. </item> </list> If none of these properties are set, the converter
    /// returns <see langword="null"/>.
    /// <para>Be aware that a binding whose source is the <see cref="Hyperlink"/> object itself only re-evaluates when
    /// that object reference changes, so it will not pick up later changes to the properties the tooltip is derived
    /// from.  Bind to <see cref="Hyperlink.AutoToolTip"/> instead, which is a dependency property that raises change
    /// notifications.  The default control template does this.</para>
    /// </remarks>
    public class HyperlinkToolTipConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Static instance of the <see cref="HyperlinkToolTipConverter"/> for use in XAML.
        /// </summary>
        public static readonly HyperlinkToolTipConverter Instance = new();

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <inheritdoc cref="Convert"/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is Hyperlink link ? link.AutoToolTip : null;
        }

        /// <inheritdoc cref="Convert"/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
