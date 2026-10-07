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
using System.Globalization;

namespace Mosaic.UI.Wpf.Controls.Scripting.DataList;

/// <summary>
/// A row in a <see cref="DataList"/>. Cells can be read and written by column name or index.
/// </summary>
public sealed class DataListRow
{
    internal DataListRow(DataList owner, List<object?> cells, object? source, long sequence)
    {
        Owner = owner;
        Cells = new DataListCellCollection(cells);
        Source = source;
        Sequence = sequence;
    }

    /// <summary>
    /// Gets the list the row belongs to, or null after it is removed.
    /// </summary>
    internal DataList? Owner { get; set; }

    /// <summary>
    /// Gets the order in which the row was added, used to keep sorting stable.
    /// </summary>
    internal long Sequence { get; }

    /// <summary>
    /// Gets the cell values in column order; the grid binds to these.
    /// </summary>
    public DataListCellCollection Cells { get; }

    /// <summary>
    /// Gets or sets the value of a cell by column name.
    /// </summary>
    /// <param name="column">The column name.</param>
    public object? this[string column]
    {
        get => Get(column);
        set => Set(column, value);
    }

    /// <summary>
    /// Gets or sets the value of a cell by column index.
    /// </summary>
    /// <param name="column">The zero-based column index.</param>
    public object? this[int column]
    {
        get => Get(column);
        set => Set(column, value);
    }

    /// <summary>
    /// Gets or sets an arbitrary value associated with the row; it is not displayed.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Gets the object the row was resolved from when it was added from an object, otherwise null.
    /// </summary>
    public object? Source { get; private set; }

    /// <summary>
    /// Gets the row's display index, or -1 after it has been removed.
    /// </summary>
    public int Index => Owner?.IndexOf(this) ?? -1;

    /// <summary>
    /// Gets a snapshot of the cell values in column order.
    /// </summary>
    public object?[] Values => Run(() => Cells.ToArray());

    /// <summary>
    /// Gets or sets whether the row is selected.
    /// </summary>
    public bool IsSelected
    {
        get
        {
            var owner = Owner;
            return owner != null && owner.Invoke(() => owner.ListView.SelectedItems.Contains(this));
        }
        set
        {
            var owner = Owner ?? throw new InvalidOperationException("The row has been removed from its DataList.");
            owner.Invoke(() =>
            {
                if (value && !owner.ListView.SelectedItems.Contains(this))
                {
                    if (owner.ListView.SelectionMode == System.Windows.Controls.SelectionMode.Single)
                    {
                        owner.ListView.SelectedItem = this;
                    }
                    else
                    {
                        owner.ListView.SelectedItems.Add(this);
                    }
                }
                else if (!value)
                {
                    owner.ListView.SelectedItems.Remove(this);
                }
            });
        }
    }

    /// <summary>
    /// Gets the value of a cell.
    /// </summary>
    /// <param name="column">A column name or zero-based column index.</param>
    [ScriptModuleMethod(Description = "Gets a cell value by column name or index.")]
    public object? Get(object column) => Run(() => Cells[ResolveColumn(column)]);

    /// <summary>
    /// Sets the value of a cell and refreshes the display.
    /// </summary>
    /// <param name="column">A column name or zero-based column index.</param>
    /// <param name="value">The new value.</param>
    [ScriptModuleMethod(Description = "Sets a cell value by column name or index.")]
    public void Set(object column, object? value) => Run(() =>
    {
        int index = ResolveColumn(column);
        Cells.SetCore(index, value);
        Owner?.OnCellChanged(this, index);
        return true;
    });

    /// <summary>
    /// Replaces every cell, using the same arguments as <see cref="DataList.Add"/>.
    /// </summary>
    /// <param name="values">The cell values, or a single object to resolve by property.</param>
    [ScriptModuleMethod(Description = "Replaces every cell using one value per column, or an object whose properties match the column names.")]
    public void Update(params object?[]? values)
    {
        var owner = Owner ?? throw new InvalidOperationException("The row has been removed from its DataList.");
        owner.Invoke(() =>
        {
            var cells = owner.ResolveCells(values, out var source);
            for (int i = 0; i < cells.Count; i++)
            {
                Cells.SetCore(i, cells[i]);
            }

            Source = source ?? Source;
            owner.OnCellChanged(this, null);
        });
    }

    /// <summary>
    /// Removes the row from its list.
    /// </summary>
    /// <returns>True when the row was removed.</returns>
    public bool Remove() => Owner?.Remove(this) ?? false;

    /// <summary>
    /// Returns the cell values separated by tabs.
    /// </summary>
    public override string ToString() => string.Join("\t", Values.Select(v => Convert.ToString(v, CultureInfo.CurrentCulture)));

    private int ResolveColumn(object column)
    {
        int count = Cells.Count;
        int index = column switch
        {
            string name when Owner != null => Owner.Columns.GetCore(name).Index,
            string name => throw new InvalidOperationException($"The row has been removed, so column '{name}' cannot be resolved; use an index."),
            sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToInt32(column, CultureInfo.InvariantCulture),
            _ => throw new ArgumentException("A column must be a name or an index.", nameof(column))
        };

        return index >= 0 && index < count
            ? index
            : throw new ArgumentOutOfRangeException(nameof(column), column, $"The column index must be between 0 and {count - 1}.");
    }

    private T Run<T>(Func<T> func) => Owner != null ? Owner.Invoke(func) : func();
}

/// <summary>
/// The cell values of a <see cref="DataListRow"/>, with change notification for data binding.
/// </summary>
public sealed class DataListCellCollection : IReadOnlyList<object?>, INotifyPropertyChanged
{
    private readonly List<object?> _cells;

    internal DataListCellCollection(List<object?> cells)
    {
        _cells = cells;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the number of cells.
    /// </summary>
    public int Count => _cells.Count;

    /// <summary>
    /// Gets the value at the specified column index, or null when there is no such cell.
    /// </summary>
    /// <param name="index">The zero-based column index.</param>
    public object? this[int index] => index >= 0 && index < _cells.Count ? _cells[index] : null;

    /// <inheritdoc />
    public IEnumerator<object?> GetEnumerator() => _cells.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void SetCore(int index, object? value)
    {
        _cells[index] = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    internal void Pad(int count)
    {
        while (_cells.Count < count)
        {
            _cells.Add(null);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
