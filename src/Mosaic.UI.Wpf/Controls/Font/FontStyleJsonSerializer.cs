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
    /// <see cref="JsonConverter"/> for a WPF <see cref="FontStyle"/>.
    /// </summary>
    /// <remarks>
    /// [JsonConverter(typeof(FontStyleJsonSerializer))]
    /// </remarks>
    public class FontStyleJsonSerializer : JsonConverter<FontStyle>
    {
        /// <inheritdoc/>
        public override FontStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? fontStyle = reader.GetString();

            if (string.IsNullOrWhiteSpace(fontStyle))
            {
                return FontStyles.Normal;
            }

            return FontStyleCatalog.Find(fontStyle) ?? FontStyles.Normal;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, FontStyle value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
