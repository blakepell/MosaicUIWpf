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
using System.Windows.Data;
using Path = System.Windows.Shapes.Path;
using Shape = System.Windows.Shapes.Shape;
using Mosaic.UI.Wpf.Themes;

namespace Mosaic.UI.Wpf.Controls.Scripting.DataList;

/// <summary>
/// A column in a <see cref="DataList"/>.
/// </summary>
public sealed class DataListColumn
{
    private readonly DataList _owner;
    private string _header;
    private TextAlignment _alignment = TextAlignment.Left;
    private string? _format;
    private string? _property;
    private bool _fixedWidth;

    internal DataListColumn(DataList owner, string name, int index)
    {
        _owner = owner;
        _header = name;
        Name = name;
        Index = index;
        GridColumn = new GridViewColumn();
        UpdateHeader();
        UpdateCellTemplate();
    }

    /// <summary>
    /// Gets the underlying grid view column.
    /// </summary>
    internal GridViewColumn GridColumn { get; }

    /// <summary>
    /// Gets the column name used to look up the column and resolve object properties.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the zero-based position of the column.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets or sets the header text, which defaults to the column name.
    /// </summary>
    public string Header
    {
        get => _header;
        set => _owner.Invoke(() =>
        {
            _header = value ?? string.Empty;
            UpdateHeader();
        });
    }

    /// <summary>
    /// Gets or sets the width, or NaN when automatic. A negative value sizes the column automatically
    /// and 0 hides it. A column given a fixed width is left alone by <see cref="DataList.AutoSize"/>.
    /// </summary>
    [ScriptModuleMethod(Description = "The column width. A negative value sizes automatically; 0 hides the column. Fixed widths are kept by grid.AutoSize().")]
    public double Width
    {
        get => _owner.Invoke(() => GridColumn.Width);
        set => _owner.Invoke(() =>
        {
            _fixedWidth = value >= 0;
            GridColumn.Width = _fixedWidth ? value : double.NaN;
        });
    }

    /// <summary>
    /// Gets the rendered width.
    /// </summary>
    public double ActualWidth => _owner.Invoke(() => GridColumn.ActualWidth);

    /// <summary>
    /// Gets or sets the cell text alignment: Left, Center or Right (case-insensitive).
    /// </summary>
    [ScriptModuleMethod(Description = "Left, Center or Right.")]
    public string Align
    {
        get => _alignment.ToString();
        set
        {
            var alignment = DataList.ParseEnum<TextAlignment>(value, nameof(Align));
            if (alignment == TextAlignment.Justify)
            {
                throw new ArgumentException("Align must be Left, Center or Right.", nameof(Align));
            }

            _owner.Invoke(() =>
            {
                _alignment = alignment;
                UpdateCellTemplate();
            });
        }
    }

    /// <summary>
    /// Gets or sets a .NET format string for the cells, such as "N2", "C" or "yyyy-MM-dd".
    /// </summary>
    [ScriptModuleMethod(Description = "A .NET format string for cell values, such as N2, C or yyyy-MM-dd.")]
    public string? Format
    {
        get => _format;
        set => _owner.Invoke(() =>
        {
            _format = string.IsNullOrEmpty(value) ? null : value;
            UpdateCellTemplate();
        });
    }

    /// <summary>
    /// Gets or sets the property name to read when a row is added from an object; defaults to the column name.
    /// </summary>
    [ScriptModuleMethod(Description = "The object property this column reads when a row is added from an object. Defaults to the column name.")]
    public string? Property
    {
        get => _property;
        set => _owner.Invoke(() => _property = string.IsNullOrWhiteSpace(value) ? null : value);
    }

    /// <summary>
    /// Gets or sets whether clicking the header sorts by this column.
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// Sizes the column to fit its header and displayed cells, replacing any fixed width.
    /// </summary>
    public void AutoSize() => _owner.Invoke(() =>
    {
        _fixedWidth = false;
        AutoSizeCore();
    });

    internal void AutoSizeCore()
    {
        if (_fixedWidth)
        {
            return;
        }

        // Assigning a fixed width resets the measured size so NaN measures again.
        GridColumn.Width = GridColumn.ActualWidth;
        GridColumn.Width = double.NaN;
    }

    /// <summary>
    /// Gets the names tried, in order, when resolving this column from an object.
    /// </summary>
    internal IEnumerable<string> GetMemberNames()
    {
        if (_property != null)
        {
            yield return _property;
            yield break;
        }

        yield return Name;
        string compact = new(Name.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (compact != Name)
        {
            yield return compact;
        }
    }

    /// <summary>
    /// Shows the header text followed, when sorted by this column, by the same accent chevron the
    /// Mosaic DataGrid column header uses.
    /// </summary>
    internal void UpdateHeader()
    {
        GridColumn.Header = _header;

        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetValue(TextBlock.TextProperty, _header);
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        panel.AppendChild(text);

        if (_owner.GetSortDirection(this) is { } direction)
        {
            var chevron = new FrameworkElementFactory(typeof(Path));
            chevron.SetValue(Path.DataProperty, Geometry.Parse(direction == ListSortDirection.Ascending ? "M 1,5 L 5,1 L 9,5" : "M 1,1 L 5,5 L 9,1"));
            chevron.SetValue(FrameworkElement.WidthProperty, 10.0);
            chevron.SetValue(FrameworkElement.HeightProperty, 6.0);
            chevron.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 1, 0));
            chevron.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            chevron.SetValue(Shape.StretchProperty, Stretch.Uniform);
            chevron.SetValue(Shape.StrokeThicknessProperty, 1.5);
            chevron.SetValue(Shape.StrokeStartLineCapProperty, PenLineCap.Round);
            chevron.SetValue(Shape.StrokeEndLineCapProperty, PenLineCap.Round);
            chevron.SetValue(Shape.StrokeLineJoinProperty, PenLineJoin.Round);
            chevron.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
            chevron.SetResourceReference(Shape.StrokeProperty, MosaicTheme.AccentBrush);
            panel.AppendChild(chevron);
        }

        GridColumn.HeaderTemplate = new DataTemplate { VisualTree = panel };
    }

    private void UpdateCellTemplate()
    {
        var binding = new Binding($"{nameof(DataListRow.Cells)}[{Index}]") { Mode = BindingMode.OneWay };
        if (_format != null)
        {
            binding.StringFormat = _format.Contains('{') ? _format : $"{{0:{_format}}}";
        }

        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.SetBinding(TextBlock.TextProperty, binding);
        text.SetValue(TextBlock.TextAlignmentProperty, _alignment);
        text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        GridColumn.CellTemplate = new DataTemplate { VisualTree = text };
    }
}

/// <summary>
/// The columns of a <see cref="DataList"/>, which can be looked up by name or index.
/// </summary>
public sealed class DataListColumnCollection : IEnumerable<DataListColumn>
{
    private readonly DataList _owner;
    private readonly List<DataListColumn> _columns = new();

    internal DataListColumnCollection(DataList owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Count => _owner.Invoke(() => _columns.Count);

    /// <summary>
    /// Gets a column by name (case-insensitive).
    /// </summary>
    /// <param name="name">The column name.</param>
    public DataListColumn this[string name] => _owner.Invoke(() => GetCore(name));

    /// <summary>
    /// Gets a column by index.
    /// </summary>
    /// <param name="index">The zero-based column index.</param>
    public DataListColumn this[int index] => _owner.Invoke(() =>
        index >= 0 && index < _columns.Count
            ? _columns[index]
            : throw new ArgumentOutOfRangeException(nameof(index), index, $"The column index must be between 0 and {_columns.Count - 1}."));

    /// <summary>
    /// Adds a column; existing rows get an empty cell for it.
    /// </summary>
    /// <param name="name">The column name, also used as its header.</param>
    [ScriptModuleMethod(Description = "Adds a column. Existing rows get an empty cell for it.", ReturnType = typeof(DataListColumn))]
    public DataListColumn Add(string name) => _owner.Invoke(() =>
    {
        var column = AddCore(name);
        _owner.OnColumnAdded();
        return column;
    });

    /// <summary>
    /// Gets whether a column with the specified name exists.
    /// </summary>
    /// <param name="name">The column name.</param>
    public bool Contains(string name) => _owner.Invoke(() => Find(name) != null);

    /// <inheritdoc />
    public IEnumerator<DataListColumn> GetEnumerator() => _owner.Invoke(() => _columns.ToList()).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal DataListColumn AddCore(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A column name cannot be empty.", nameof(name));
        }

        if (Find(name) != null)
        {
            throw new ArgumentException($"A column named '{name}' already exists.", nameof(name));
        }

        var column = new DataListColumn(_owner, name, _columns.Count);
        _columns.Add(column);
        ((GridView)_owner.ListView.View).Columns.Add(column.GridColumn);
        return column;
    }

    internal DataListColumn GetCore(string name)
    {
        return Find(name) ?? throw new ArgumentException($"There is no column named '{name}'. Columns: {string.Join(", ", _columns.Select(c => c.Name))}.", nameof(name));
    }

    private DataListColumn? Find(string name)
    {
        return _columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal))
               ?? _columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
