# SqliteQueryControl

**Base class:** `Control` (implements `IDisposable`)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/SqliteQueryControl/SqliteQueryControl.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/SqliteQueryControlExample.xaml`

## Description

A SQLite query workbench in one control: a database explorer tree, a [SyntaxEditor](SyntaxEditor.md)-based SQL editor with schema-aware completion, an [ExecutionControl](ExecutionControl.md) run/cancel tool bar, and a results `DataGrid`.

Point it at a database with `DatabaseFilePath` (or `ConnectionString`) and the schema loads into the explorer. The explorer's context menu is rebuilt on every open and surfaced as `SchemaContextMenuRequested` so consumers can add or remove items.

**Keyboard:** F5 executes the selection (or the whole document when nothing is selected); Ctrl+Space requests completion.

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_SchemaTree` | `TreeView` | Database explorer. |
| `PART_SqlEditor` | `SyntaxEditor` | SQL editor. |
| `PART_Results` | `DataGrid` | Result set grid. |
| `PART_ExecutionControl` | `ExecutionControl` | Run / cancel tool bar. |
| `PART_RefreshButton` | `ButtonBase` | Reloads the schema. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string?` | `null` | Connection string used to reach the database. Setting it reloads the schema. |
| `DatabaseFilePath` | `string?` | `null` | Convenience over `ConnectionString` — projects a file path onto it. |
| `Schema` | `SqliteSchema?` | `null` | The schema as of the last refresh. Drives the explorer and completion. |
| `QueryText` | `string` | `""` | The text currently in the SQL editor (kept in sync both ways). |
| `StatusText` | `string` | — | Status message shown beside the run tool bar. |
| `IsQueryExecuting` | `bool` | `false` | Whether a query is currently running. |
| `RefreshSchemaAfterQuery` | `bool` | — | Reload the schema after every query so DDL is reflected immediately. |
| `AutoCompleteEnabled` | `bool` | — | Whether schema-aware auto completion is offered in the editor. |
| `RowLimit` | `int` | `1000` | Row count used by the explorer's *Select Top N Rows* command. |

## Events

| Event | Args | Description |
|---|---|---|
| `SchemaContextMenuRequested` | `SqliteSchemaContextMenuEventArgs` (bubbling) | Raised after the standard explorer menu items are populated. Add/remove items, or set `Cancel` to suppress the menu. |
| `QueryCompleted` | `RoutedEventArgs` (bubbling) | Raised after a query finishes — succeeded, failed, or cancelled. |

`SqliteSchemaContextMenuEventArgs` carries `ContextMenu`, `NodeKind` (`SqliteSchemaNodeKind`), `Node`, `ObjectName`, and a settable `Cancel`.

## Commands and Methods

| Member | Description |
|---|---|
| `ExecuteQueryCommand` (`IAsyncRelayCommand`) | Runs the selection, or the whole document when nothing is selected. |
| `CancelQueryCommand` (`IRelayCommand`) | Cancels the running query. |
| `RefreshSchemaCommand` (`IAsyncRelayCommand`) | Reloads the schema into the explorer. |
| `Task OpenDatabaseAsync(string filePath)` | Opens a database file and loads its schema. |
| `Task RefreshSchemaAsync()` | Reloads the schema. |
| `Task ExecuteQueryAsync(string sql)` | Executes SQL and shows the first result set in the grid. |

## Schema model

`SqliteSchema` (`DatabaseName`, `ConnectionString`, `Tables`, `Views`) → `SqliteTable` / `SqliteView` (`Name`, `Sql`, `Fields`) → `SqliteField` (`ColumnId`, `Name`, `Type`, `NotNull`, `DefaultValue`, `PrimaryKey`). All are `ObservableObject`s. `SqliteSchemaLoader` performs the reads.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:SqliteQueryControl
    DatabaseFilePath="{Binding DatabasePath}"
    RowLimit="500"
    RefreshSchemaAfterQuery="True"
    QueryCompleted="Sqlite_QueryCompleted"
    SchemaContextMenuRequested="Sqlite_SchemaContextMenuRequested" />
```

## Notes

- The explorer generates `SELECT` / `INSERT` / `UPDATE` statements for a table or view, annotating each column with its type and key role, and appends them to the editor.
- Generated column headers double underscores, since WPF treats a single underscore in a header as an access key.
- Call `Dispose()` when tearing the control down — it detaches handlers and disposes the active result set.
