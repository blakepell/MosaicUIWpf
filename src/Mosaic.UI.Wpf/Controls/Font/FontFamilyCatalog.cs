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
    /// Provides the shared, cached list of <see cref="FontFamily"/> instances installed on the
    /// system along with the lookup and item-template helpers used by <see cref="FontComboBox"/>
    /// and <see cref="FontAutoCompleteBox"/>.
    /// </summary>
    /// <remarks>
    /// Enumerating <see cref="Fonts.SystemFontFamilies"/> is comparatively expensive, so the list is
    /// built once on first access and shared by every font control in the application.  The list is
    /// sorted by <see cref="FontFamily.Source"/> using the current culture and is not refreshed if
    /// fonts are installed or removed while the application is running.
    /// </remarks>
    public static class FontFamilyCatalog
    {
        private static readonly Lazy<IReadOnlyList<FontFamily>> LazyFamilies =
            new(BuildFamilies, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the system font families, sorted by name.  The first access enumerates the installed
        /// fonts; every subsequent access returns the cached list.
        /// </summary>
        public static IReadOnlyList<FontFamily> Families => LazyFamilies.Value;

        /// <summary>
        /// Finds the cached <see cref="FontFamily"/> whose <see cref="FontFamily.Source"/> matches
        /// <paramref name="name"/>, ignoring case.
        /// </summary>
        /// <param name="name">The font family name to locate, for example <c>Segoe UI</c>.</param>
        /// <returns>The matching family, or <c>null</c> when the font is not installed.</returns>
        public static FontFamily? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            foreach (var family in Families)
            {
                if (string.Equals(family.Source, name, StringComparison.OrdinalIgnoreCase))
                {
                    return family;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the cached <see cref="FontFamily"/> equivalent to <paramref name="fontFamily"/>.
        /// Matching is performed on the family name so that a family created by a consumer (for
        /// example <c>new FontFamily("Arial")</c>) resolves to the instance held in the catalog and
        /// can therefore participate in selection.
        /// </summary>
        /// <param name="fontFamily">The font family to resolve.</param>
        /// <returns>The matching cached family, or <c>null</c> when the font is not installed.</returns>
        public static FontFamily? Resolve(FontFamily? fontFamily)
        {
            return fontFamily == null ? null : Find(fontFamily.Source);
        }

        /// <summary>
        /// Builds a <see cref="DataTemplate"/> that displays a <see cref="FontFamily"/> by name,
        /// rendered in the font itself, so the drop-down doubles as a font preview.
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
            textBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(FontFamily.Source)));

            // An empty path binds to the data item itself, which is the FontFamily being displayed.
            textBlock.SetBinding(TextBlock.FontFamilyProperty, new Binding());

            textBlock.SetValue(TextBlock.FontSizeProperty, fontSize);
            textBlock.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            textBlock.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var template = new DataTemplate(typeof(FontFamily)) { VisualTree = textBlock };
            template.Seal();

            return template;
        }

        /// <summary>
        /// Enumerates the installed font families into a sorted, read-only list.
        /// </summary>
        private static IReadOnlyList<FontFamily> BuildFamilies()
        {
            var families = Fonts.SystemFontFamilies
                .Where(family => !string.IsNullOrWhiteSpace(family.Source))
                .OrderBy(family => family.Source, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new ReadOnlyCollection<FontFamily>(families);
        }
    }
}
