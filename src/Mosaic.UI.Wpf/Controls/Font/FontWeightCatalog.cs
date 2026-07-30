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
    /// Provides the shared list of the standard <see cref="FontWeight"/> values along with the
    /// lookup and item-template helpers used by <see cref="FontWeightComboBox"/>.
    /// </summary>
    /// <remarks>
    /// The list holds the ten distinct weights defined by <see cref="FontWeights"/>, ordered from
    /// lightest to heaviest.  The aliases (<c>Regular</c>, <c>DemiBold</c>, <c>UltraBold</c>,
    /// <c>Heavy</c>, <c>UltraBlack</c>, <c>UltraLight</c>) are not listed separately because they
    /// resolve to the same OpenType weights as their canonical counterparts, but they are still
    /// accepted by <see cref="Find"/>.
    /// </remarks>
    public static class FontWeightCatalog
    {
        private static readonly IReadOnlyList<FontWeight> AllWeights = new ReadOnlyCollection<FontWeight>(
        [
            FontWeights.Thin,
            FontWeights.ExtraLight,
            FontWeights.Light,
            FontWeights.Normal,
            FontWeights.Medium,
            FontWeights.SemiBold,
            FontWeights.Bold,
            FontWeights.ExtraBold,
            FontWeights.Black,
            FontWeights.ExtraBlack
        ]);

        /// <summary>
        /// Gets the standard font weights, ordered from lightest to heaviest.
        /// </summary>
        public static IReadOnlyList<FontWeight> Weights => AllWeights;

        /// <summary>
        /// Finds the weight whose name matches <paramref name="name"/>, ignoring case.  Aliases such
        /// as <c>Regular</c> or <c>DemiBold</c> resolve to their canonical weight.
        /// </summary>
        /// <param name="name">The weight name to locate, for example <c>SemiBold</c>.</param>
        /// <returns>The matching weight, or <c>null</c> when the name is not a known weight.</returns>
        public static FontWeight? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            try
            {
                // The framework converter understands both the canonical names and the aliases, as
                // well as the numeric OpenType weights (for example "600").
                if (new FontWeightConverter().ConvertFromString(name.Trim()) is FontWeight converted)
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
        /// Finds the listed weight equal to <paramref name="fontWeight"/> so a value created by a
        /// consumer participates in selection.
        /// </summary>
        /// <param name="fontWeight">The weight to resolve.</param>
        /// <returns>The matching listed weight, or <c>null</c> when the weight is not one of the standard weights.</returns>
        public static FontWeight? Resolve(FontWeight fontWeight)
        {
            foreach (var weight in AllWeights)
            {
                if (weight == fontWeight)
                {
                    return weight;
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a <see cref="DataTemplate"/> that displays a <see cref="FontWeight"/> by name,
        /// rendered in the weight itself, so the drop-down doubles as a weight preview.
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

            // An empty path binds to the data item itself, which is the FontWeight being displayed;
            // its ToString() is the weight name, for example "SemiBold".
            textBlock.SetBinding(TextBlock.TextProperty, new Binding());
            textBlock.SetBinding(TextBlock.FontWeightProperty, new Binding());

            textBlock.SetValue(TextBlock.FontSizeProperty, fontSize);
            textBlock.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            textBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var template = new DataTemplate(typeof(FontWeight)) { VisualTree = textBlock };
            template.Seal();

            return template;
        }
    }
}
