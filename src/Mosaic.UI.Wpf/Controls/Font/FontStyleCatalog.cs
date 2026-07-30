/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows.Data;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides the shared list of the <see cref="FontStyle"/> values along with the lookup and
    /// item-template helpers used by <see cref="FontStyleComboBox"/>.
    /// </summary>
    /// <remarks>
    /// The list holds the three styles defined by <see cref="FontStyles"/>: <c>Normal</c>,
    /// <c>Italic</c>, and <c>Oblique</c>.
    /// </remarks>
    public static class FontStyleCatalog
    {
        private static readonly IReadOnlyList<FontStyle> AllStyles = new ReadOnlyCollection<FontStyle>(
        [
            FontStyles.Normal,
            FontStyles.Italic,
            FontStyles.Oblique
        ]);

        /// <summary>
        /// Gets the font styles, in the order <c>Normal</c>, <c>Italic</c>, <c>Oblique</c>.
        /// </summary>
        public static IReadOnlyList<FontStyle> Styles => AllStyles;

        /// <summary>
        /// Finds the style whose name matches <paramref name="name"/>, ignoring case.
        /// </summary>
        /// <param name="name">The style name to locate, for example <c>Italic</c>.</param>
        /// <returns>The matching style, or <c>null</c> when the name is not a known style.</returns>
        public static FontStyle? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            try
            {
                if (new FontStyleConverter().ConvertFromString(name.Trim()) is FontStyle converted)
                {
                    return Resolve(converted);
                }
            }
            catch (Exception)
            {
                // An unparsable name is not exceptional here, it simply means no match.
            }

            return null;
        }

        /// <summary>
        /// Finds the listed style equal to <paramref name="fontStyle"/> so a value created by a
        /// consumer participates in selection.
        /// </summary>
        /// <param name="fontStyle">The style to resolve.</param>
        /// <returns>The matching listed style, or <c>null</c> when the style is not one of the known styles.</returns>
        public static FontStyle? Resolve(FontStyle fontStyle)
        {
            foreach (var style in AllStyles)
            {
                if (style == fontStyle)
                {
                    return style;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a <see cref="DataTemplate"/> that displays a <see cref="FontStyle"/> by name,
        /// rendered in the style itself, so the drop-down doubles as a style preview.
        /// </summary>
        /// <param name="fontSize">The font size used to render the preview text.</param>
        /// <remarks>
        /// The size is baked into the returned template rather than bound, because the template is
        /// applied inside a popup where an ancestor binding to the owning control is not guaranteed
        /// to resolve.  Callers rebuild the template when their preview size changes.
        /// </remarks>
        internal static DataTemplate CreatePreviewTemplate(double fontSize)
        {
            var textBlock = new FrameworkElementFactory(typeof(TextBlock));

            // An empty path binds to the data item itself, which is the FontStyle being displayed;
            // its ToString() is the style name, for example "Italic".
            textBlock.SetBinding(TextBlock.TextProperty, new Binding());
            textBlock.SetBinding(TextBlock.FontStyleProperty, new Binding());

            textBlock.SetValue(TextBlock.FontSizeProperty, fontSize);
            textBlock.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            textBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var template = new DataTemplate(typeof(FontStyle)) { VisualTree = textBlock };
            template.Seal();

            return template;
        }
    }
}
