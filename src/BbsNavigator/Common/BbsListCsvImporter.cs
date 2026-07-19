/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using System.IO;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Imports BBS profiles from the bblist CSV format.
    /// </summary>
    internal static class BbsListCsvImporter
    {
        /// <summary>
        /// Reads importable Telnet profiles from a bblist CSV file.
        /// </summary>
        /// <param name="fileName">The CSV file to read.</param>
        /// <returns>The imported profiles and number of rows that were skipped.</returns>
        /// <exception cref="InvalidDataException">The CSV does not contain the required headers.</exception>
        public static BbsListImportResult Import(string fileName)
        {
            using var parser = new TextFieldParser(fileName);
            return Import(parser);
        }

        /// <summary>
        /// Reads importable Telnet profiles from a bblist CSV stream.
        /// </summary>
        /// <param name="stream">The CSV stream to read.</param>
        /// <returns>The imported profiles and number of rows that were skipped.</returns>
        /// <exception cref="InvalidDataException">The CSV does not contain the required headers.</exception>
        public static BbsListImportResult Import(Stream stream)
        {
            using var parser = new TextFieldParser(stream);
            return Import(parser);
        }

        private static BbsListImportResult Import(TextFieldParser parser)
        {
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TextFieldType = FieldType.Delimited;
            parser.TrimWhiteSpace = true;
            parser.SetDelimiters(",");

            string[]? headers = parser.ReadFields();
            if (headers == null)
            {
                throw new InvalidDataException("The CSV file is empty.");
            }

            Dictionary<string, int> columns = headers
                .Select((header, index) => new { Header = header.Trim().TrimStart('\uFEFF'), Index = index })
                .GroupBy(column => column.Header, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Index, StringComparer.OrdinalIgnoreCase);

            int nameIndex = GetRequiredColumn(columns, "bbsName");
            int hostIndex = GetRequiredColumn(columns, "TelnetAddress");
            int portIndex = GetRequiredColumn(columns, "bbsPort");
            var profiles = new List<BbsProfile>();
            int skippedCount = 0;

            while (!parser.EndOfData)
            {
                string[]? fields;
                try
                {
                    fields = parser.ReadFields();
                }
                catch (MalformedLineException)
                {
                    skippedCount++;
                    continue;
                }

                string host = GetField(fields, hostIndex);
                string portText = GetField(fields, portIndex);
                if (string.IsNullOrWhiteSpace(host) ||
                    !int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out int port) ||
                    port is < 1 or > 65535)
                {
                    skippedCount++;
                    continue;
                }

                string name = GetField(fields, nameIndex);
                profiles.Add(new BbsProfile
                {
                    Name = string.IsNullOrWhiteSpace(name) ? host : name,
                    Host = host,
                    Port = port
                });
            }

            return new BbsListImportResult(profiles, skippedCount);
        }

        private static int GetRequiredColumn(IReadOnlyDictionary<string, int> columns, string name)
        {
            if (columns.TryGetValue(name, out int index))
            {
                return index;
            }

            throw new InvalidDataException($"Required column '{name}' was not found.");
        }

        private static string GetField(string[]? fields, int index)
        {
            return fields != null && index < fields.Length ? fields[index].Trim() : string.Empty;
        }
    }

    /// <summary>
    /// Contains the outcome of a bblist CSV import.
    /// </summary>
    /// <param name="Profiles">Profiles that contain a valid Telnet endpoint.</param>
    /// <param name="SkippedCount">The number of rows that could not be imported.</param>
    internal sealed record BbsListImportResult(IReadOnlyList<BbsProfile> Profiles, int SkippedCount);
}
