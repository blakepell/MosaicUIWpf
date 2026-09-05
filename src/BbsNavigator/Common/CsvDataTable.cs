/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.VisualBasic.FileIO;
using System.Data;
using System.IO;
using System.Text;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Reads and writes a CSV file as a <see cref="DataTable"/> of string columns.
    /// </summary>
    /// <remarks>
    /// Every column is typed as <see cref="string"/> so that a round trip through the editor preserves
    /// the original text of each field rather than reformatting it through a parsed type.
    /// </remarks>
    internal static class CsvDataTable
    {
        /// <summary>
        /// Loads a CSV file into a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="fileName">The CSV file to read.</param>
        /// <param name="tableName">The name to give the resulting table.</param>
        public static DataTable Load(string fileName, string tableName)
        {
            using var parser = new TextFieldParser(fileName);
            return Load(parser, tableName);
        }

        /// <summary>
        /// Loads a CSV stream into a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="stream">The CSV stream to read.</param>
        /// <param name="tableName">The name to give the resulting table.</param>
        public static DataTable Load(Stream stream, string tableName)
        {
            using var parser = new TextFieldParser(stream);
            return Load(parser, tableName);
        }

        private static DataTable Load(TextFieldParser parser, string tableName)
        {
            parser.HasFieldsEnclosedInQuotes = true;
            parser.TextFieldType = FieldType.Delimited;
            parser.TrimWhiteSpace = false;
            parser.SetDelimiters(",");

            var table = new DataTable(tableName)
            {
                Locale = System.Globalization.CultureInfo.CurrentCulture
            };

            string[]? headers = parser.ReadFields();

            if (headers == null)
            {
                throw new InvalidDataException("The CSV file is empty.");
            }

            var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string header in headers)
            {
                string name = header.Trim().TrimStart('\uFEFF');

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Column{table.Columns.Count + 1}";
                }

                // The bblist format repeats a couple of header names; a DataTable requires them to be
                // unique, so duplicates are suffixed rather than dropped.
                string unique = name;
                int suffix = 2;

                while (!columnNames.Add(unique))
                {
                    unique = $"{name}{suffix++}";
                }

                table.Columns.Add(unique, typeof(string));
            }

            while (!parser.EndOfData)
            {
                string[]? fields;

                try
                {
                    fields = parser.ReadFields();
                }
                catch (MalformedLineException)
                {
                    // A row the parser cannot make sense of is skipped so the rest of the file still loads.
                    continue;
                }

                if (fields == null)
                {
                    continue;
                }

                DataRow row = table.NewRow();

                for (int i = 0; i < table.Columns.Count; i++)
                {
                    row[i] = i < fields.Length ? fields[i] : string.Empty;
                }

                table.Rows.Add(row);
            }

            table.AcceptChanges();

            return table;
        }

        /// <summary>
        /// Writes a <see cref="DataTable"/> out as a CSV file.
        /// </summary>
        /// <param name="table">The table to write.</param>
        /// <param name="fileName">The file to write it to.</param>
        public static void Save(DataTable table, string fileName)
        {
            using var writer = new StreamWriter(fileName, false, new UTF8Encoding(false));

            writer.WriteLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Quote(c.ColumnName))));

            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                {
                    continue;
                }

                writer.WriteLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => Quote(Convert.ToString(row[c]) ?? string.Empty))));
            }
        }

        /// <summary>
        /// Quotes a field when it contains a character that would otherwise change the shape of the row.
        /// </summary>
        private static string Quote(string value)
        {
            if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
