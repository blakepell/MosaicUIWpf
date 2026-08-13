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
using Mosaic.UI.Wpf.Controls;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates <see cref="SqliteQueryControl"/> against a small scratch database created on load.
    /// </summary>
    public partial class SqliteQueryControlExample
    {
        private static readonly string DatabasePath = Path.Combine(Path.GetTempPath(), "MosaicWpfDemo", "sample.db");

        public SqliteQueryControlExample()
        {
            InitializeComponent();
            this.Loaded += this.OnLoaded;
        }

        /// <summary>
        /// Seeds the sample database the first time the example is shown and points the control at it.
        /// </summary>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // The side menu keeps example views alive, so only do this once.
            this.Loaded -= this.OnLoaded;

            try
            {
                CreateSampleDatabase();
                await this.QueryControl.OpenDatabaseAsync(DatabasePath);

                this.QueryControl.QueryText =
                    "SELECT a.Name AS Artist, al.Title, al.ReleaseYear" + Environment.NewLine +
                    "FROM Album al" + Environment.NewLine +
                    "     INNER JOIN Artist a ON a.ArtistId = al.ArtistId" + Environment.NewLine +
                    "ORDER BY a.Name, al.ReleaseYear;";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The sample database could not be created.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "SqliteQueryControl", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Appends an example specific item to the database explorer's context menu, demonstrating the
        /// extensibility point. The standard items are already in the menu at this point.
        /// </summary>
        private void OnSchemaContextMenuRequested(object? sender, SqliteSchemaContextMenuEventArgs e)
        {
            if (e.NodeKind != SqliteSchemaNodeKind.Table || string.IsNullOrEmpty(e.ObjectName))
            {
                return;
            }

            string objectName = e.ObjectName;

            e.ContextMenu.Items.Add(new Separator());

            var item = new MenuItem { Header = "Count Rows (added by the demo)" };
            item.Click += async (_, _) => await this.QueryControl.ExecuteQueryAsync($"SELECT COUNT(*) AS RowCount_ FROM [{objectName}];");
            e.ContextMenu.Items.Add(item);
        }

        /// <summary>
        /// Creates the scratch database, replacing any copy left over from a previous run so the demo
        /// always starts from a known state.
        /// </summary>
        private static void CreateSampleDatabase()
        {
            string? directory = Path.GetDirectoryName(DatabasePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Pooling keeps the file handle open, which would defeat the delete below.
            SqliteConnection.ClearAllPools();

            if (File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
            }

            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE Artist (
                    ArtistId   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Name       TEXT    NOT NULL,
                    Country    TEXT,
                    FormedYear INTEGER
                );

                CREATE TABLE Album (
                    AlbumId     INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ArtistId    INTEGER NOT NULL REFERENCES Artist (ArtistId),
                    Title       TEXT    NOT NULL,
                    ReleaseYear INTEGER,
                    TrackCount  INTEGER DEFAULT 0,
                    Rating      REAL
                );

                CREATE VIEW AlbumsByArtist AS
                SELECT a.Name AS ArtistName, al.Title AS AlbumTitle, al.ReleaseYear, al.Rating
                FROM Album al
                     INNER JOIN Artist a ON a.ArtistId = al.ArtistId;

                INSERT INTO Artist (Name, Country, FormedYear) VALUES
                    ('Aphex Twin', 'UK', 1988),
                    ('Boards of Canada', 'UK', 1986),
                    ('Tycho', 'US', 2001),
                    ('Explosions in the Sky', 'US', 1999),
                    ('Godspeed You! Black Emperor', 'CA', 1994);

                INSERT INTO Album (ArtistId, Title, ReleaseYear, TrackCount, Rating) VALUES
                    (1, 'Selected Ambient Works 85-92', 1992, 13, 4.8),
                    (1, 'Richard D. James Album', 1996, 15, 4.5),
                    (1, 'Syro', 2014, 12, 4.2),
                    (2, 'Music Has the Right to Children', 1998, 17, 4.9),
                    (2, 'Geogaddi', 2002, 23, 4.6),
                    (2, 'Tomorrow''s Harvest', 2013, 17, 4.3),
                    (3, 'Dive', 2011, 10, 4.4),
                    (3, 'Awake', 2014, 8, 4.3),
                    (3, 'Epoch', 2016, 11, 4.1),
                    (4, 'The Earth Is Not a Cold Dead Place', 2003, 5, 4.7),
                    (4, 'All of a Sudden I Miss Everyone', 2007, 6, 4.2),
                    (5, 'F# A# Infinity', 1997, 3, 4.6),
                    (5, 'Lift Your Skinny Fists Like Antennas to Heaven', 2000, 4, 4.9);
                """;

            command.ExecuteNonQuery();
        }
    }
}
