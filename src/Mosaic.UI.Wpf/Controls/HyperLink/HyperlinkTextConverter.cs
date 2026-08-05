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
    /// Provides a value converter that extracts display text from a <see cref="Hyperlink"/> object for use in data
    /// binding scenarios.
    /// </summary>
    /// <remarks>
    /// <para>This converter is typically used to display the text or URI of a <see cref="Hyperlink"/> in UI elements.
    /// If the <see cref="Hyperlink.Text"/> property is not set, the converter will fall back to the
    /// <see cref="Hyperlink.NavigateUri"/> property. If neither is available, an empty string is returned.</para>
    /// <para>Be aware that a binding whose source is the <see cref="Hyperlink"/> object itself only re-evaluates when
    /// that object reference changes, so it will not pick up later changes to <see cref="Hyperlink.Text"/> or
    /// <see cref="Hyperlink.NavigateUri"/>.  Bind to <see cref="Hyperlink.DisplayText"/> instead, which is a dependency
    /// property that raises change notifications.  The default control template does this.</para>
    /// </remarks>
    public class HyperlinkTextConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Static instance of the <see cref="HyperlinkTextConverter"/> for use in XAML.
        /// </summary>
        public static readonly HyperlinkTextConverter Instance = new();

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <inheritdoc cref="Convert"/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Hyperlink link)
            {
                return link.DisplayText;
            }

            return "";
        }

        /// <inheritdoc cref="Convert"/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
