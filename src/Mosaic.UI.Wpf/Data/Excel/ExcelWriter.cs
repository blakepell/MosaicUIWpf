/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using SpreadCheetah;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Mosaic.UI.Wpf.Data.Excel
{
    /// <summary>
    /// A left to right top to bottom spreadsheet implementation that can create large spreadsheets
    /// from supported data sets.
    /// </summary>
    public class ExcelWriter : IAsyncDisposable
    {
        /// <summary>
        /// The underlying abstracted stream type (e.g. could be a FileStream, could be a MemoryStream, etc.).
        /// </summary>
        private readonly Stream _stream;

        /// <summary>
        /// The underlying spreadsheet.
        /// </summary>
        private Spreadsheet? _spreadsheet;

        /// <summary>
        /// The file path if the class was initialized with the file path constructor.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Provides a key/pair lookup to swap out database column names for a friendly names.  The key should correspond
        /// to the field name the query returns and the value should be the name to swap in it's place.  E.g. a key of
        /// "log_time" might map to "Log Time".
        /// </summary>
        public Dictionary<string, string> ColumnHeaderMap { get; set; } = new();

        /// <summary>
        /// Tracks the number of sheets that have been added which also corresponds with the sheets SheetId if the
        /// AddSheet methods in this class were used to add the sheets.
        /// </summary>
        private int SheetId { get; set; } = 0;

        /// <summary>
        /// If the stream was passed in from the caller.
        /// </summary>
        private bool OutsideStream { get; set; } = false;

        /// <summary>
        /// Creates an Excel spreadsheet.
        /// </summary>
        /// <param name="filePath"></param>
        public ExcelWriter(string filePath)
        {
            this.FilePath = filePath;
            _stream = new FileStream(filePath, FileMode.Create);
        }

        /// <summary>
        /// Creates an Excel spreadsheet.
        /// </summary>
        /// <param name="s"></param>
        public ExcelWriter(Stream s)
        {
            this.OutsideStream = true;
            _stream = s;
        }

        /// <summary>
        /// Adds a sheet to the current spreadsheet from an IDataReader implementation.
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="workSheetName"></param>
        public async Task AddSheetAsync(IDataReader dr, string workSheetName)
        {
            // Validate the sheet name to make sure it's not illegal
            ValidateWorksheetName(workSheetName);

            // Increment the sheet number this is
            this.SheetId += 1;

            // If they didn't provide a sheet name put in the default name with the Incremented number behind it
            // like Excel would do.
            if (string.IsNullOrWhiteSpace(workSheetName))
            {
                workSheetName = $"Sheet{this.SheetId}";
            }

            // Initialize the spreadsheet if it hasn't already been initialized.
            _spreadsheet ??= await Spreadsheet.CreateNewAsync(_stream);

            // Start the worksheet that will be this dataset.
            await _spreadsheet.StartWorksheetAsync(workSheetName);

            var headerStyle = new SpreadCheetah.Styling.Style();
            headerStyle.Font.Bold = true;
            var headerStyleId = _spreadsheet.AddStyle(headerStyle);

            /*
             * Header Row
             */
            var row = new List<Cell>();

            for (int x = 0; x <= dr.FieldCount - 1; x++)
            {
                row.Add(new Cell(ColumnHeaderLookup(dr.GetName(x)), headerStyleId));
            }

            await _spreadsheet.AddRowAsync(row);

            /*
             * Add the data rows for each record.
             */
            while (dr.Read())
            {
                row.Clear();

                // Add value for each field in the DataReader.
                for (int i = 0; i <= dr.FieldCount - 1; i++)
                {
                    var fieldType = dr.GetFieldType(i);

                    if (dr.IsDBNull(i))
                    {
                        row.Add(new Cell());
                        continue;
                    }

                    switch (Type.GetTypeCode(fieldType))
                    {
                        case TypeCode.Int64:
                            row.Add(new Cell(dr.GetInt64(i)));
                            break;
                        case TypeCode.Int32:
                            row.Add(new Cell(dr.GetInt32(i)));
                            break;
                        case TypeCode.String:
                            row.Add(new Cell(dr.GetString(i)));
                            break;
                        case TypeCode.Boolean:
                            row.Add(new Cell(dr.GetBoolean(i)));
                            break;
                        case TypeCode.DateTime:
                            row.Add(new Cell(dr.GetDateTime(i)));
                            break;
                        case TypeCode.Decimal:
                            row.Add(new Cell(dr.GetDecimal(i)));
                            break;
                        case TypeCode.Double:
                            row.Add(new Cell(dr.GetDouble(i)));
                            break;
                        case TypeCode.Single:
                            row.Add(new Cell(dr.GetFloat(i)));
                            break;
                        case TypeCode.Byte:
                            row.Add(new Cell(dr.GetByte(i)));
                            break;
                        default:
                            row.Add(new Cell(dr[i].ToString()));
                            break;
                    }
                }

                await _spreadsheet.AddRowAsync(row);
            }
        }

        /// <summary>
        /// Adds a sheet to the current spreadsheet from an IDataReader implementation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="workSheetName"></param>
        public async Task AddSheetAsync<T>(IEnumerable<T> data, string workSheetName) where T : class
        {
            // Validate the sheet name to make sure it's not illegal
            ValidateWorksheetName(workSheetName);

            // Increment the sheet number this is
            this.SheetId += 1;

            // If they didn't provide a sheet name put in the default name with the Incremented number behind it
            // like Excel would do.
            if (string.IsNullOrWhiteSpace(workSheetName))
            {
                workSheetName = $"Sheet{this.SheetId}";
            }

            // Initialize the spreadsheet if it hasn't already been initialized.
            _spreadsheet ??= await Spreadsheet.CreateNewAsync(_stream);

            // Start the worksheet that will be this dataset.
            await _spreadsheet.StartWorksheetAsync(workSheetName);

            var headerStyle = new SpreadCheetah.Styling.Style();
            headerStyle.Font.Bold = true;
            var headerStyleId = _spreadsheet.AddStyle(headerStyle);

            /*
             * Header Row
             */
            var dataProperties = typeof(T).GetProperties();
            var row = new List<Cell>();

            foreach (var property in dataProperties)
            {
                row.Add(new Cell(ColumnHeaderLookup(property.Name), headerStyleId));
            }

            await _spreadsheet.AddRowAsync(row);

            /*
             * Add the data rows for each record.
             */
            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }

                row.Clear();

                // Add value for each property.
                // Add value for each property.
                foreach (var property in dataProperties)
                {
                    var fieldType = property.PropertyType;
                    var value = property?.GetValue(item, null);

                    if (value == null)
                    {
                        row.Add(new Cell());
                        continue;
                    }

                    switch (Type.GetTypeCode(fieldType))
                    {
                        case TypeCode.Int64:
                            row.Add(new Cell((long)value));
                            break;
                        case TypeCode.Int32:
                            row.Add(new Cell((int)value));
                            break;
                        case TypeCode.String:
                            row.Add(new Cell(value.ToString()));
                            break;
                        case TypeCode.Boolean:
                            row.Add(new Cell((bool)value));
                            break;
                        case TypeCode.DateTime:
                            row.Add(new Cell((DateTime)value));
                            break;
                        case TypeCode.Decimal:
                            row.Add(new Cell((decimal)value));
                            break;
                        case TypeCode.Double:
                            row.Add(new Cell((double)value));
                            break;
                        case TypeCode.Single:
                            row.Add(new Cell((float)value));
                            break;
                        case TypeCode.Byte:
                            row.Add(new Cell((byte)value));
                            break;
                        default:
                            row.Add(new Cell(value.ToString()));
                            break;
                    }
                }

                await _spreadsheet.AddRowAsync(row);
            }
        }

        /// <summary>
        /// Adds a sheet to the current spreadsheet from a DataTable.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="workSheetName"></param>
        public async Task AddSheetAsync(DataTable dt, string workSheetName)
        {
            await AddSheetAsync(dt.CreateDataReader(), workSheetName);
        }

        /// <summary>
        /// Checks whether a worksheet name is valid or not, if a worksheet name is invalid an Exception will
        /// be thrown with information as to why the name is invalid.  If the worksheet name is valid this method
        /// will silently succeed.
        /// </summary>
        /// <param name="worksheetName"></param>
        private void ValidateWorksheetName(string worksheetName)
        {
            if (worksheetName.Length > 31)
            {
                throw new Exception($"Maximum length of worksheet name exceeded:  A worksheet name must be 31 or less characters.  You attempted to create a worksheet name that was {worksheetName.Length.ToString()} characters long.");
            }

            if (worksheetName.IndexOfAny(new[] { '\\', '/', '*', '[', ']', ':', '?' }) != -1)
            {
                throw new Exception("Invalid worksheet name:  You attempted to create a worksheet name that contains an invalid character");
            }
        }

        /// <summary>
        /// Checks whether a worksheet name is valid or not.
        /// </summary>
        /// <param name="worksheetName"></param>
        public static bool WorksheetNameIsValid(string worksheetName)
        {
            if (worksheetName.Length > 31 || worksheetName.IndexOfAny(new[] { '\\', '/', '*', '[', ']', ':', '?' }) != -1)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Private lookup for the column header map that checks for whether it's set (not null) and then
        /// returns either the found mapped value or the passed in key if it is not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string ColumnHeaderLookup(string key)
        {
            return this.ColumnHeaderMap.GetValueOrDefault(key, key);
        }

        /// <summary>
        /// Dispose of resources from the class and close / finish up writing
        /// of the spreadsheet.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // If it's an internally created stream then dispose of it, otherwise it's the
            // caller's responsibility.
            if (!this.OutsideStream)
            {
                _stream?.DisposeAsync();
            }

            if (_spreadsheet != null)
            {
                await _spreadsheet.FinishAsync();
                await _spreadsheet.DisposeAsync();
            }
        }
    }
}
