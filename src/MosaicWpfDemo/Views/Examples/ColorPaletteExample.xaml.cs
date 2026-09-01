/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Themes;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates Mosaic's theme-independent Foundation color palette.
    /// </summary>
    public partial class ColorPaletteExample
    {
        private static readonly string[] PaletteNames =
        [
            "Blue",
            "Indigo",
            "Purple",
            "Pink",
            "Red",
            "Orange",
            "Yellow",
            "Green",
            "Teal",
            "Cyan",
            "Gray"
        ];

        private static readonly int[] ShadeValues = [100, 200, 300, 400, 500, 600, 700, 800, 900];

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPaletteExample"/> class.
        /// </summary>
        public ColorPaletteExample()
        {
            InitializeComponent();

            Palettes = PaletteNames
                .Select(CreatePaletteRow)
                .ToArray();

            DataContext = this;
        }

        /// <summary>
        /// Gets the palette rows displayed by the example.
        /// </summary>
        /// <value>The ordered collection of palette rows.</value>
        public IReadOnlyList<PaletteRow> Palettes { get; }

        /// <summary>
        /// Copies the selected swatch's XAML dynamic-resource expression to the clipboard.
        /// </summary>
        /// <param name="sender">The tile or context-menu item whose data context identifies the swatch.</param>
        /// <param name="e">The routed click event data.</param>
        private void CopyDynamicResource_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: PaletteSwatch swatch })
            {
                return;
            }

            Clipboard.SetText(swatch.ResourceExpression);
            CopyStatusText.Text = $"Copied {swatch.ShortName}: {swatch.ResourceExpression}";
            e.Handled = true;
        }

        /// <summary>
        /// Creates one named palette row from its registered Foundation resources.
        /// </summary>
        /// <param name="paletteName">The Pascal-case palette family name.</param>
        /// <returns>A palette row containing the complete 100 through 900 scale.</returns>
        /// <exception cref="InvalidOperationException">A required palette resource is unavailable.</exception>
        private PaletteRow CreatePaletteRow(string paletteName)
        {
            var swatches = ShadeValues
                .Select(shade => CreatePaletteSwatch(paletteName, shade))
                .ToArray();

            var baseSwatch = swatches.Single(swatch => swatch.Shade == 500);

            return new PaletteRow(
                paletteName.ToLowerInvariant(),
                swatches,
                baseSwatch.Brush,
                baseSwatch.ForegroundBrush);
        }

        /// <summary>
        /// Creates a swatch by resolving its strongly typed color and brush resource keys.
        /// </summary>
        /// <param name="paletteName">The Pascal-case palette family name.</param>
        /// <param name="shade">The palette shade number.</param>
        /// <returns>A swatch backed by the registered Foundation resources.</returns>
        /// <exception cref="InvalidOperationException">A required palette resource is unavailable or has an unexpected type.</exception>
        private PaletteSwatch CreatePaletteSwatch(string paletteName, int shade)
        {
            string colorPropertyName = $"{paletteName}{shade}BackgroundColorKey";
            string brushPropertyName = $"{paletteName}{shade}BackgroundBrushKey";
            var colorKey = GetResourceKey(colorPropertyName);
            var brushKey = GetResourceKey(brushPropertyName);

            if (TryFindResource(colorKey) is not Color color)
            {
                throw new InvalidOperationException($"The palette color resource '{colorPropertyName}' is unavailable.");
            }

            if (TryFindResource(brushKey) is not Brush brush)
            {
                throw new InvalidOperationException($"The palette brush resource '{brushPropertyName}' is unavailable.");
            }

            string resourceExpression = $"{{DynamicResource {{x:Static themes:AssetResourceKeys.{brushPropertyName}}}}}";

            return new PaletteSwatch(
                shade,
                $"{paletteName.ToLowerInvariant()}-{shade}",
                FormatHex(color),
                brush,
                GetReadableForeground(color),
                resourceExpression);
        }

        /// <summary>
        /// Resolves a public static resource-key property by name.
        /// </summary>
        /// <param name="propertyName">The resource-key property name.</param>
        /// <returns>The component resource key exposed by the property.</returns>
        /// <exception cref="InvalidOperationException">The requested property is missing or does not expose a component resource key.</exception>
        private static ComponentResourceKey GetResourceKey(string propertyName)
        {
            PropertyInfo? property = typeof(AssetResourceKeys).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Static);

            if (property?.GetValue(null) is ComponentResourceKey resourceKey)
            {
                return resourceKey;
            }

            throw new InvalidOperationException($"AssetResourceKeys.{propertyName} is not available.");
        }

        /// <summary>
        /// Formats a WPF color as a conventional hexadecimal color string.
        /// </summary>
        /// <param name="color">The color to format.</param>
        /// <returns>A six-digit RGB string for opaque colors; otherwise, an eight-digit ARGB string.</returns>
        private static string FormatHex(Color color)
        {
            return color.A == byte.MaxValue
                ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B)
                : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Selects black or white text according to WCAG relative luminance.
        /// </summary>
        /// <param name="background">The background color behind the text.</param>
        /// <returns>A shared brush that provides the stronger contrast.</returns>
        private static Brush GetReadableForeground(Color background)
        {
            double luminance =
                (0.2126 * ToLinearColorChannel(background.R)) +
                (0.7152 * ToLinearColorChannel(background.G)) +
                (0.0722 * ToLinearColorChannel(background.B));

            return luminance > 0.179 ? Brushes.Black : Brushes.White;
        }

        /// <summary>
        /// Converts an sRGB color channel to its linear-light value.
        /// </summary>
        /// <param name="channel">The eight-bit sRGB channel value.</param>
        /// <returns>The normalized linear-light value.</returns>
        private static double ToLinearColorChannel(byte channel)
        {
            double normalized = channel / 255d;
            return normalized <= 0.04045
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Describes one named row in the Foundation palette.
        /// </summary>
        public sealed class PaletteRow
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PaletteRow"/> class.
            /// </summary>
            /// <param name="displayName">The lowercase palette family name.</param>
            /// <param name="swatches">The ordered palette swatches.</param>
            /// <param name="headerBrush">The brush used by the row header.</param>
            /// <param name="headerForegroundBrush">The contrasting brush used by the row header text.</param>
            public PaletteRow(
                string displayName,
                IReadOnlyList<PaletteSwatch> swatches,
                Brush headerBrush,
                Brush headerForegroundBrush)
            {
                DisplayName = displayName;
                Swatches = swatches;
                HeaderBrush = headerBrush;
                HeaderForegroundBrush = headerForegroundBrush;
            }

            /// <summary>
            /// Gets the lowercase palette family name.
            /// </summary>
            /// <value>The palette family name displayed in the row header.</value>
            public string DisplayName { get; }

            /// <summary>
            /// Gets the ordered swatches in the palette row.
            /// </summary>
            /// <value>The complete 100 through 900 scale.</value>
            public IReadOnlyList<PaletteSwatch> Swatches { get; }

            /// <summary>
            /// Gets the brush used by the row header.
            /// </summary>
            /// <value>The palette's 500-level background brush.</value>
            public Brush HeaderBrush { get; }

            /// <summary>
            /// Gets the contrasting brush used by the row header text.
            /// </summary>
            /// <value>A black or white foreground brush.</value>
            public Brush HeaderForegroundBrush { get; }
        }

        /// <summary>
        /// Describes one color swatch and its copyable XAML resource expression.
        /// </summary>
        public sealed class PaletteSwatch
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PaletteSwatch"/> class.
            /// </summary>
            /// <param name="shade">The palette shade number.</param>
            /// <param name="shortName">The short palette resource name.</param>
            /// <param name="hex">The displayed hexadecimal color value.</param>
            /// <param name="brush">The swatch background brush.</param>
            /// <param name="foregroundBrush">The contrasting text brush.</param>
            /// <param name="resourceExpression">The copyable XAML dynamic-resource expression.</param>
            public PaletteSwatch(
                int shade,
                string shortName,
                string hex,
                Brush brush,
                Brush foregroundBrush,
                string resourceExpression)
            {
                Shade = shade;
                ShortName = shortName;
                Hex = hex;
                Brush = brush;
                ForegroundBrush = foregroundBrush;
                ResourceExpression = resourceExpression;
            }

            /// <summary>
            /// Gets the palette shade number.
            /// </summary>
            /// <value>A value from 100 through 900 in 100-point increments.</value>
            public int Shade { get; }

            /// <summary>
            /// Gets the short palette resource name.
            /// </summary>
            /// <value>A lowercase name such as <c>blue-500</c>.</value>
            public string ShortName { get; }

            /// <summary>
            /// Gets the displayed hexadecimal color value.
            /// </summary>
            /// <value>The color in RGB or ARGB hexadecimal notation.</value>
            public string Hex { get; }

            /// <summary>
            /// Gets the swatch background brush.
            /// </summary>
            /// <value>The brush resolved from the Foundation palette.</value>
            public Brush Brush { get; }

            /// <summary>
            /// Gets the contrasting text brush.
            /// </summary>
            /// <value>A black or white foreground brush.</value>
            public Brush ForegroundBrush { get; }

            /// <summary>
            /// Gets the copyable XAML dynamic-resource expression.
            /// </summary>
            /// <value>The complete expression for the swatch's strongly typed brush key.</value>
            public string ResourceExpression { get; }
        }
    }
}
