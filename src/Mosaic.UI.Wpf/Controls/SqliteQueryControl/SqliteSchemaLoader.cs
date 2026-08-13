/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Reads the table, view and column metadata out of a SQLite database using plain ADO.NET.
    /// </summary>
    internal static class SqliteSchemaLoader
    {
        private const string TableQuery = "SELECT name, sql FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' ORDER BY name";
        private const string ViewQuery = "SELECT name, sql FROM sqlite_master WHERE type = 'view' ORDER BY name";

        // pragma_table_info is a table valued function, so unlike the bare PRAGMA statement it
        // accepts a bound parameter and needs no identifier quoting. Columns are, in order:
        // cid, name, type, notnull, dflt_value, pk.
        private const string FieldQuery = "SELECT * FROM pragma_table_info($name)";

        /// <summary>
        /// Loads the complete schema for the specified connection string.
        /// </summary>
        /// <param name="connectionString">The SQLite connection string to read from.</param>
        /// <param name="cancellationToken">A token used to cancel the load.</param>
        /// <returns>The schema as of this call.</returns>
        internal static async Task<SqliteSchema> LoadAsync(string connectionString, CancellationToken cancellationToken)
        {
            var schema = new SqliteSchema();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            schema.ConnectionString = connection.DataSource;
            schema.DatabaseName = string.IsNullOrEmpty(connection.DataSource)
                ? "(in memory)"
                : Path.GetFileName(connection.DataSource);

            foreach (var (name, sql) in await ReadObjectsAsync(connection, TableQuery, cancellationToken).ConfigureAwait(false))
            {
                schema.Tables.Add(new SqliteTable
                {
                    Name = name,
                    Sql = sql,
                    Fields = await ReadFieldsAsync(connection, name, cancellationToken).ConfigureAwait(false)
                });
            }

            foreach (var (name, sql) in await ReadObjectsAsync(connection, ViewQuery, cancellationToken).ConfigureAwait(false))
            {
                schema.Views.Add(new SqliteView
                {
                    Name = name,
                    Sql = sql,
                    Fields = await ReadFieldsAsync(connection, name, cancellationToken).ConfigureAwait(false)
                });
            }

            return schema;
        }

        /// <summary>
        /// Reads the name and creation SQL of every object returned by the specified sqlite_master query.
        /// </summary>
        /// <param name="connection">An open connection.</param>
        /// <param name="commandText">The query to run.</param>
        /// <param name="cancellationToken">A token used to cancel the read.</param>
        /// <returns>The name and creation SQL of each object.</returns>
        private static async Task<List<(string Name, string? Sql)>> ReadObjectsAsync(SqliteConnection connection, string commandText, CancellationToken cancellationToken)
        {
            var results = new List<(string, string?)>();

            await using var command = connection.CreateCommand();
            command.CommandText = commandText;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                results.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            return results;
        }

        /// <summary>
        /// Reads the column metadata for a single table or view.
        /// </summary>
        /// <param name="connection">An open connection.</param>
        /// <param name="objectName">The table or view to describe.</param>
        /// <param name="cancellationToken">A token used to cancel the read.</param>
        /// <returns>The columns in ordinal order.</returns>
        private static async Task<ObservableCollection<SqliteField>> ReadFieldsAsync(SqliteConnection connection, string objectName, CancellationToken cancellationToken)
        {
            var fields = new ObservableCollection<SqliteField>();

            await using var command = connection.CreateCommand();
            command.CommandText = FieldQuery;
            command.Parameters.AddWithValue("$name", objectName);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                fields.Add(new SqliteField
                {
                    ColumnId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Type = reader.IsDBNull(2) ? null : reader.GetString(2),
                    NotNull = !reader.IsDBNull(3) && reader.GetInt64(3) != 0,
                    DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    PrimaryKey = !reader.IsDBNull(5) && reader.GetInt64(5) != 0
                });
            }

            return fields;
        }

        /// <summary>
        /// Executes arbitrary SQL and materializes the first result set into a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="connectionString">The SQLite connection string to run against.</param>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="cancellationToken">A token used to cancel the query.</param>
        /// <returns>The result set. Statements that return no rows produce an empty table.</returns>
        internal static async Task<DataTable> ExecuteDataTableAsync(string connectionString, string sql, CancellationToken cancellationToken)
        {
            var dataTable = new DataTable();

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            // Microsoft.Data.Sqlite is synchronous underneath, so a long running statement only
            // observes the token between steps. Cancel() interrupts the native call itself.
            await using var registration = cancellationToken.Register(static state => ((SqliteCommand)state!).Cancel(), command);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            // Loading through a DataSet with constraints disabled matters: a joined query can legally
            // return the same key many times, which would otherwise trip DataTable's constraint logic.
            using (var dataSet = new DataSet { EnforceConstraints = false })
            {
                dataSet.Tables.Add(dataTable);
                dataTable.BeginLoadData();
                dataTable.Load(reader);
                dataTable.EndLoadData();
                dataSet.Tables.Remove(dataTable);
            }

            cancellationToken.ThrowIfCancellationRequested();

            return dataTable;
        }
    }
}
