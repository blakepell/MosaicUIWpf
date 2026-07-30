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
    /// <see cref="JsonConverter"/> for a WPF <see cref="FontWeight"/>.
    /// </summary>
    /// <remarks>
    /// [JsonConverter(typeof(FontWeightJsonSerializer))]
    /// </remarks>
    public class FontWeightJsonSerializer : JsonConverter<FontWeight>
    {
        /// <inheritdoc/>
        public override FontWeight Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? fontWeight = reader.GetString();

            if (string.IsNullOrWhiteSpace(fontWeight))
            {
                return FontWeights.Normal;
            }

            return FontWeightCatalog.Find(fontWeight) ?? FontWeights.Normal;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, FontWeight value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
