/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text.Json;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// <see cref="JsonConverter"/> for a WPF <see cref="FontFamily"/>.
    /// </summary>
    /// <remarks>
    /// [JsonConverter(typeof(FontFamilyJsonConverter))]
    /// </remarks>
    public class FontFamilyJsonSerializer : JsonConverter<FontFamily>
    {
        /// <inheritdoc/>
        public override FontFamily? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? fontFamily = reader.GetString();

            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                return null;
            }

            return new FontFamily(fontFamily);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, FontFamily? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                string colorString = value.Source;
                writer.WriteStringValue(colorString);
            }
        }
    }
}
