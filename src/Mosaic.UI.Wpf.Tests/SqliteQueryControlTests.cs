/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.Data.Sqlite;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class SqliteQueryControlTests
    {
        /// <summary>
        /// Runs the test body on an STA thread, which WPF controls require.
        /// </summary>
        private static void RunSta(Action action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// Applies the control's shipped style and realizes its template. Doing so also proves the
        /// XAML parses and that every resource it references resolves.
        /// </summary>
        private static SqliteQueryControl Realize(SqliteQueryControl control)
        {
            EnsureThemeManagerRegistered();

            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/SqliteQueryControl/SqliteQueryControl.xaml")
            };

            control.Style = (Style)dictionary[typeof(SqliteQueryControl)];
            control.Measure(new Size(900, 600));
            control.Arrange(new Rect(0, 0, 900, 600));
            control.ApplyTemplate();
            control.UpdateLayout();

            return control;
        }

        private static readonly Lock ThemeManagerGate = new();

        /// <summary>
        /// AdaptiveImage resolves the current theme through AppServices, which only a hosting
        /// MosaicApp populates. Register one so the template's node icons can be constructed.
        /// AppServices is process wide, so the check and the registration have to be atomic across
        /// the parallel test threads.
        /// </summary>
        private static void EnsureThemeManagerRegistered()
        {
            lock (ThemeManagerGate)
            {
                try
                {
                    AppServices.AddSingleton(new ThemeManager());
                }
                catch (InvalidOperationException)
                {
                    // Another test in this process registered it first, which is all we needed.
                }
            }
        }

        /// <summary>
        /// Runs an asynchronous test body on an STA thread with a real dispatcher pumping, so the
        /// control's continuations marshal back the same way they do inside a hosted application.
        /// </summary>
        private static void RunStaAsync(Func<Task> action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

                dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await action();
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                    finally
                    {
                        dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                    }
                });

                Dispatcher.Run();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// Creates a throwaway database file seeded with two related tables and a view.
        /// </summary>
        private static string CreateDatabase()
        {
            string path = Path.Combine(Path.GetTempPath(), $"mosaic-sqlite-tests-{Guid.NewGuid():N}.db");

            using var connection = new SqliteConnection($"Data Source={path}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE Artist (
                    ArtistId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    Name     TEXT    NOT NULL,
                    Country  TEXT    DEFAULT 'US'
                );

                CREATE TABLE Album (
                    AlbumId  INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    ArtistId INTEGER NOT NULL REFERENCES Artist (ArtistId),
                    Title    TEXT    NOT NULL
                );

                CREATE VIEW AlbumsByArtist AS
                SELECT a.Name, al.Title FROM Album al INNER JOIN Artist a ON a.ArtistId = al.ArtistId;

                INSERT INTO Artist (Name) VALUES ('Aphex Twin'), ('Tycho');
                INSERT INTO Album (ArtistId, Title) VALUES (1, 'Syro'), (1, 'Drukqs'), (2, 'Dive');
                """;

            command.ExecuteNonQuery();

            return path;
        }

        private static void DeleteDatabase(string path)
        {
            SqliteConnection.ClearAllPools();

            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // A leaked handle is not what this suite is testing.
            }
        }

        [Fact]
        public void ShippedTemplate_RealizesEveryPart()
        {
            RunSta(() =>
            {
                var control = Realize(new SqliteQueryControl());

                Assert.NotNull(control.Template.FindName("PART_SchemaTree", control) as TreeView);
                Assert.NotNull(control.Template.FindName("PART_SqlEditor", control) as SyntaxEditor);
                Assert.NotNull(control.Template.FindName("PART_Results", control) as DataGrid);
                Assert.NotNull(control.Template.FindName("PART_ExecutionControl", control) as ExecutionControl);
                Assert.NotNull(control.Template.FindName("PART_RefreshButton", control) as Button);
            });
        }

        [Fact]
        public void ExecuteCommand_IsDisabledUntilADatabaseIsOpen()
        {
            RunSta(() =>
            {
                var control = Realize(new SqliteQueryControl());

                Assert.False(control.ExecuteQueryCommand.CanExecute(null));
                Assert.False(control.CancelQueryCommand.CanExecute(null));

                control.ConnectionString = "Data Source=:memory:";

                Assert.True(control.ExecuteQueryCommand.CanExecute(null));
            });
        }

        [Fact]
        public async Task LoadAsync_ReadsTablesViewsAndColumns()
        {
            string path = CreateDatabase();

            try
            {
                var schema = await SqliteSchemaLoaderProbe.LoadAsync($"Data Source={path}");

                Assert.Equal(new[] { "Album", "Artist" }, schema.Tables.Select(x => x.Name));
                Assert.Equal(new[] { "AlbumsByArtist" }, schema.Views.Select(x => x.Name));

                var artist = schema.Tables.Single(x => x.Name == "Artist");
                Assert.Equal(new[] { "ArtistId", "Name", "Country" }, artist.Fields.Select(x => x.Name));

                var artistId = artist.Fields.Single(x => x.Name == "ArtistId");
                Assert.True(artistId.PrimaryKey);
                Assert.True(artistId.NotNull);
                Assert.Equal("INTEGER", artistId.Type);

                var country = artist.Fields.Single(x => x.Name == "Country");
                Assert.False(country.PrimaryKey);
                Assert.Equal("'US'", country.DefaultValue);

                // A view's columns come back through the same pragma path as a table's.
                Assert.Equal(2, schema.Views.Single().Fields.Count);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        [Fact]
        public async Task LoadAsync_SkipsInternalSqliteTables()
        {
            string path = CreateDatabase();

            try
            {
                var schema = await SqliteSchemaLoaderProbe.LoadAsync($"Data Source={path}");

                // AUTOINCREMENT creates sqlite_sequence, which is noise in an explorer.
                Assert.DoesNotContain(schema.Tables, x => x.Name!.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        [Fact]
        public void ExecuteQueryAsync_PopulatesTheGrid()
        {
            string path = CreateDatabase();

            try
            {
                RunStaAsync(async () =>
                {
                    var control = Realize(new SqliteQueryControl { RefreshSchemaAfterQuery = false });
                    control.ConnectionString = $"Data Source={path}";

                    await control.ExecuteQueryAsync("SELECT * FROM Album ORDER BY AlbumId;");

                    Assert.Equal("3 records returned.", control.StatusText);
                    Assert.False(control.IsQueryExecuting);

                    var grid = (DataGrid)control.Template.FindName("PART_Results", control)!;
                    Assert.NotNull(grid.ItemsSource);
                    Assert.Equal(3, grid.ItemsSource.Cast<object>().Count());
                });
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        [Fact]
        public void Results_SurviveTheTemplateBeingReapplied()
        {
            string path = CreateDatabase();

            try
            {
                RunStaAsync(async () =>
                {
                    var control = Realize(new SqliteQueryControl { RefreshSchemaAfterQuery = false });
                    control.ConnectionString = $"Data Source={path}";

                    await control.ExecuteQueryAsync("SELECT * FROM Album ORDER BY AlbumId;");

                    var original = (DataGrid)control.Template.FindName("PART_Results", control)!;

                    // Switching themes re-applies the template and hands the control a new grid.
                    var template = control.Template;
                    control.Template = null;
                    control.Template = template;
                    control.Measure(new Size(900, 600));
                    control.Arrange(new Rect(0, 0, 900, 600));
                    control.ApplyTemplate();
                    control.UpdateLayout();

                    var grid = (DataGrid)control.Template.FindName("PART_Results", control)!;

                    Assert.NotSame(original, grid);

                    Assert.NotNull(grid.ItemsSource);
                    Assert.Equal(3, grid.ItemsSource.Cast<object>().Count());
                });
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        [Fact]
        public void ExecuteQueryAsync_ReportsSyntaxErrorsWithoutThrowing()
        {
            string path = CreateDatabase();

            try
            {
                RunStaAsync(async () =>
                {
                    var control = Realize(new SqliteQueryControl { RefreshSchemaAfterQuery = false });
                    control.ConnectionString = $"Data Source={path}";

                    await control.ExecuteQueryAsync("SELECT * FROM NoSuchTable;");

                    Assert.Contains("no such table", control.StatusText, StringComparison.OrdinalIgnoreCase);
                    Assert.False(control.IsQueryExecuting);

                    // The transport buttons have to recover after a failure.
                    Assert.True(control.ExecuteQueryCommand.CanExecute(null));
                    Assert.False(control.CancelQueryCommand.CanExecute(null));
                });
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        #region Completion word scanning

        // These cover the offsets that made the original implementation throw: it computed
        // "CaretOffset - 2" and indexed the document with it unguarded, so a document one character
        // long produced a negative offset and an ArgumentOutOfRangeException.

        [Theory]
        [InlineData("", 0, "")]
        [InlineData("a", 0, "")]
        [InlineData("a", 1, "a")]
        [InlineData("a ", 2, "a")]
        [InlineData("a  ", 3, "a")]
        [InlineData(" ", 1, "")]
        [InlineData("select * from ", 14, "from")]
        [InlineData("SELECT * FROM\t", 14, "FROM")]
        [InlineData("update\r\n", 8, "update")]
        [InlineData("a", 99, "a")]
        [InlineData("a", -5, "")]
        public void GetWordBefore_HandlesBoundaryOffsets(string text, int offset, string expected)
        {
            var document = new TextDocument(text);

            Assert.Equal(expected, SyntaxCompletionController.GetWordBefore(document, offset));
        }

        [Theory]
        [InlineData("", 0, "")]
        [InlineData(".", 0, "")]
        [InlineData("a.", 1, "a")]
        [InlineData("Album.", 5, "Album")]
        [InlineData("select al.", 9, "al")]
        [InlineData("select a from x.", 15, "x")]
        [InlineData("  .", 2, "")]
        [InlineData("abc", 99, "abc")]
        [InlineData("abc", -3, "")]
        public void GetIdentifierBefore_HandlesBoundaryOffsets(string text, int offset, string expected)
        {
            var document = new TextDocument(text);

            Assert.Equal(expected, SyntaxCompletionController.GetIdentifierBefore(document, offset));
        }

        [Fact]
        public void CompletionController_DoesNotOpenAWindowWhenTheProviderDeclines()
        {
            RunSta(() =>
            {
                var editor = new SyntaxEditor { Language = SyntaxLanguage.Sqlite };
                using var controller = new SyntaxCompletionController(editor)
                {
                    ProvideCompletions = _ => Array.Empty<ICSharpCode.AvalonEdit.CodeCompletion.ICompletionData>()
                };

                controller.RequestCompletion();

                // An open-but-empty list is what made committing an entry throw.
                Assert.False(controller.IsOpen);
            });
        }

        [Fact]
        public void CompletionController_SurvivesAProviderThatThrows()
        {
            RunSta(() =>
            {
                var editor = new SyntaxEditor { Language = SyntaxLanguage.Sqlite };
                using var controller = new SyntaxCompletionController(editor)
                {
                    ProvideCompletions = _ => throw new InvalidOperationException("boom")
                };

                controller.RequestCompletion();

                Assert.False(controller.IsOpen);
            });
        }

        [Fact]
        public void AutoComplete_DoesNotThrowBeforeASchemaIsLoaded()
        {
            RunSta(() =>
            {
                var control = Realize(new SqliteQueryControl());
                var editor = (SyntaxEditor)control.Template.FindName("PART_SqlEditor", control)!;

                Assert.Null(control.Schema);

                // Reproduces the two reported crashes: a lone period, and a single character
                // followed by a space, both against a control with no database open.
                editor.Text = string.Empty;
                editor.CaretOffset = 0;
                editor.TextArea.PerformTextInput(".");

                editor.Text = string.Empty;
                editor.CaretOffset = 0;
                editor.TextArea.PerformTextInput("a");
                editor.TextArea.PerformTextInput(" ");

                Assert.Equal("a ", editor.Text);
            });
        }

        [Fact]
        public void AutoComplete_OffersTablesAfterFromAndColumnsAfterADot()
        {
            string path = CreateDatabase();

            try
            {
                RunStaAsync(async () =>
                {
                    var control = Realize(new SqliteQueryControl());
                    control.ConnectionString = $"Data Source={path}";

                    // The schema loads on a background continuation; wait for it deterministically.
                    await control.RefreshSchemaAsync();

                    Assert.NotNull(control.Schema);
                    Assert.Equal(2, control.Schema!.Tables.Count);

                    var editor = (SyntaxEditor)control.Template.FindName("PART_SqlEditor", control)!;

                    editor.Text = "select * from";
                    editor.CaretOffset = editor.Text.Length;
                    editor.TextArea.PerformTextInput(" ");

                    Assert.Equal("select * from ", editor.Text);

                    // A dot after a known table must not throw either.
                    editor.Text = "select Artist";
                    editor.CaretOffset = editor.Text.Length;
                    editor.TextArea.PerformTextInput(".");

                    Assert.Equal("select Artist.", editor.Text);
                });
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        #endregion

        [Fact]
        public void SqliteLanguage_ResolvesItsHighlightingDefinition()
        {
            RunSta(() =>
            {
                var editor = new SyntaxEditor { Language = SyntaxLanguage.Sqlite };

                Assert.NotNull(editor.SyntaxHighlighting);
                Assert.Equal("SQLite", editor.SyntaxHighlighting.Name);

                // The Xml light definition sat in the wrong folder and silently resolved to null.
                var xml = new SyntaxEditor { Theme = MosaicThemeMode.Light, Language = SyntaxLanguage.Xml };
                Assert.NotNull(xml.SyntaxHighlighting);
            });
        }
    }

    /// <summary>
    /// Reaches the internal schema loader from the test assembly without widening its visibility.
    /// </summary>
    internal static class SqliteSchemaLoaderProbe
    {
        internal static Task<SqliteSchema> LoadAsync(string connectionString)
        {
            var method = typeof(SqliteQueryControl).Assembly
                .GetType("Mosaic.UI.Wpf.Controls.SqliteSchemaLoader")!
                .GetMethod("LoadAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            return (Task<SqliteSchema>)method.Invoke(null, [connectionString, CancellationToken.None])!;
        }
    }
}
