/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text.Json;
using Mosaic.UI.Wpf.Cache;

namespace Mosaic.UI.Wpf.Json
{
    /// <summary>
    /// <see cref="JsonConverter"/> for a WPF <see cref="SolidColorBrush"/>.
    /// </summary>
    /// <remarks>
    /// [JsonConverter(typeof(SolidColorBrushJsonConverter))]
    /// </remarks>
    public class SolidColorBrushJsonConverter : JsonConverter<SolidColorBrush>
    {
        /// <inheritdoc/>
        public override SolidColorBrush Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var colorString = reader.GetString();

            if (string.IsNullOrWhiteSpace(colorString))
            {
                return null;
            }

            return ColorPaletteCache.GetBrush(colorString);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, SolidColorBrush? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                var colorString = value.Color.ToString();
                writer.WriteStringValue(colorString);
            }
        }
    }
}
