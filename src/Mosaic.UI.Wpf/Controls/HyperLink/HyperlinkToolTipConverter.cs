/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
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
    /// cref="Hyperlink"/>: <list type="bullet"> <item> If the <see cref="Hyperlink.ToolTip"/> property is set, its
    /// value is returned. </item> <item> If the <see cref="Hyperlink.NavigateUri"/> property is set, its string
    /// representation is returned to indicate the destination URI. </item> <item> If the <see
    /// cref="Hyperlink.Command"/> property is set, a default message is returned to inform the user that the hyperlink
    /// will execute application-defined code. </item> </list> If none of these properties are set, the converter
    /// returns <see langword="null"/>.</remarks>
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
            var link = value as Hyperlink;

            if (link == null)
            {
                return null;
            }

            if (link.EnableAutoToolTip == false)
            {
                return null;
            }

            if (link.ToolTip != null)
            {
                return link.ToolTip;
            }

            // Otherwise, if the NavigateUri is set, return its string representation.  This is useful
            // if a link displays text, but is going to navigate to a URI to let the user know where it's
            // going.
            if (link.NavigateUri?.ToString() != null)
            {
                return link.NavigateUri?.ToString();
            }

            // Let the user know that this link will execute code if it has a command set.
            if (link.Command != null)
            {
                return "This link will execute code defined by the application.";
            }

            return null;
        }

        /// <inheritdoc cref="Convert"/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
