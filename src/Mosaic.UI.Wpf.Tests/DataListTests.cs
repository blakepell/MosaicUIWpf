/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Mosaic.UI.Wpf.Scripting;
using Xunit;

namespace Mosaic.UI.Wpf.Tests;

public class DataListTests
{
    [Fact]
    public void ScriptsAddEditSortSelectAndRemoveRows() => RunStaAsync(async () =>
    {
        var list = new DataList(Dispatcher.CurrentDispatcher, "First Name", "Last Name", "Age");
        var environment = new ScriptEnvironment();
        Assert.True(environment.Registrations["DataList"].IsType);
        environment.RegisterObject("grid", list);
        environment.RegisterType(typeof(Person));

        await environment.ExecuteAsync("""
            grid.BeginUpdate();
            grid.Add('Blake', 'Pell', 47);
            grid.Add('Lucy', 'Pell', 11);
            var row = grid.Add('Isaac', 'Pell', 17);
            row.Tag = 'Cool Person';
            grid.Columns['Age'].Width = 80;
            grid.Columns['Age'].Align = 'right';
            globals.updating = grid.IsUpdating;
            globals.pendingIndex = row.Index;
            grid.EndUpdate();

            grid.Add({ FirstName: 'John', 'Last Name': 'Butler', Age: 22 });
            let person = new Person();
            person.FirstName = 'Billy';
            person.LastName = 'Thompson';
            person.Age = 102;
            grid.Add(person);

            grid.Sort('Age', true);
            globals.oldest = grid[0]['First Name'];
            row['Age'] = 200;
            globals.afterEdit = grid.GetRow(0).Get(0);
            globals.tag = grid[0].Tag;

            grid.Remove(row);
            grid.Remove(0);
            globals.count = grid.Count;
            grid.SelectedIndex = 0;
            globals.selected = grid.SelectedRow['First Name'];
            grid.AutoSize();
            """);

        Assert.Equal(true, environment.Globals["updating"]);
        Assert.Equal(2, environment.Globals["pendingIndex"]);
        Assert.Equal("Billy", environment.Globals["oldest"]);
        Assert.Equal("Isaac", environment.Globals["afterEdit"]);
        Assert.Equal("Cool Person", environment.Globals["tag"]);
        Assert.Equal(3, environment.Globals["count"]);
        Assert.Equal("Blake", environment.Globals["selected"]);
        Assert.Equal(80, list.Columns["Age"].Width);
        Assert.Equal("Right", list.Columns["Age"].Align);
        Assert.Equal(["Blake", "John", "Lucy"], list.Rows.Select(r => r["First Name"]));
        Assert.NotNull(list.Rows.Single(r => (string?)r["First Name"] == "John").Source);

        await environment.ExecuteAsync("grid.Clear(); globals.cleared = grid.Count; globals.columns = grid.Columns.Count;");
        Assert.Equal(0, environment.Globals["cleared"]);
        Assert.Equal(3, environment.Globals["columns"]);
    });

    [Fact]
    public void RecordsResolveByPropertyAndInvalidInputIsReported() => RunStaAsync(() =>
    {
        var list = new DataList(Dispatcher.CurrentDispatcher, "FirstName", "Age");
        var person = new Person { FirstName = "Ann", Age = 30 };
        var row = list.Add(person);
        Assert.Same(person, row.Source);
        Assert.Equal(new object?[] { "Ann", 30 }, row.Values);

        list.Columns["FirstName"].Property = "LastName";
        Assert.Equal(new object?[] { "Smith", 5 }, list.Add(new Person { LastName = "Smith", Age = 5 }).Values);
        Assert.Equal(new object?[] { "only", null }, list.Add("only").Values);
        Assert.Equal(new object?[] { 1, 2 }, list.Add(new List<object?> { 1, 2 }).Values);

        Assert.Throws<ArgumentException>(() => list.Add(1, 2, 3));
        Assert.Throws<ArgumentException>(() => list.Add(new Uri("https://example.com")));
        Assert.Throws<ArgumentException>(() => list.Columns["Missing"]);
        Assert.Throws<ArgumentException>(() => list.Columns.Add("age"));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Remove(10));
        Assert.Throws<ArgumentException>(() => list.SelectionMode = "Sideways");

        var added = list.Columns.Add("Notes");
        Assert.Equal(2, added.Index);
        Assert.All(list.Rows, r => Assert.Equal(3, r.Cells.Count));
        row.Set("Notes", "hello");
        Assert.Equal("hello", row["notes"]);
        Assert.True(row.Remove());
        Assert.Equal(-1, row.Index);
        return Task.CompletedTask;
    });

    [Fact]
    public void GridRendersCellsAndHeaderClicksToggleSorting() => RunStaAsync(async () =>
    {
        var list = new DataList(Dispatcher.CurrentDispatcher, "Name", "Value");
        list.Add("b", 10);
        list.Add("a", 9);
        list.Add("c", 100);
        list.Columns["Value"].Align = "Right";
        list.Columns["Value"].Format = "N1";

        var window = new Window { Content = list.ListView, Width = 400, Height = 300, ShowActivated = false, WindowStyle = WindowStyle.None };
        try
        {
            window.Show();
            window.UpdateLayout();

            var header = FindAll<GridViewColumnHeader>(list.ListView).First(h => h.Column == list.Columns["Value"].GridColumn);
            header.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, header));
            Assert.Equal("Value", list.SortColumn);
            Assert.False(list.SortDescending);
            Assert.Equal("Value", header.Content);
            header.UpdateLayout();
            Assert.Equal(Geometry.Parse("M 1,5 L 5,1 L 9,5").ToString(), Assert.Single(FindAll<System.Windows.Shapes.Path>(header)).Data.ToString());
            Assert.Equal(["a", "b", "c"], list.Rows.Select(r => r[0]));

            header.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, header));
            Assert.True(list.SortDescending);
            header.UpdateLayout();
            Assert.Equal(Geometry.Parse("M 1,1 L 5,5 L 9,1").ToString(), Assert.Single(FindAll<System.Windows.Shapes.Path>(header)).Data.ToString());
            Assert.Equal(["c", "b", "a"], list.Rows.Select(r => r[0]));

            window.UpdateLayout();
            var cells = CellText(list, 0);
            Assert.Equal(["c", "100.0"], cells.Select(t => t.Text));
            Assert.Equal(TextAlignment.Right, cells[1].TextAlignment);

            list[0]["Value"] = 1;
            await Dispatcher.Yield(DispatcherPriority.Background);
            window.UpdateLayout();
            Assert.Equal(["b", "10.0"], CellText(list, 0).Select(t => t.Text));

            list.ClearSort();
            header.UpdateLayout();
            Assert.Empty(FindAll<System.Windows.Shapes.Path>(header));
            Assert.Equal(["b", "a", "c"], list.Rows.Select(r => r[0]));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void ShowHostsTheListInAWindowThatCanBeReopened() => RunStaAsync(() =>
    {
        var list = new DataList(Dispatcher.CurrentDispatcher, "Name");
        list.Add("a");
        for (int i = 0; i < 2; i++)
        {
            list.Show("People", 300, 200);
            var window = Assert.IsType<DataListWindow>(Window.GetWindow(list.ListView));
            Assert.Equal("People", window.Title);
            Assert.True(window.IsVisible);
            list.Close();
            Assert.Null(Window.GetWindow(list.ListView));
        }

        return Task.CompletedTask;
    });

    private static List<TextBlock> CellText(DataList list, int index)
    {
        var container = (ListViewItem)list.ListView.ItemContainerGenerator.ContainerFromIndex(index);
        var presenter = FindAll<GridViewRowPresenter>(container).Single();
        return Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(presenter))
            .Select(i => FindAll<TextBlock>(VisualTreeHelper.GetChild(presenter, i)).First())
            .ToList();
    }

    private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in FindAll<T>(child)) yield return descendant;
        }
    }

    private static void RunStaAsync(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.InvokeAsync(async () =>
            {
                try { await action(); }
                catch (Exception ex) { failure = ex; }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal); }
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "STA test timed out.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public class Person
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Age { get; set; }
    }
}
