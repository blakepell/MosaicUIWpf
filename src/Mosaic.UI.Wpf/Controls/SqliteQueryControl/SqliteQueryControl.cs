/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using Mosaic.UI.Wpf.Data.Excel;
using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A SQLite query workbench: a database explorer tree, a SQL editor with schema aware
    /// auto completion, and a results grid, wired to a transport style run/cancel tool bar.
    /// </summary>
    /// <remarks>
    /// Point the control at a database with <see cref="DatabaseFilePath"/> or
    /// <see cref="ConnectionString"/> and the schema loads automatically. Queries run on a background
    /// thread and are cancellable; because Microsoft.Data.Sqlite is synchronous underneath,
    /// cancellation is cooperative and a single long running statement may not stop instantly.
    ///
    /// The explorer's context menu is rebuilt on every open and raised as
    /// <see cref="SchemaContextMenuRequested"/> before it is shown, so consumers can add their own
    /// items per node without subclassing.
    ///
    /// Keyboard: F5 executes (the selection when there is one, otherwise the whole document),
    /// Ctrl+Space requests completion.
    /// </remarks>
    [TemplatePart(Name = PartSchemaTree, Type = typeof(TreeView))]
    [TemplatePart(Name = PartSqlEditor, Type = typeof(SyntaxEditor))]
    [TemplatePart(Name = PartResults, Type = typeof(DataGrid))]
    [TemplatePart(Name = PartExecutionControl, Type = typeof(ExecutionControl))]
    [TemplatePart(Name = PartRefreshButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartSaveButton, Type = typeof(ButtonBase))]
    [DefaultEvent(nameof(SchemaContextMenuRequested))]
    [DefaultProperty(nameof(DatabaseFilePath))]
    public class SqliteQueryControl : Control, IDisposable
    {
        private const string PartSchemaTree = "PART_SchemaTree";
        private const string PartSqlEditor = "PART_SqlEditor";
        private const string PartResults = "PART_Results";
        private const string PartExecutionControl = "PART_ExecutionControl";
        private const string PartRefreshButton = "PART_RefreshButton";
        private const string PartSaveButton = "PART_SaveButton";

        /// <summary>
        /// Words that, when followed by white space, indicate the user is about to name a table or view.
        /// </summary>
        private static readonly HashSet<string> ObjectContextKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "FROM", "JOIN", "INTO", "UPDATE", "TABLE"
        };

        /// <summary>
        /// Words that can never be a table alias, used to stop alias resolution running away.
        /// </summary>
        private static readonly HashSet<string> AliasStopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "AS", "ON", "WHERE", "SET", "VALUES", "GROUP", "ORDER", "JOIN", "INNER", "LEFT", "RIGHT",
            "FULL", "OUTER", "CROSS", "NATURAL", "LIMIT", "HAVING", "USING", "AND", "OR", "WHEN", "UNION"
        };

        /// <summary>
        /// Resolves <c>FROM|JOIN|UPDATE|INTO &lt;object&gt; [AS] &lt;alias&gt;</c> so a dotted qualifier
        /// that is an alias rather than a table name can still complete to the right columns.
        /// </summary>
        private static readonly Regex AliasPattern = new(
            """\b(?:from|join|update|into)\s+(?:\[(?<name>[^\]]+)]|"(?<name>[^"]+)"|`(?<name>[^`]+)`|(?<name>\w+))\s+(?:as\s+)?(?<alias>\w+)\b""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private TreeView? _schemaTree;
        private SyntaxEditor? _sqlEditor;
        private DataGrid? _results;
        private ButtonBase? _refreshButton;
        private ButtonBase? _saveButton;
        private ContextMenu? _schemaContextMenu;
        private SyntaxCompletionController? _completion;
        private CancellationTokenSource? _queryCancellation;
        private DataTable? _dataTable;
        private bool _synchronizingQueryText;
        private bool _disposed;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="ConnectionString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ConnectionStringProperty = DependencyProperty.Register(
            nameof(ConnectionString), typeof(string), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(null, OnConnectionStringChanged));

        /// <summary>
        /// Gets or sets the connection string used to reach the database. Setting it reloads the schema.
        /// </summary>
        [Category("Data")]
        [Description("The connection string used to reach the SQLite database.")]
        public string? ConnectionString
        {
            get => (string?)this.GetValue(ConnectionStringProperty);
            set => this.SetValue(ConnectionStringProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DatabaseFilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DatabaseFilePathProperty = DependencyProperty.Register(
            nameof(DatabaseFilePath), typeof(string), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(null, OnDatabaseFilePathChanged));

        /// <summary>
        /// Gets or sets the path of the database file. This is a convenience over
        /// <see cref="ConnectionString"/>; setting it builds a <c>Data Source=</c> connection string.
        /// </summary>
        [Category("Data")]
        [Description("Path of the SQLite database file to open.")]
        public string? DatabaseFilePath
        {
            get => (string?)this.GetValue(DatabaseFilePathProperty);
            set => this.SetValue(DatabaseFilePathProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Schema"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SchemaProperty = DependencyProperty.Register(
            nameof(Schema), typeof(SqliteSchema), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets the database schema as of the last refresh. This drives the explorer tree and the
        /// auto completion candidates.
        /// </summary>
        [Category("Data")]
        [Description("The database schema as of the last refresh.")]
        public SqliteSchema? Schema
        {
            get => (SqliteSchema?)this.GetValue(SchemaProperty);
            private set => this.SetValue(SchemaProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="QueryText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty QueryTextProperty = DependencyProperty.Register(
            nameof(QueryText), typeof(string), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnQueryTextChanged));

        /// <summary>
        /// Gets or sets the text currently in the SQL editor.
        /// </summary>
        [Category("Data")]
        [Description("The text currently in the SQL editor.")]
        public string QueryText
        {
            get => (string)this.GetValue(QueryTextProperty);
            set => this.SetValue(QueryTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StatusText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
            nameof(StatusText), typeof(string), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata("Idle"));

        /// <summary>
        /// Gets or sets the status message shown beside the run tool bar.
        /// </summary>
        [Category("Common")]
        [Description("The status message shown beside the run tool bar.")]
        public string StatusText
        {
            get => (string)this.GetValue(StatusTextProperty);
            set => this.SetValue(StatusTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsQueryExecuting"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsQueryExecutingProperty = DependencyProperty.Register(
            nameof(IsQueryExecuting), typeof(bool), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets a value indicating whether a query is currently running.
        /// </summary>
        [Category("Common")]
        [Description("Whether a query is currently running.")]
        public bool IsQueryExecuting
        {
            get => (bool)this.GetValue(IsQueryExecutingProperty);
            private set => this.SetValue(IsQueryExecutingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RefreshSchemaAfterQuery"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RefreshSchemaAfterQueryProperty = DependencyProperty.Register(
            nameof(RefreshSchemaAfterQuery), typeof(bool), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the schema is reloaded after every query, so DDL
        /// executed in the editor shows up in the explorer. Defaults to <see langword="true"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the schema is reloaded after every query.")]
        public bool RefreshSchemaAfterQuery
        {
            get => (bool)this.GetValue(RefreshSchemaAfterQueryProperty);
            set => this.SetValue(RefreshSchemaAfterQueryProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoCompleteEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoCompleteEnabledProperty = DependencyProperty.Register(
            nameof(AutoCompleteEnabled), typeof(bool), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(true, OnAutoCompleteEnabledChanged));

        /// <summary>
        /// Gets or sets a value indicating whether schema aware auto completion is offered in the
        /// editor. Defaults to <see langword="true"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether schema aware auto completion is offered in the editor.")]
        public bool AutoCompleteEnabled
        {
            get => (bool)this.GetValue(AutoCompleteEnabledProperty);
            set => this.SetValue(AutoCompleteEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RowLimit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowLimitProperty = DependencyProperty.Register(
            nameof(RowLimit), typeof(int), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(1000));

        /// <summary>
        /// Gets or sets the row count used by the explorer's "Select Top N Rows" command.
        /// Defaults to 1,000.
        /// </summary>
        [Category("Behavior")]
        [Description("The row count used by the explorer's Select Top N Rows command.")]
        public int RowLimit
        {
            get => (int)this.GetValue(RowLimitProperty);
            set => this.SetValue(RowLimitProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowExportOptions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowExportOptionsProperty = DependencyProperty.Register(
            nameof(ShowExportOptions), typeof(bool), typeof(SqliteQueryControl),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the save and export menu is visible.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the save and export menu is visible.")]
        public bool ShowExportOptions
        {
            get => (bool)this.GetValue(ShowExportOptionsProperty);
            set => this.SetValue(ShowExportOptionsProperty, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="SchemaContextMenuRequested"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SchemaContextMenuRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(SchemaContextMenuRequested), RoutingStrategy.Bubble,
            typeof(EventHandler<SqliteSchemaContextMenuEventArgs>), typeof(SqliteQueryControl));

        /// <summary>
        /// Raised after the control has populated the standard explorer context menu items for the
        /// clicked node but before the menu is shown, allowing consumers to customize it.
        /// </summary>
        [Category("Action")]
        [Description("Raised before the database explorer context menu is shown.")]
        public event EventHandler<SqliteSchemaContextMenuEventArgs> SchemaContextMenuRequested
        {
            add => this.AddHandler(SchemaContextMenuRequestedEvent, value);
            remove => this.RemoveHandler(SchemaContextMenuRequestedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="QueryCompleted"/> routed event.
        /// </summary>
        public static readonly RoutedEvent QueryCompletedEvent = EventManager.RegisterRoutedEvent(
            nameof(QueryCompleted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SqliteQueryControl));

        /// <summary>
        /// Raised after a query finishes, whether it succeeded, failed, or was cancelled. Inspect
        /// <see cref="StatusText"/> for the outcome.
        /// </summary>
        [Category("Action")]
        [Description("Raised after a query finishes.")]
        public event RoutedEventHandler QueryCompleted
        {
            add => this.AddHandler(QueryCompletedEvent, value);
            remove => this.RemoveHandler(QueryCompletedEvent, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Runs the current selection, or the whole document when nothing is selected. Reports that it
        /// cannot execute while a query is already running or no database is open.
        /// </summary>
        public IAsyncRelayCommand ExecuteQueryCommand { get; }

        /// <summary>
        /// Cancels the running query. Reports that it cannot execute when nothing is running.
        /// </summary>
        public IRelayCommand CancelQueryCommand { get; }

        /// <summary>
        /// Reloads the database schema into the explorer.
        /// </summary>
        public IAsyncRelayCommand RefreshSchemaCommand { get; }

        /// <summary>
        /// Exports the data set to Excel.
        /// </summary>
        public IAsyncRelayCommand ExportToExcelCommand { get; }

        /// <summary>
        /// Saves the data set as JSON.
        /// </summary>
        public IAsyncRelayCommand SaveAsJsonCommand { get; }

        /// <summary>
        /// Saves the data set as a Markdown table.
        /// </summary>
        public IAsyncRelayCommand SaveAsMarkdownCommand { get; }

        /// <summary>
        /// Saves the data set as comma-separated values.
        /// </summary>
        public IAsyncRelayCommand SaveAsCsvCommand { get; }

        #endregion

        static SqliteQueryControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SqliteQueryControl), new FrameworkPropertyMetadata(typeof(SqliteQueryControl)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteQueryControl"/> class.
        /// </summary>
        public SqliteQueryControl()
        {
            this.ExecuteQueryCommand = new AsyncRelayCommand(this.ExecuteEditorQueryAsync, () => !this.IsQueryExecuting && !string.IsNullOrWhiteSpace(this.ConnectionString));
            this.CancelQueryCommand = new RelayCommand(this.CancelQuery, () => this.IsQueryExecuting);
            this.RefreshSchemaCommand = new AsyncRelayCommand(this.RefreshSchemaAsync, () => !string.IsNullOrWhiteSpace(this.ConnectionString));
            this.ExportToExcelCommand = new AsyncRelayCommand(this.ExportToExcelAsync, () => !this.IsQueryExecuting && !string.IsNullOrWhiteSpace(this.ConnectionString));
            this.SaveAsJsonCommand = new AsyncRelayCommand(this.SaveAsJsonAsync, () => !this.IsQueryExecuting && !string.IsNullOrWhiteSpace(this.ConnectionString));
            this.SaveAsMarkdownCommand = new AsyncRelayCommand(this.SaveAsMarkdownAsync, () => !this.IsQueryExecuting && !string.IsNullOrWhiteSpace(this.ConnectionString));
            this.SaveAsCsvCommand = new AsyncRelayCommand(this.SaveAsCsvAsync, () => !this.IsQueryExecuting && !string.IsNullOrWhiteSpace(this.ConnectionString));
            this.Unloaded += this.OnUnloaded;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SqliteQueryControlAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.DetachTemplateParts();

            _schemaTree = this.GetTemplateChild(PartSchemaTree) as TreeView;
            _sqlEditor = this.GetTemplateChild(PartSqlEditor) as SyntaxEditor;
            _results = this.GetTemplateChild(PartResults) as DataGrid;
            _refreshButton = this.GetTemplateChild(PartRefreshButton) as ButtonBase;
            _saveButton = this.GetTemplateChild(PartSaveButton) as ButtonBase;

            if (_schemaTree != null)
            {
                // One menu instance whose items are rebuilt per open. Rebuilding the items rather than
                // swapping in a fresh ContextMenu keeps the handler wiring in one place.
                _schemaContextMenu = new ContextMenu();
                _schemaTree.ContextMenu = _schemaContextMenu;
                _schemaTree.ContextMenuOpening += this.OnSchemaContextMenuOpening;
            }

            if (_results != null)
            {
                _results.AutoGeneratingColumn += OnResultsAutoGeneratingColumn;

                // A theme switch re-applies the template, which hands us a brand new grid. Without
                // this the last result set would silently disappear every time the theme changed.
                if (_dataTable != null)
                {
                    _results.ItemsSource = _dataTable.DefaultView;
                }
            }

            if (_sqlEditor != null)
            {
                _sqlEditor.TextChanged += this.OnEditorTextChanged;
                _sqlEditor.PreviewKeyDown += this.OnEditorPreviewKeyDown;

                if (!string.IsNullOrEmpty(this.QueryText) && _sqlEditor.Text != this.QueryText)
                {
                    _sqlEditor.Text = this.QueryText;
                }

                _completion = new SyntaxCompletionController(_sqlEditor)
                {
                    IsEnabled = this.AutoCompleteEnabled,
                    ProvideCompletions = this.ProvideCompletions
                };
            }
        }

        /// <summary>
        /// Detaches every handler attached in <see cref="OnApplyTemplate"/> so re-templating and
        /// unloading do not leak subscriptions.
        /// </summary>
        private void DetachTemplateParts()
        {
            if (_schemaTree != null)
            {
                _schemaTree.ContextMenuOpening -= this.OnSchemaContextMenuOpening;
                _schemaTree = null;
            }

            if (_results != null)
            {
                _results.AutoGeneratingColumn -= OnResultsAutoGeneratingColumn;
                _results = null;
            }

            if (_sqlEditor != null)
            {
                _sqlEditor.TextChanged -= this.OnEditorTextChanged;
                _sqlEditor.PreviewKeyDown -= this.OnEditorPreviewKeyDown;
                _sqlEditor = null;
            }

            _completion?.Dispose();
            _completion = null;
            _schemaContextMenu = null;
            _refreshButton = null;
            _saveButton = null;
        }

        #region Property change handlers

        /// <summary>
        /// Reloads the schema when the connection string changes.
        /// </summary>
        /// <param name="d">The control whose property changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnConnectionStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SqliteQueryControl control)
            {
                return;
            }

            control.ExecuteQueryCommand.NotifyCanExecuteChanged();
            control.RefreshSchemaCommand.NotifyCanExecuteChanged();
            control.NotifyExportCommandsCanExecuteChanged();

            if (!string.IsNullOrWhiteSpace(e.NewValue as string))
            {
                _ = control.RefreshSchemaAsync();
            }
            else
            {
                control.Schema = null;
            }
        }

        /// <summary>
        /// Projects a file path onto <see cref="ConnectionString"/>.
        /// </summary>
        /// <param name="d">The control whose property changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnDatabaseFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SqliteQueryControl control)
            {
                return;
            }

            control.ConnectionString = e.NewValue is string path && !string.IsNullOrWhiteSpace(path)
                ? $"Data Source={path}"
                : null;
        }

        /// <summary>
        /// Pushes the bound query text into the editor.
        /// </summary>
        /// <param name="d">The control whose property changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnQueryTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SqliteQueryControl { _synchronizingQueryText: false, _sqlEditor: not null } control)
            {
                return;
            }

            string text = e.NewValue as string ?? string.Empty;

            if (control._sqlEditor.Text != text)
            {
                control._synchronizingQueryText = true;

                try
                {
                    control._sqlEditor.Text = text;
                }
                finally
                {
                    control._synchronizingQueryText = false;
                }
            }
        }

        /// <summary>
        /// Enables or disables the completion controller.
        /// </summary>
        /// <param name="d">The control whose property changed.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnAutoCompleteEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SqliteQueryControl { _completion: not null } control)
            {
                control._completion.IsEnabled = (bool)e.NewValue;
            }
        }

        /// <summary>
        /// Mirrors editor edits back onto <see cref="QueryText"/>.
        /// </summary>
        /// <param name="sender">The editor.</param>
        /// <param name="e">The event data.</param>
        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (_synchronizingQueryText || _sqlEditor == null)
            {
                return;
            }

            _synchronizingQueryText = true;

            try
            {
                this.QueryText = _sqlEditor.Text;
            }
            finally
            {
                _synchronizingQueryText = false;
            }
        }

        /// <summary>
        /// Handles the editor's execute and completion gestures.
        /// </summary>
        /// <param name="sender">The editor.</param>
        /// <param name="e">The event data for the key press.</param>
        private void OnEditorPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                if (this.ExecuteQueryCommand.CanExecute(null))
                {
                    this.ExecuteQueryCommand.Execute(null);
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                _completion?.RequestCompletion();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Doubles underscores in generated column headers. WPF treats a single underscore in header
        /// content as an access key marker and swallows it.
        /// </summary>
        /// <param name="sender">The results grid.</param>
        /// <param name="e">The event data for the generated column.</param>
        private static void OnResultsAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header is string header && header.Contains('_'))
            {
                e.Column.Header = header.Replace("_", "__");
            }
        }

        #endregion

        #region Query execution

        /// <summary>
        /// Opens a database file and loads its schema.
        /// </summary>
        /// <param name="filePath">The path of the database file. The directory must already exist.</param>
        /// <returns>A task that completes once the schema has loaded.</returns>
        public Task OpenDatabaseAsync(string filePath)
        {
            this.DatabaseFilePath = filePath;
            return this.RefreshSchemaAsync();
        }

        /// <summary>
        /// Prompts for a destination and creates an Excel workbook from the current query results.
        /// </summary>
        public async Task ExportToExcelAsync()
        {
            await this.SaveResultsAsync(
                "Export Results to Excel",
                "Excel Workbook (*.xlsx)|*.xlsx",
                async (dataTable, path) =>
                {
                    await using var excel = new ExcelWriter(path);
                    await excel.AddSheetAsync(dataTable, "Sheet1");
                });
        }

        /// <summary>
        /// Prompts for a destination and saves the current query results as JSON.
        /// </summary>
        public async Task SaveAsJsonAsync()
        {
            await this.SaveResultsAsync(
                "Save Results as JSON",
                "JSON Files (*.json)|*.json",
                SaveJsonAsync);
        }

        /// <summary>
        /// Prompts for a destination and saves the current query results as a Markdown table.
        /// </summary>
        public async Task SaveAsMarkdownAsync()
        {
            await this.SaveResultsAsync(
                "Save Results as Markdown",
                "Markdown Files (*.md)|*.md|Markdown Files (*.markdown)|*.markdown",
                SaveMarkdownAsync);
        }

        /// <summary>
        /// Prompts for a destination and saves the current query results as comma-separated values.
        /// </summary>
        public async Task SaveAsCsvAsync()
        {
            await this.SaveResultsAsync(
                "Save Results as CSV",
                "CSV Files (*.csv)|*.csv",
                SaveCsvAsync);
        }

        /// <summary>
        /// Prompts for a destination, refreshes the current result set, and writes it with the
        /// supplied exporter.
        /// </summary>
        /// <param name="title">The Save dialog title.</param>
        /// <param name="filter">The Save dialog file type filter.</param>
        /// <param name="saveAsync">The format-specific writer.</param>
        private async Task SaveResultsAsync(string title, string filter, Func<DataTable, string, Task> saveAsync)
        {
            string? path = WpfUtilities.SaveFileRequest(title, filter);

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            await this.ExecuteEditorQueryAsync();

            if (_dataTable == null || _dataTable.Columns.Count == 0)
            {
                this.StatusText = "No tabular results are available to save.";
                return;
            }

            this.SetExecuting(true);
            this.StatusText = "Saving results…";

            try
            {
                await saveAsync(_dataTable, path);
                this.StatusText = $"Results saved to {path}.";
            }
            catch (Exception ex)
            {
                this.StatusText = $"Save failed: {ex.Message}";
            }
            finally
            {
                this.SetExecuting(false);
            }
        }

        /// <summary>
        /// Writes a data table as an indented JSON array of row objects.
        /// </summary>
        /// <param name="dataTable">The result set.</param>
        /// <param name="path">The destination file.</param>
        private static async Task SaveJsonAsync(DataTable dataTable, string path)
        {
            var rows = dataTable.Rows.Cast<DataRow>()
                .Select(row => dataTable.Columns.Cast<DataColumn>().ToDictionary(
                    column => column.ColumnName,
                    column => row.IsNull(column) ? null : row[column]))
                .ToList();

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, rows, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Writes a data table as a GitHub-flavored Markdown table.
        /// </summary>
        /// <param name="dataTable">The result set.</param>
        /// <param name="path">The destination file.</param>
        private static async Task SaveMarkdownAsync(DataTable dataTable, string path)
        {
            await using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
            var columns = dataTable.Columns.Cast<DataColumn>().ToList();

            await writer.WriteLineAsync($"| {string.Join(" | ", columns.Select(column => EscapeMarkdownCell(column.ColumnName)))} |");
            await writer.WriteLineAsync($"| {string.Join(" | ", columns.Select(_ => "---"))} |");

            foreach (DataRow row in dataTable.Rows)
            {
                await writer.WriteLineAsync($"| {string.Join(" | ", columns.Select(column => EscapeMarkdownCell(FormatCell(row[column]))))} |");
            }
        }

        /// <summary>
        /// Writes a data table using RFC 4180-style CSV escaping.
        /// </summary>
        /// <param name="dataTable">The result set.</param>
        /// <param name="path">The destination file.</param>
        private static async Task SaveCsvAsync(DataTable dataTable, string path)
        {
            await using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
            var columns = dataTable.Columns.Cast<DataColumn>().ToList();

            await writer.WriteLineAsync(string.Join(",", columns.Select(column => EscapeCsvCell(column.ColumnName))));

            foreach (DataRow row in dataTable.Rows)
            {
                await writer.WriteLineAsync(string.Join(",", columns.Select(column => EscapeCsvCell(FormatCell(row[column])))));
            }
        }

        /// <summary>
        /// Formats a database value for text exports using invariant culture.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The formatted value, or an empty string for a database null.</returns>
        private static string FormatCell(object value)
        {
            return value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// Escapes a cell for comma-separated output.
        /// </summary>
        /// <param name="value">The unescaped cell.</param>
        /// <returns>The escaped cell.</returns>
        private static string EscapeCsvCell(string value)
        {
            if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Escapes table delimiters and line breaks in a Markdown cell.
        /// </summary>
        /// <param name="value">The unescaped cell.</param>
        /// <returns>The escaped cell.</returns>
        private static string EscapeMarkdownCell(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("|", "\\|")
                .Replace("\r\n", "<br>")
                .Replace("\r", "<br>")
                .Replace("\n", "<br>");
        }

        /// <summary>
        /// Reloads the database schema into the explorer.
        /// </summary>
        /// <returns>A task that completes once the schema has loaded.</returns>
        public async Task RefreshSchemaAsync()
        {
            string? connectionString = this.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                this.Schema = null;
                return;
            }

            try
            {
                this.Schema = await Task.Run(() => SqliteSchemaLoader.LoadAsync(connectionString, CancellationToken.None)).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                this.StatusText = $"Schema refresh failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Runs the editor's selection, or the whole document when there is no selection.
        /// </summary>
        /// <returns>A task that completes once the query has finished.</returns>
        private Task ExecuteEditorQueryAsync()
        {
            if (_sqlEditor == null)
            {
                return Task.CompletedTask;
            }

            // Reading the text on the UI thread first avoids touching the editor from the worker.
            string sql = _sqlEditor.SelectionLength > 0 ? _sqlEditor.SelectedText : _sqlEditor.Text;

            return this.ExecuteQueryAsync(sql);
        }

        /// <summary>
        /// Executes SQL against the open database and shows the first result set in the grid.
        /// </summary>
        /// <param name="sql">The SQL to run.</param>
        /// <returns>A task that completes once the query has finished.</returns>
        public async Task ExecuteQueryAsync(string sql)
        {
            _completion?.Close();

            if (this.IsQueryExecuting)
            {
                return;
            }

            string? connectionString = this.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                this.StatusText = "No database is open.";
                return;
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                this.StatusText = "Nothing to execute.";
                return;
            }

            this.SetExecuting(true);
            this.StatusText = "Executing…";
            this.ClearResults();

            var cancellation = new CancellationTokenSource();
            _queryCancellation = cancellation;

            try
            {
                var dataTable = await Task.Run(() => SqliteSchemaLoader.ExecuteDataTableAsync(connectionString, sql, cancellation.Token), cancellation.Token).ConfigureAwait(true);

                _dataTable = dataTable;

                if (_results != null)
                {
                    _results.ItemsSource = dataTable.DefaultView;
                }

                int rowCount = dataTable.Rows.Count;
                this.StatusText = $"{rowCount:N0} {(rowCount == 1 ? "record" : "records")} returned.";
            }
            catch (OperationCanceledException)
            {
                this.StatusText = "Query cancelled.";
            }
            catch (Exception ex)
            {
                this.StatusText = ex.Message;
            }
            finally
            {
                _queryCancellation = null;
                cancellation.Dispose();
                this.SetExecuting(false);
            }

            if (this.RefreshSchemaAfterQuery)
            {
                await this.RefreshSchemaAsync().ConfigureAwait(true);
            }

            this.RaiseEvent(new RoutedEventArgs(QueryCompletedEvent, this));
        }

        /// <summary>
        /// Requests cancellation of the running query.
        /// </summary>
        private void CancelQuery()
        {
            try
            {
                _queryCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The query finished between the button press and this call.
            }
        }

        /// <summary>
        /// Updates the executing flag and re-evaluates the transport commands.
        /// </summary>
        /// <param name="executing">Whether a query is running.</param>
        private void SetExecuting(bool executing)
        {
            this.IsQueryExecuting = executing;
            this.ExecuteQueryCommand.NotifyCanExecuteChanged();
            this.CancelQueryCommand.NotifyCanExecuteChanged();
            this.NotifyExportCommandsCanExecuteChanged();
        }

        /// <summary>
        /// Re-evaluates every save and export command.
        /// </summary>
        private void NotifyExportCommandsCanExecuteChanged()
        {
            this.ExportToExcelCommand.NotifyCanExecuteChanged();
            this.SaveAsJsonCommand.NotifyCanExecuteChanged();
            this.SaveAsMarkdownCommand.NotifyCanExecuteChanged();
            this.SaveAsCsvCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Detaches and disposes the previous result set.
        /// </summary>
        private void ClearResults()
        {
            if (_results != null)
            {
                _results.ItemsSource = null;
            }

            _dataTable?.Dispose();
            _dataTable = null;
        }

        #endregion

        #region Database explorer context menu

        /// <summary>
        /// Rebuilds the explorer context menu for the node under the cursor and gives consumers a
        /// chance to customize it before it is displayed.
        /// </summary>
        /// <param name="sender">The explorer tree.</param>
        /// <param name="e">The event data for the menu opening.</param>
        private void OnSchemaContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_schemaContextMenu == null)
            {
                return;
            }

            var item = FindTreeViewItem(e.OriginalSource as DependencyObject);
            var (nodeKind, node, objectName) = ClassifyNode(item);

            _schemaContextMenu.Items.Clear();

            switch (nodeKind)
            {
                case SqliteSchemaNodeKind.Table:
                case SqliteSchemaNodeKind.View:
                    this.BuildObjectMenu(_schemaContextMenu, nodeKind, node, objectName);
                    break;

                case SqliteSchemaNodeKind.Field when node is SqliteField field:
                    this.BuildFieldMenu(_schemaContextMenu, field, objectName);
                    break;
            }

            var args = new SqliteSchemaContextMenuEventArgs(SchemaContextMenuRequestedEvent, this, _schemaContextMenu, nodeKind, node, objectName);
            this.RaiseEvent(args);

            // Suppress an empty menu; a bare grey box on a folder node is worse than no menu at all.
            if (args.Cancel || _schemaContextMenu.Items.Count == 0)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Builds the standard menu for a table or view node.
        /// </summary>
        /// <param name="menu">The menu to populate.</param>
        /// <param name="nodeKind">Whether the node is a table or a view.</param>
        /// <param name="node">The bound table or view.</param>
        /// <param name="objectName">The object name.</param>
        private void BuildObjectMenu(ContextMenu menu, SqliteSchemaNodeKind nodeKind, object? node, string? objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return;
            }

            string quoted = QuoteIdentifier(objectName);

            menu.Items.Add(CreateItem("Select All Rows", (_, _) => _ = this.ExecuteQueryAsync($"SELECT * FROM {quoted};")));
            menu.Items.Add(CreateItem($"Select Top {this.RowLimit:N0} Rows", (_, _) => _ = this.ExecuteQueryAsync($"SELECT * FROM {quoted} LIMIT {this.RowLimit};")));
            menu.Items.Add(new Separator());

            var generate = new MenuItem { Header = "Generate SQL" };

            if (node is SqliteTable table)
            {
                generate.Items.Add(CreateItem("Select Statement", (_, _) => this.AppendToEditor(BuildSelectStatement(table))));
                generate.Items.Add(CreateItem("Insert Statement", (_, _) => this.AppendToEditor(BuildInsertStatement(table))));
                generate.Items.Add(CreateItem("Update Statement", (_, _) => this.AppendToEditor(BuildUpdateStatement(table))));
                generate.Items.Add(new Separator());
                generate.Items.Add(CreateItem("Create Table", (_, _) => this.AppendToEditor(table.Sql)));
            }
            else if (node is SqliteView view)
            {
                generate.Items.Add(CreateItem("Select Statement", (_, _) => this.AppendToEditor(BuildSelectStatement(view.Name, view.Fields))));
                generate.Items.Add(new Separator());
                generate.Items.Add(CreateItem("Create View", (_, _) => this.AppendToEditor(view.Sql)));
            }

            if (generate.Items.Count > 0)
            {
                menu.Items.Add(generate);
            }

            _ = nodeKind;
        }

        /// <summary>
        /// Builds the standard menu for a column node.
        /// </summary>
        /// <param name="menu">The menu to populate.</param>
        /// <param name="field">The bound column.</param>
        /// <param name="objectName">The owning table or view name.</param>
        private void BuildFieldMenu(ContextMenu menu, SqliteField field, string? objectName)
        {
            if (string.IsNullOrEmpty(field.Name))
            {
                return;
            }

            menu.Items.Add(CreateItem("Copy Column Name", (_, _) => SetClipboardText(field.Name)));

            if (!string.IsNullOrEmpty(objectName))
            {
                string quotedObject = QuoteIdentifier(objectName);
                string quotedField = QuoteIdentifier(field.Name);

                menu.Items.Add(CreateItem("Select Distinct Values", (_, _) =>
                    _ = this.ExecuteQueryAsync($"SELECT DISTINCT {quotedField} FROM {quotedObject} ORDER BY 1;")));
            }
        }

        /// <summary>
        /// Copies text to the clipboard, tolerating the transient failures the Win32 clipboard is
        /// prone to when another process holds it open.
        /// </summary>
        /// <param name="text">The text to copy.</param>
        private static void SetClipboardText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception)
            {
                // Nothing actionable; the clipboard is owned by another process.
            }
        }

        /// <summary>
        /// Creates a click-handled menu item.
        /// </summary>
        /// <param name="header">The item header.</param>
        /// <param name="onClick">The click handler.</param>
        /// <returns>The configured item.</returns>
        private static MenuItem CreateItem(string header, RoutedEventHandler onClick)
        {
            var item = new MenuItem { Header = header };
            item.Click += onClick;
            return item;
        }

        /// <summary>
        /// Walks up the visual tree to the nearest <see cref="TreeViewItem"/>.
        /// </summary>
        /// <param name="source">The element the click originated from.</param>
        /// <returns>The containing item, or <see langword="null"/> when the click missed every node.</returns>
        private static TreeViewItem? FindTreeViewItem(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is TreeViewItem item)
                {
                    return item;
                }

                if (source is TreeView)
                {
                    return null;
                }

                source = source is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            }

            return null;
        }

        /// <summary>
        /// Determines what kind of schema node an explorer item represents.
        /// </summary>
        /// <param name="item">The item that was clicked.</param>
        /// <returns>The node kind, its bound data item, and the table or view name it resolves to.</returns>
        private static (SqliteSchemaNodeKind Kind, object? Node, string? ObjectName) ClassifyNode(TreeViewItem? item)
        {
            if (item == null)
            {
                return (SqliteSchemaNodeKind.None, null, null);
            }

            switch (item.DataContext)
            {
                case SqliteTable table:
                    return (SqliteSchemaNodeKind.Table, table, table.Name);

                case SqliteView view:
                    return (SqliteSchemaNodeKind.View, view, view.Name);

                case SqliteField field:
                    return (SqliteSchemaNodeKind.Field, field, FindOwningObjectName(item));
            }

            // The database and folder nodes are declared in the template and identified by their Tag.
            return item.Tag as string switch
            {
                "Database" => (SqliteSchemaNodeKind.Database, null, null),
                "Tables" => (SqliteSchemaNodeKind.TablesFolder, null, null),
                "Views" => (SqliteSchemaNodeKind.ViewsFolder, null, null),
                _ => (SqliteSchemaNodeKind.None, null, null)
            };
        }

        /// <summary>
        /// Walks up from a column node to the table or view that contains it.
        /// </summary>
        /// <param name="item">The column's tree item.</param>
        /// <returns>The owning object's name, when one can be found.</returns>
        private static string? FindOwningObjectName(TreeViewItem item)
        {
            DependencyObject? current = VisualTreeHelper.GetParent(item);

            while (current != null)
            {
                if (current is TreeViewItem parent)
                {
                    switch (parent.DataContext)
                    {
                        case SqliteTable table:
                            return table.Name;

                        case SqliteView view:
                            return view.Name;
                    }
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        #endregion

        #region SQL generation

        /// <summary>
        /// Appends generated SQL to the editor, separated from anything already there.
        /// </summary>
        /// <param name="sql">The SQL to append.</param>
        private void AppendToEditor(string? sql)
        {
            if (_sqlEditor == null || string.IsNullOrWhiteSpace(sql))
            {
                return;
            }

            if (_sqlEditor.Document.TextLength > 0)
            {
                _sqlEditor.AppendText(Environment.NewLine + Environment.NewLine);
            }

            _sqlEditor.AppendText(sql);
            _sqlEditor.Focus();
        }

        /// <summary>
        /// Builds a fully enumerated <c>SELECT</c> for a table, annotating each column with its type.
        /// </summary>
        /// <param name="table">The table to describe.</param>
        /// <returns>The generated statement.</returns>
        private static string BuildSelectStatement(SqliteTable table)
        {
            return BuildSelectStatement(table.Name, table.Fields);
        }

        /// <summary>
        /// Builds a fully enumerated <c>SELECT</c> for a named object.
        /// </summary>
        /// <param name="objectName">The table or view name.</param>
        /// <param name="fields">The columns to project.</param>
        /// <returns>The generated statement.</returns>
        private static string BuildSelectStatement(string? objectName, IEnumerable<SqliteField> fields)
        {
            var builder = new StringBuilder("SELECT");
            int counter = 0;

            foreach (var field in fields)
            {
                builder.Append(counter++ == 0 ? "\r\n      " : "\r\n    , ");
                builder.Append(QuoteIdentifier(field.Name)).Append(DescribeField(field));
            }

            if (counter == 0)
            {
                builder.Append(" *");
            }

            builder.Append("\r\nFROM ").Append(QuoteIdentifier(objectName)).Append(';');

            return builder.ToString();
        }

        /// <summary>
        /// Builds a parameterized <c>INSERT</c> covering every column of a table.
        /// </summary>
        /// <param name="table">The table to insert into.</param>
        /// <returns>The generated statement.</returns>
        private static string BuildInsertStatement(SqliteTable table)
        {
            var builder = new StringBuilder();
            builder.Append("INSERT INTO ").Append(QuoteIdentifier(table.Name)).Append(" (");

            int counter = 0;

            foreach (var field in table.Fields)
            {
                builder.Append(counter++ == 0 ? "\r\n      " : "\r\n    , ");
                builder.Append(QuoteIdentifier(field.Name));
            }

            builder.Append("\r\n) VALUES (");
            counter = 0;

            foreach (var field in table.Fields)
            {
                builder.Append(counter++ == 0 ? "\r\n      " : "\r\n    , ");
                builder.Append('$').Append(field.Name).Append(DescribeField(field));
            }

            builder.Append("\r\n);");

            return builder.ToString();
        }

        /// <summary>
        /// Builds a parameterized <c>UPDATE</c> covering every non key column of a table, with a
        /// <c>WHERE</c> clause over the primary key when one is declared.
        /// </summary>
        /// <param name="table">The table to update.</param>
        /// <returns>The generated statement.</returns>
        private static string BuildUpdateStatement(SqliteTable table)
        {
            var keyFields = table.Fields.Where(x => x.PrimaryKey).ToList();
            var setFields = table.Fields.Where(x => !x.PrimaryKey).ToList();

            // A table with no declared key would otherwise generate an UPDATE with an empty SET list.
            if (setFields.Count == 0)
            {
                setFields = table.Fields.ToList();
            }

            var builder = new StringBuilder();
            builder.Append("UPDATE ").Append(QuoteIdentifier(table.Name)).Append("\r\nSET ");

            int counter = 0;

            foreach (var field in setFields)
            {
                builder.Append(counter++ == 0 ? "\r\n      " : "\r\n    , ");
                builder.Append(QuoteIdentifier(field.Name)).Append(" = $").Append(field.Name).Append(DescribeField(field));
            }

            if (keyFields.Count > 0)
            {
                builder.Append("\r\nWHERE ");
                counter = 0;

                foreach (var field in keyFields)
                {
                    builder.Append(counter++ == 0 ? string.Empty : "\r\n  AND ");
                    builder.Append(QuoteIdentifier(field.Name)).Append(" = $").Append(field.Name);
                }
            }
            else
            {
                builder.Append("\r\n-- No primary key is declared on this table; add a WHERE clause before running this.");
            }

            builder.Append(';');

            return builder.ToString();
        }

        /// <summary>
        /// Renders the trailing comment that annotates a generated column with its type and key role.
        /// </summary>
        /// <param name="field">The column to describe.</param>
        /// <returns>The comment, including its leading separator.</returns>
        private static string DescribeField(SqliteField field)
        {
            string type = string.IsNullOrWhiteSpace(field.Type) ? "unspecified" : field.Type;

            return field.PrimaryKey ? $" -- {type}, PK" : $" -- {type}";
        }

        /// <summary>
        /// Quotes an identifier for use in generated SQL.
        /// </summary>
        /// <param name="identifier">The identifier to quote.</param>
        /// <returns>The bracket quoted identifier.</returns>
        private static string QuoteIdentifier(string? identifier)
        {
            return $"[{identifier?.Replace("]", "]]")}]";
        }

        #endregion

        #region Auto completion

        /// <summary>
        /// Produces the completion candidates for a request against the loaded schema.
        /// </summary>
        /// <param name="request">The request to satisfy.</param>
        /// <returns>The candidates, or <see langword="null"/> when there is nothing to offer.</returns>
        private IReadOnlyList<ICompletionData>? ProvideCompletions(SyntaxCompletionRequest request)
        {
            // Until a database is opened there is no schema at all; every path below has to tolerate
            // that rather than assume the collections exist.
            var schema = this.Schema;

            if (schema == null)
            {
                return null;
            }

            return request.Trigger switch
            {
                SyntaxCompletionTrigger.MemberAccess => this.CompleteFields(schema, request.PrecedingText),
                SyntaxCompletionTrigger.WordBoundary when ObjectContextKeywords.Contains(request.PrecedingText) => CompleteObjects(schema),
                SyntaxCompletionTrigger.Explicit => CompleteObjects(schema),
                _ => null
            };
        }

        /// <summary>
        /// Builds the table and view candidates.
        /// </summary>
        /// <param name="schema">The loaded schema.</param>
        /// <returns>The candidates.</returns>
        private static List<ICompletionData> CompleteObjects(SqliteSchema schema)
        {
            var results = new List<ICompletionData>();

            foreach (var table in schema.Tables)
            {
                if (!string.IsNullOrEmpty(table.Name))
                {
                    results.Add(new SyntaxCompletionData(table.Name, $"Table\r\n{table.Fields.Count} column(s)", priority: 2.0));
                }
            }

            foreach (var view in schema.Views)
            {
                if (!string.IsNullOrEmpty(view.Name))
                {
                    results.Add(new SyntaxCompletionData(view.Name, $"View\r\n{view.Fields.Count} column(s)"));
                }
            }

            return results;
        }

        /// <summary>
        /// Builds the column candidates for a dotted qualifier, resolving table aliases where possible
        /// and falling back to every column in the database when the qualifier is unrecognized.
        /// </summary>
        /// <param name="schema">The loaded schema.</param>
        /// <param name="qualifier">The identifier that preceded the dot.</param>
        /// <returns>The candidates.</returns>
        private List<ICompletionData> CompleteFields(SqliteSchema schema, string qualifier)
        {
            var fields = this.ResolveFields(schema, qualifier);
            var results = new List<ICompletionData>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field.Name) || !seen.Add(field.Name))
                {
                    continue;
                }

                results.Add(new SyntaxCompletionData(field.Name, DescribeFieldForCompletion(field)));
            }

            return results;
        }

        /// <summary>
        /// Resolves the columns a dotted qualifier refers to.
        /// </summary>
        /// <param name="schema">The loaded schema.</param>
        /// <param name="qualifier">The identifier that preceded the dot.</param>
        /// <returns>The matching columns, or every column when the qualifier cannot be resolved.</returns>
        private IEnumerable<SqliteField> ResolveFields(SqliteSchema schema, string qualifier)
        {
            if (!string.IsNullOrEmpty(qualifier))
            {
                var direct = FindFieldsByName(schema, qualifier);

                if (direct != null)
                {
                    return direct;
                }

                string? aliased = this.ResolveAlias(qualifier);

                if (aliased != null)
                {
                    var byAlias = FindFieldsByName(schema, aliased);

                    if (byAlias != null)
                    {
                        return byAlias;
                    }
                }
            }

            return schema.Tables.SelectMany(x => x.Fields).Concat(schema.Views.SelectMany(x => x.Fields));
        }

        /// <summary>
        /// Finds the columns of a table or view by name.
        /// </summary>
        /// <param name="schema">The loaded schema.</param>
        /// <param name="name">The object name to match.</param>
        /// <returns>The columns, or <see langword="null"/> when no object matches.</returns>
        private static IEnumerable<SqliteField>? FindFieldsByName(SqliteSchema schema, string name)
        {
            var table = schema.Tables.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            if (table != null)
            {
                return table.Fields;
            }

            var view = schema.Views.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            return view?.Fields;
        }

        /// <summary>
        /// Scans the editor text for a <c>FROM|JOIN|UPDATE|INTO object alias</c> pair matching the
        /// supplied alias.
        /// </summary>
        /// <param name="alias">The alias to resolve.</param>
        /// <returns>The object the alias refers to, when one is found.</returns>
        private string? ResolveAlias(string alias)
        {
            string text = this.QueryText;

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            foreach (Match match in AliasPattern.Matches(text))
            {
                string candidate = match.Groups["alias"].Value;

                if (AliasStopWords.Contains(candidate))
                {
                    continue;
                }

                if (string.Equals(candidate, alias, StringComparison.OrdinalIgnoreCase))
                {
                    return match.Groups["name"].Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Renders the description tool tip shown for a column candidate.
        /// </summary>
        /// <param name="field">The column to describe.</param>
        /// <returns>The description text.</returns>
        private static string DescribeFieldForCompletion(SqliteField field)
        {
            var builder = new StringBuilder();
            builder.Append("Type: ").Append(string.IsNullOrWhiteSpace(field.Type) ? "unspecified" : field.Type);
            builder.Append("\r\nNot Null: ").Append(field.NotNull ? "Yes" : "No");

            if (field.PrimaryKey)
            {
                builder.Append("\r\nPrimary Key: Yes");
            }

            if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                builder.Append("\r\nDefault: ").Append(field.DefaultValue);
            }

            return builder.ToString();
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Releases the resources held while the control was loaded.
        /// </summary>
        /// <param name="sender">The control.</param>
        /// <param name="e">The event data.</param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.CancelQuery();
            _completion?.Close();
        }

        /// <summary>
        /// Cancels any running query and releases the completion window and cached result set.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            this.Unloaded -= this.OnUnloaded;
            this.CancelQuery();
            this.DetachTemplateParts();
            this.ClearResults();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
