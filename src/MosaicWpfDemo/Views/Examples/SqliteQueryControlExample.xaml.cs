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
                    ('The Beatles', 'UK', 1960),
                    ('Margot & the Nuclear So and So''s', 'US', 2004),
                    ('Jesse Welles', 'US', 2012),
                    ('Phantom Planet', 'US', 1994),
                    ('Rogue Wave', 'US', 2002);
                
                INSERT INTO Album (ArtistId, Title, ReleaseYear, TrackCount, Rating) VALUES
                    -- The Beatles
                    (1, 'Please Please Me', 1963, 14, 4.5),
                    (1, 'Rubber Soul', 1965, 14, 4.8),
                    (1, 'Revolver', 1966, 14, 4.9),
                    (1, 'Sgt. Pepper''s Lonely Hearts Club Band', 1967, 13, 4.9),
                    (1, 'Abbey Road', 1969, 17, 5.0),
                
                    -- Margot & the Nuclear So and So's
                    (2, 'The Dust of Retreat', 2005, 12, 4.7),
                    (2, 'Animal!', 2008, 12, 4.4),
                    (2, 'Not Animal', 2008, 12, 4.6),
                
                    -- Jesse Welles
                    (3, 'Hells Welles', 2024, 21, 4.7),
                    (3, 'Patchwork', 2024, 12, 4.6),
                    (3, 'Middle', 2025, 12, 4.5),
                    (3, 'Masks Off', 2026, 12, 5.0),
                
                    -- Phantom Planet
                    (4, 'The Guest', 2002, 12, 4.6),
                    (4, 'Phantom Planet', 2004, 11, 4.4),
                    (4, 'Raise the Dead', 2008, 12, 4.2),
                
                    -- Rogue Wave
                    (5, 'Out of the Shadow', 2003, 12, 4.5),
                    (5, 'Descended Like Vultures', 2005, 11, 4.7),
                    (5, 'Asleep at Heaven''s Gate', 2007, 12, 4.4);
                """;

            command.ExecuteNonQuery();
        }
    }
}
