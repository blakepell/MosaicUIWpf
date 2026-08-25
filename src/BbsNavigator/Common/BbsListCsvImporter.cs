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
        /// The port assumed for a row that does not list a Telnet or an SSH port.
        /// </summary>
        private const int DefaultTelnetPort = 23;


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
            int sshPortIndex = GetOptionalColumn(columns, "sshPort");
            var profiles = new List<BbsProfile>();
            var seenEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                if (string.IsNullOrWhiteSpace(host))
                {
                    skippedCount++;
                    continue;
                }

                // The sshPort column is optional and frequently blank; a missing or invalid value means the BBS has no SSH endpoint.
                bool hasSshPort = TryGetPort(fields, sshPortIndex, out int sshPort);
                if (!TryGetPort(fields, portIndex, out int port))
                {
                    // A row that lists neither port is assumed to be a plain Telnet board on the well-known port.
                    // A row that lists only an SSH port keeps a Telnet port of zero rather than being given
                    // one it never advertised; the profile then connects over SSH alone.
                    port = hasSshPort ? 0 : DefaultTelnetPort;
                }

                string name = GetField(fields, nameIndex);
                var profile = new BbsProfile
                {
                    Name = string.IsNullOrWhiteSpace(name) ? host : name,
                    Host = host,
                    Port = port,
                    SshPort = sshPort
                };

                // The bblist format repeats a handful of endpoints under different names. Keeping the first
                // occurrence makes a repeated import of the same file idempotent instead of flip-flopping
                // those profiles between the duplicate rows' values.
                if (!seenEndpoints.Add(port > 0 ? $"{host}:{port}" : $"{host}:ssh{sshPort}"))
                {
                    skippedCount++;
                    continue;
                }

                profiles.Add(profile);
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

        /// <summary>
        /// Reads a column as a valid TCP port number.
        /// </summary>
        /// <param name="fields">The fields of the current row.</param>
        /// <param name="index">The column index to read, or -1 when the column is absent.</param>
        /// <param name="port">When this method returns, contains the port, or zero when none was present.</param>
        /// <returns><see langword="true"/> when the column held a port between 1 and 65535; otherwise, <see langword="false"/>.</returns>
        private static bool TryGetPort(string[]? fields, int index, out int port)
        {
            string text = GetField(fields, index);

            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out port) &&
                port is >= 1 and <= 65535)
            {
                return true;
            }

            port = 0;
            return false;
        }

        /// <summary>
        /// Returns the index of an optional column, or -1 when the column is not present.
        /// </summary>
        private static int GetOptionalColumn(IReadOnlyDictionary<string, int> columns, string name)
        {
            return columns.TryGetValue(name, out int index) ? index : -1;
        }

        private static string GetField(string[]? fields, int index)
        {
            return fields != null && index >= 0 && index < fields.Length ? fields[index].Trim() : string.Empty;
        }
    }

    /// <summary>
    /// Contains the outcome of a bblist CSV import.
    /// </summary>
    /// <param name="Profiles">Profiles that contain a valid Telnet endpoint.</param>
    /// <param name="SkippedCount">The number of rows that could not be imported.</param>
    internal sealed record BbsListImportResult(IReadOnlyList<BbsProfile> Profiles, int SkippedCount);
}
