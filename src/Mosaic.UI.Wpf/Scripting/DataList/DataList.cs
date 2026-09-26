/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Scripting;

/// <summary>
/// A script-friendly <see cref="System.Windows.Controls.ListView"/> with a <see cref="GridView"/>
/// whose columns, sorting, selection and row editing are already wired up.
/// </summary>
/// <remarks>
/// Scripts run on a worker thread, so every member marshals to the WPF dispatcher that owns the
/// underlying <see cref="ListView"/>. Row indexes are always in display (sorted) order. Hosts can
/// place <see cref="ListView"/> in their own layout, or scripts can call <see cref="Show"/>.
/// </remarks>
[ScriptModule(Name = "DataList", Description = "A sortable list view with columns: new DataList('Name', 'Age')")]
public sealed class DataList
{
    private readonly Dispatcher _dispatcher;
    private readonly RowCollection _rows = new();
    private readonly List<DataListRow> _pending = new();
    private GridView _gridView = null!;
    private ListCollectionView _view = null!;
    private DataListWindow? _window;
    private DataListColumn? _sortColumn;
    private bool _sortDescending;
    private int _updateDepth;
    private long _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataList"/> class with the supplied columns.
    /// </summary>
    /// <param name="columns">The column names, which are also used as the column headers.</param>
    public DataList(params string[] columns) : this(ResolveDispatcher(), columns)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataList"/> class on a specific dispatcher.
    /// </summary>
    internal DataList(Dispatcher dispatcher, params string[] columns)
    {
        _dispatcher = dispatcher;
        Columns = new DataListColumnCollection(this);
        Invoke(() =>
        {
            _gridView = new GridView();
            ListView = new ListView
            {
                View = _gridView,
                ItemsSource = _rows,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                SelectionMode = System.Windows.Controls.SelectionMode.Extended
            };
            ListView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(OnColumnHeaderClick));
            _view = (ListCollectionView)CollectionViewSource.GetDefaultView(_rows);

            foreach (string column in columns ?? [])
            {
                Columns.AddCore(column);
            }
        });
    }

    /// <summary>
    /// Gets the underlying list view so a host can place it in its own layout.
    /// </summary>
    /// <remarks>This is a WPF element; only access it on its dispatcher thread.</remarks>
    [ScriptHidden]
    public ListView ListView { get; private set; } = null!;

    /// <summary>
    /// Gets the columns, which can be looked up by name or index.
    /// </summary>
    [ScriptModuleMethod(Description = "The columns, by name or index: grid.Columns['Age'].Width = 80")]
    public DataListColumnCollection Columns { get; }

    /// <summary>
    /// Gets the number of rows, including rows added since <see cref="BeginUpdate"/>.
    /// </summary>
    public int Count => Invoke(() => _rows.Count + _pending.Count);

    /// <summary>
    /// Gets whether <see cref="BeginUpdate"/> is deferring changes.
    /// </summary>
    public bool IsUpdating => Invoke(() => _updateDepth > 0);

    /// <summary>
    /// Gets a snapshot of the rows in display order.
    /// </summary>
    public DataListRow[] Rows => Invoke(() => _view.Cast<DataListRow>().Concat(_pending).ToArray());

    /// <summary>
    /// Gets the row at the specified display index.
    /// </summary>
    /// <param name="index">The zero-based display index.</param>
    public DataListRow this[int index] => GetRow(index);

    /// <summary>
    /// Gets or sets the selected display index, or -1 when nothing is selected.
    /// </summary>
    public int SelectedIndex
    {
        get => Invoke(() => ListView.SelectedIndex);
        set => Invoke(() =>
        {
            ListView.SelectedIndex = value;
            ScrollSelectionIntoView();
        });
    }

    /// <summary>
    /// Gets or sets the selected row, or null when nothing is selected.
    /// </summary>
    public DataListRow? SelectedRow
    {
        get => Invoke(() => ListView.SelectedItem as DataListRow);
        set => Invoke(() =>
        {
            ListView.SelectedItem = value;
            ScrollSelectionIntoView();
        });
    }

    /// <summary>
    /// Gets the selected rows.
    /// </summary>
    public DataListRow[] SelectedRows => Invoke(() => ListView.SelectedItems.Cast<DataListRow>().ToArray());

    /// <summary>
    /// Gets or sets the selection mode: Single, Multiple or Extended (case-insensitive).
    /// </summary>
    [ScriptModuleMethod(Description = "Single, Multiple or Extended (the default).")]
    public string SelectionMode
    {
        get => Invoke(() => ListView.SelectionMode.ToString());
        set
        {
            var mode = ParseEnum<System.Windows.Controls.SelectionMode>(value, nameof(SelectionMode));
            Invoke(() => ListView.SelectionMode = mode);
        }
    }

    /// <summary>
    /// Gets the name of the column the rows are sorted by, or null when unsorted.
    /// </summary>
    public string? SortColumn => Invoke(() => _sortColumn?.Name);

    /// <summary>
    /// Gets whether the rows are sorted in descending order.
    /// </summary>
    public bool SortDescending => Invoke(() => _sortDescending);

    /// <summary>
    /// Adds a row and returns it.
    /// </summary>
    /// <remarks>
    /// Pass one value per column, or a single object (a JavaScript object, dictionary, array or
    /// .NET instance) whose properties are matched to the column names. A column whose name has
    /// spaces also matches the property without them, so "First Name" resolves FirstName.
    /// </remarks>
    /// <param name="values">The cell values, or a single object to resolve by property.</param>
    [ScriptModuleMethod(Description = "Adds a row from one value per column, or from an object whose properties match the column names. Returns the row.",
        ReturnType = typeof(DataListRow))]
    public DataListRow Add(params object?[]? values)
    {
        return Invoke(() =>
        {
            var cells = ResolveCells(values, out object? source);
            var row = new DataListRow(this, cells, source, ++_sequence);

            if (_updateDepth > 0)
            {
                _pending.Add(row);
            }
            else
            {
                _rows.Add(row);
            }

            return row;
        });
    }

    /// <summary>
    /// Removes a row, or the row at a display index.
    /// </summary>
    /// <param name="rowOrIndex">A <see cref="DataListRow"/> or a zero-based display index.</param>
    /// <returns>True when a row was removed.</returns>
    [ScriptModuleMethod(Description = "Removes a row object, or the row at a display index. Returns true when a row was removed.")]
    public bool Remove(object? rowOrIndex)
    {
        return rowOrIndex switch
        {
            DataListRow row => Invoke(() => RemoveCore(row)),
            null => false,
            _ when IsNumeric(rowOrIndex) => Invoke(() => RemoveCore(GetRowCore(Convert.ToInt32(rowOrIndex, CultureInfo.InvariantCulture)))),
            _ => throw new ArgumentException("Remove expects a DataListRow or a row index.", nameof(rowOrIndex))
        };
    }

    /// <summary>
    /// Removes the row at the specified display index.
    /// </summary>
    /// <param name="index">The zero-based display index.</param>
    public void RemoveAt(int index) => Invoke(() => RemoveCore(GetRowCore(index)));

    /// <summary>
    /// Removes every row; the columns are kept.
    /// </summary>
    public void Clear() => Invoke(() =>
    {
        foreach (var row in _rows.Concat(_pending))
        {
            row.Owner = null;
        }

        _pending.Clear();
        _rows.Clear();
    });

    /// <summary>
    /// Gets the row at the specified display index.
    /// </summary>
    /// <param name="index">The zero-based display index.</param>
    public DataListRow GetRow(int index) => Invoke(() => GetRowCore(index));

    /// <summary>
    /// Gets the display index of a row, or -1 when it does not belong to this list.
    /// </summary>
    /// <param name="row">The row to find.</param>
    public int IndexOf(DataListRow? row) => row == null ? -1 : Invoke(() =>
    {
        int index = _view.IndexOf(row);
        if (index >= 0)
        {
            return index;
        }

        index = _pending.IndexOf(row);
        return index < 0 ? -1 : _view.Count + index;
    });

    /// <summary>
    /// Defers display updates until the matching <see cref="EndUpdate"/>, which is much faster for
    /// large numbers of rows. Calls may be nested.
    /// </summary>
    [ScriptModuleMethod(Description = "Defers display updates until EndUpdate(); use around large numbers of Add calls.")]
    public void BeginUpdate() => Invoke(() => _updateDepth++);

    /// <summary>
    /// Ends a <see cref="BeginUpdate"/> block and displays the deferred rows in a single refresh.
    /// </summary>
    public void EndUpdate() => Invoke(() =>
    {
        if (_updateDepth == 0 || --_updateDepth > 0)
        {
            return;
        }

        _rows.AddRange(_pending);
        _pending.Clear();
    });

    /// <summary>
    /// Sizes every column that has no fixed width to fit its header and displayed cells.
    /// </summary>
    /// <remarks>
    /// Columns given a width (including 0, which hides them) keep it; call the column's own AutoSize
    /// to size it anyway. Only rows that have been displayed are measured, because the list virtualizes.
    /// </remarks>
    [ScriptModuleMethod(Description = "Sizes every column without a fixed width to fit its header and displayed cells.")]
    public void AutoSize() => Invoke(() =>
    {
        foreach (var column in Columns)
        {
            column.AutoSizeCore();
        }

        // Newly added rows are measured on the next layout pass, so size again once it has run.
        _dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            foreach (var column in Columns)
            {
                column.AutoSizeCore();
            }
        });
    });

    /// <summary>
    /// Sorts the rows by a column; clicking a column header does the same.
    /// </summary>
    /// <param name="column">The column name.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    [ScriptModuleMethod(Description = "Sorts the rows by a column name. Clicking a header toggles its sort direction.")]
    public void Sort(string column, bool descending = false) => Invoke(() => SortCore(Columns.GetCore(column), descending));

    /// <summary>
    /// Removes sorting so the rows display in the order they were added.
    /// </summary>
    public void ClearSort() => Invoke(() => SortCore(null, false));

    /// <summary>
    /// Scrolls a row, or the row at a display index, into view.
    /// </summary>
    /// <param name="rowOrIndex">A <see cref="DataListRow"/> or a zero-based display index.</param>
    public void ScrollIntoView(object rowOrIndex) => Invoke(() =>
    {
        var row = rowOrIndex as DataListRow ?? GetRowCore(Convert.ToInt32(rowOrIndex, CultureInfo.InvariantCulture));
        ListView.ScrollIntoView(row);
    });

    /// <summary>
    /// Shows the list in a themed, non-modal window. Showing it again activates the same window.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="width">The window width.</param>
    /// <param name="height">The window height.</param>
    [ScriptModuleMethod(Description = "Shows the list in a non-modal window.", ParameterCount = 3)]
    public void Show(string title = "Data List", double width = 640, double height = 420) => Invoke(() =>
    {
        var window = GetWindow(title, width, height);
        window.Show();
        window.Activate();
    });

    /// <summary>
    /// Shows the list in a themed, modal window and waits until the user closes it.
    /// </summary>
    /// <param name="title">The window title.</param>
    /// <param name="width">The window width.</param>
    /// <param name="height">The window height.</param>
    /// <returns>The row selected when the window closed, or null.</returns>
    [ScriptModuleMethod(Description = "Shows the list in a modal window and waits until it closes. Returns the selected row, or null.",
        ParameterCount = 3, ReturnType = typeof(DataListRow))]
    public DataListRow? ShowDialog(string title = "Data List", double width = 640, double height = 420) => Invoke(() =>
    {
        _window?.Close();
        GetWindow(title, width, height).ShowDialog();
        return ListView.SelectedItem as DataListRow;
    });

    /// <summary>
    /// Closes the window opened by <see cref="Show"/> or <see cref="ShowDialog"/>.
    /// </summary>
    public void Close() => Invoke(() => _window?.Close());

    /// <summary>
    /// Runs an action on the list's dispatcher.
    /// </summary>
    internal void Invoke(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _dispatcher.Invoke(action);
        }
    }

    /// <summary>
    /// Runs a function on the list's dispatcher.
    /// </summary>
    internal T Invoke<T>(Func<T> func) => _dispatcher.CheckAccess() ? func() : _dispatcher.Invoke(func);

    /// <summary>
    /// Adds an empty cell to every row after a column is added.
    /// </summary>
    internal void OnColumnAdded()
    {
        foreach (var row in _rows.Concat(_pending))
        {
            row.Cells.Pad(Columns.Count);
        }
    }

    /// <summary>
    /// Keeps a sorted row in place after one of its cells changes.
    /// </summary>
    /// <param name="row">The changed row.</param>
    /// <param name="columnIndex">The changed column, or null when every cell changed.</param>
    internal void OnCellChanged(DataListRow row, int? columnIndex)
    {
        if (_sortColumn != null && (columnIndex == null || columnIndex == _sortColumn.Index) && _view.Contains(row))
        {
            _view.EditItem(row);
            _view.CommitEdit();
        }
    }

    /// <summary>
    /// Converts Add/Update arguments to one cell per column.
    /// </summary>
    internal List<object?> ResolveCells(object?[]? values, out object? source)
    {
        source = null;
        values ??= [null];
        var columns = Columns.ToList();

        if (values.Length == 1 && values[0] is { } record && !IsScalar(record))
        {
            if (record is IList list)
            {
                values = list.Cast<object?>().ToArray();
            }
            else if (record is IList<object?> genericList)
            {
                values = genericList.ToArray();
            }
            else if (TryResolveRecord(record, columns, out var cells))
            {
                source = record;
                return cells;
            }
            else if (columns.Count > 1)
            {
                throw new ArgumentException($"None of the columns ({string.Join(", ", columns.Select(c => c.Name))}) match a property of {record.GetType().Name}.");
            }
        }

        if (values.Length > columns.Count)
        {
            throw new ArgumentException($"The DataList has {columns.Count} column(s) but {values.Length} values were supplied.");
        }

        var result = new List<object?>(values);
        while (result.Count < columns.Count)
        {
            result.Add(null);
        }

        return result;
    }

    /// <summary>
    /// Compares two cell values: nulls first, then numbers (including numeric text), then values
    /// of the same comparable type, then text ignoring case.
    /// </summary>
    internal static int CompareValues(object? a, object? b)
    {
        if (a is null or DBNull)
        {
            return b is null or DBNull ? 0 : -1;
        }

        if (b is null or DBNull)
        {
            return 1;
        }

        if (TryGetNumber(a, out double x) && TryGetNumber(b, out double y))
        {
            return x.CompareTo(y);
        }

        if (a.GetType() == b.GetType() && a is IComparable comparable)
        {
            return comparable.CompareTo(b);
        }

        return string.Compare(Convert.ToString(a, CultureInfo.CurrentCulture), Convert.ToString(b, CultureInfo.CurrentCulture), StringComparison.CurrentCultureIgnoreCase);
    }

    private static Dispatcher ResolveDispatcher()
    {
        return Application.Current?.Dispatcher
               ?? (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
                   ? Dispatcher.CurrentDispatcher
                   : throw new InvalidOperationException("A DataList requires a WPF Application."));
    }

    private DataListRow GetRowCore(int index)
    {
        int visible = _view.Count;
        if (index < 0 || index >= visible + _pending.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"The index must be between 0 and {visible + _pending.Count - 1}.");
        }

        return index < visible ? (DataListRow)_view.GetItemAt(index) : _pending[index - visible];
    }

    private bool RemoveCore(DataListRow row)
    {
        if (!_pending.Remove(row) && !_rows.Remove(row))
        {
            return false;
        }

        row.Owner = null;
        return true;
    }

    private void SortCore(DataListColumn? column, bool descending)
    {
        var previous = _sortColumn;
        _sortColumn = column;
        _sortDescending = column != null && descending;
        _view.CustomSort = column == null ? null : new RowComparer(column.Index, _sortDescending);
        previous?.UpdateHeader();
        column?.UpdateHeader();
    }

    /// <summary>
    /// Gets the direction a column is sorted in, or null when the rows are not sorted by it.
    /// </summary>
    internal ListSortDirection? GetSortDirection(DataListColumn column)
    {
        return ReferenceEquals(column, _sortColumn) ? (_sortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending) : null;
    }

    private void OnColumnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader { Column: { } gridColumn, Role: not GridViewColumnHeaderRole.Padding })
        {
            return;
        }

        var column = Columns.FirstOrDefault(c => ReferenceEquals(c.GridColumn, gridColumn));
        if (column is { Sortable: true })
        {
            SortCore(column, ReferenceEquals(column, _sortColumn) && !_sortDescending);
        }
    }

    private void ScrollSelectionIntoView()
    {
        if (ListView.SelectedItem != null)
        {
            ListView.ScrollIntoView(ListView.SelectedItem);
        }
    }

    private DataListWindow GetWindow(string title, double width, double height)
    {
        if (_window == null)
        {
            if (ListView.Parent != null)
            {
                throw new InvalidOperationException("The DataList is already hosted in another element.");
            }

            _window = new DataListWindow(ListView);
            _window.Closed += (_, _) =>
            {
                _window?.ReleaseContent();
                _window = null;
            };

            var owner = Application.Current?.MainWindow;
            if (owner is { IsLoaded: true })
            {
                _window.Owner = owner;
            }
            else
            {
                _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        _window.Title = string.IsNullOrWhiteSpace(title) ? "Data List" : title;
        _window.Width = width > 0 ? width : _window.Width;
        _window.Height = height > 0 ? height : _window.Height;
        return _window;
    }

    private static bool TryResolveRecord(object record, List<DataListColumn> columns, out List<object?> cells)
    {
        cells = new List<object?>(columns.Count);
        bool isDictionary = record is IDictionary or IDictionary<string, object?>;
        bool matched = false;

        foreach (var column in columns)
        {
            object? value = null;
            foreach (string key in column.GetMemberNames())
            {
                if (TryGetMember(record, key, out value))
                {
                    matched = true;
                    break;
                }
            }

            cells.Add(value);
        }

        // A dictionary is never a useful cell value, so it always resolves by key.
        return matched || isDictionary;
    }

    private static bool TryGetMember(object record, string name, out object? value)
    {
        switch (record)
        {
            case IDictionary dictionary:
                if (dictionary.Contains(name))
                {
                    value = dictionary[name];
                    return true;
                }

                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string key && string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = entry.Value;
                        return true;
                    }
                }

                break;

            case IDictionary<string, object?> dictionary:
                if (dictionary.TryGetValue(name, out value))
                {
                    return true;
                }

                foreach (var entry in dictionary)
                {
                    if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = entry.Value;
                        return true;
                    }
                }

                break;

            default:
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
                var type = record.GetType();
                var property = type.GetProperties(flags)
                    .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.Name == name ? 0 : 1)
                    .FirstOrDefault();

                if (property != null)
                {
                    value = property.GetValue(record);
                    return true;
                }

                var field = type.GetField(name, flags);
                if (field != null)
                {
                    value = field.GetValue(record);
                    return true;
                }

                break;
        }

        value = null;
        return false;
    }

    private static bool IsScalar(object value)
    {
        return value is string or char or bool or decimal or DateTime or DateTimeOffset or TimeSpan or Guid or Enum or DBNull
               || value.GetType().IsPrimitive;
    }

    private static bool IsNumeric(object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private static bool TryGetNumber(object value, out double number)
    {
        if (IsNumeric(value))
        {
            number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }

        if (value is string text)
        {
            return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out number);
        }

        number = 0;
        return false;
    }

    internal static T ParseEnum<T>(string value, string parameterName) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new ArgumentException($"'{value}' is not valid; expected one of: {string.Join(", ", Enum.GetNames<T>())}.", parameterName);
    }

    /// <summary>
    /// Sorts rows by one column, keeping insertion order for equal values.
    /// </summary>
    private sealed class RowComparer(int columnIndex, bool descending) : IComparer
    {
        public int Compare(object? x, object? y)
        {
            var a = (DataListRow)x!;
            var b = (DataListRow)y!;
            int result = CompareValues(a.Cells[columnIndex], b.Cells[columnIndex]);
            if (descending)
            {
                result = -result;
            }

            return result != 0 ? result : a.Sequence.CompareTo(b.Sequence);
        }
    }

    /// <summary>
    /// An observable collection that can add many rows with a single reset notification.
    /// </summary>
    private sealed class RowCollection : ObservableCollection<DataListRow>
    {
        public void AddRange(IReadOnlyCollection<DataListRow> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            CheckReentrancy();
            foreach (var row in rows)
            {
                Items.Add(row);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
