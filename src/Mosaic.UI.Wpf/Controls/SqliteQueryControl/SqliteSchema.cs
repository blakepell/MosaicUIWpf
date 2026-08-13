/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The schema of a SQLite database as of the last refresh, used as the data source for the
    /// database explorer in <see cref="SqliteQueryControl"/>.
    /// </summary>
    public partial class SqliteSchema : ObservableObject
    {
        /// <summary>
        /// The database file name, shown as the root node of the explorer.
        /// </summary>
        [ObservableProperty]
        public partial string? DatabaseName { get; set; }

        /// <summary>
        /// The data source the schema was read from, shown as the root node's tool tip.
        /// </summary>
        [ObservableProperty]
        public partial string? ConnectionString { get; set; }

        /// <summary>
        /// The tables in the database, ordered by name.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<SqliteTable> Tables { get; set; } = new();

        /// <summary>
        /// The views in the database, ordered by name.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<SqliteView> Views { get; set; } = new();
    }

    /// <summary>
    /// The metadata for a single SQLite table.
    /// </summary>
    public partial class SqliteTable : ObservableObject
    {
        /// <summary>
        /// The table name.
        /// </summary>
        [ObservableProperty]
        public partial string? Name { get; set; }

        /// <summary>
        /// The <c>CREATE TABLE</c> statement SQLite reports for this table.
        /// </summary>
        [ObservableProperty]
        public partial string? Sql { get; set; }

        /// <summary>
        /// The fields contained in this table, in ordinal order.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<SqliteField> Fields { get; set; } = new();
    }

    /// <summary>
    /// The metadata for a single SQLite view.
    /// </summary>
    public partial class SqliteView : ObservableObject
    {
        /// <summary>
        /// The view name.
        /// </summary>
        [ObservableProperty]
        public partial string? Name { get; set; }

        /// <summary>
        /// The <c>CREATE VIEW</c> statement SQLite reports for this view.
        /// </summary>
        [ObservableProperty]
        public partial string? Sql { get; set; }

        /// <summary>
        /// The fields projected by this view, in ordinal order.
        /// </summary>
        [ObservableProperty]
        public partial ObservableCollection<SqliteField> Fields { get; set; } = new();
    }

    /// <summary>
    /// The metadata for a single column of a SQLite table or view, as reported by
    /// <c>PRAGMA table_info</c>.
    /// </summary>
    public partial class SqliteField : ObservableObject
    {
        /// <summary>
        /// The zero based column ordinal.
        /// </summary>
        [ObservableProperty]
        public partial int ColumnId { get; set; }

        /// <summary>
        /// The column name.
        /// </summary>
        [ObservableProperty]
        public partial string? Name { get; set; }

        /// <summary>
        /// The declared type of the column. SQLite is dynamically typed, so this is the declared
        /// type affinity rather than a guarantee about the stored values.
        /// </summary>
        [ObservableProperty]
        public partial string? Type { get; set; }

        /// <summary>
        /// Whether the column is declared <c>NOT NULL</c>.
        /// </summary>
        [ObservableProperty]
        public partial bool NotNull { get; set; }

        /// <summary>
        /// The default value expression for the column, when one is declared.
        /// </summary>
        [ObservableProperty]
        public partial string? DefaultValue { get; set; }

        /// <summary>
        /// Whether the column participates in the table's primary key.
        /// </summary>
        [ObservableProperty]
        public partial bool PrimaryKey { get; set; }
    }
}
