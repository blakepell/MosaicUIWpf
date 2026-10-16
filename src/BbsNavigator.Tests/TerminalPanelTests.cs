/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using BbsNavigator.Views;
using Mosaic.UI.Wpf.AvalonDock;
using Mosaic.UI.Wpf.AvalonDock.Layout;
using Mosaic.UI.Wpf.Controls.Scripting;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using Xunit;

namespace BbsNavigator.Tests;

public class TerminalPanelTests
{
    [Fact]
    public void TextMethodsSetAddAndEditTheScreen() => RunSta(() =>
    {
        var panel = new TerminalPanel("stats");
        panel.Resize(5, 20);

        panel.SetText("Line one\nLine two");
        Assert.Equal("Line one\r\nLine two", panel.GetText());

        panel.AddLine();
        panel.AddText("Three");
        Assert.Equal("Three", panel.GetLine(3));

        panel.WriteAt(1, 6, "ONE");
        Assert.Equal("Line ONE", panel.GetLine(1));

        panel.SetLine(2, "Replaced");
        Assert.Equal("Replaced", panel.GetLine(2));

        panel.ClearLine(1);
        Assert.Equal(string.Empty, panel.GetLine(1));

        panel.SetColor(14, 1);
        panel.WriteAt(5, 1, "Colored");
        panel.ResetColor();
        Assert.Equal("Colored", panel.GetLine(5));
        Assert.Equal(string.Empty, panel.GetLine(99));

        panel.Clear();
        Assert.Equal(string.Empty, panel.GetText());
        Assert.Equal((5, 20), (panel.Rows, panel.Columns));
    });

    [Fact]
    public void WorkerThreadCallsAreMarshalledToTheUiThread() => RunSta(() =>
    {
        var panel = new TerminalPanel("worker");
        panel.Resize(3, 20);
        var frame = new DispatcherFrame();
        Exception? failure = null;
        string? read = null;
        Task.Run(() =>
        {
            try
            {
                panel.SetText("from a script");
                panel.IsEditable = true;
                read = panel.GetLine(1);
            }
            catch (Exception ex) { failure = ex; }
            finally { frame.Continue = false; }
        });
        Dispatcher.PushFrame(frame);
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
        Assert.Equal("from a script", read);
        Assert.True(panel.IsEditable);
    });

    [Fact]
    public void PanelHostsAsDocumentOrToolWindowAndReportsClosing() => RunSta(() =>
    {
        var documentPane = new LayoutDocumentPane();
        var dock = new DockingManager { Layout = new LayoutRoot { RootPanel = new LayoutPanel(documentPane) } };

        var document = new TerminalPanel("doc");
        var documentHost = dock.Add(document, "Document Panel");
        document.AttachHost(documentHost);
        Assert.Contains(documentHost, documentPane.Children);
        Assert.False(document.IsToolWindow);

        var tool = new TerminalPanel("tool");
        var toolHost = dock.AddToolWindow(tool, "Tool Panel", AnchorableShowStrategy.Right);
        tool.AttachHost(toolHost);
        Assert.True(tool.IsToolWindow);
        Assert.True(toolHost.CanClose);
        Assert.False(toolHost.CanHide);
        Assert.Same(toolHost, dock.Layout.Descendents().OfType<LayoutAnchorable>().Single());

        tool.Title = "Renamed";
        Assert.Equal("Renamed", toolHost.Title);

        bool closed = false;
        tool.Closed += (_, _) => closed = true;
        tool.Close();
        Assert.True(closed);
        Assert.Empty(dock.Layout.Descendents().OfType<LayoutAnchorable>());
        Assert.Equal(string.Empty, tool.Title);
    });

    [Fact]
    public void ScriptsRetrievePanelsByIdAcrossRunsAndEngines() => RunSta(() =>
    {
        var documentPane = new LayoutDocumentPane();
        var dock = new DockingManager { Layout = new LayoutRoot { RootPanel = new LayoutPanel(documentPane) } };
        var panels = new PanelScriptCommands(dock);

        var first = new ScriptEnvironment();
        first.RegisterObject("panels", panels);
        RunScript(first, """
            let p = panels.CreateTool('who', 'Who is Online');
            p.Resize(4, 30);
            p.SetText('Users online: 3');
            p.AddLine();
            p.AddText('Last caller: ');
            p.SetColor(11);
            p.AddLine('Sysop');
            p.SetColor(15, 4);
            p.WriteAt(4, 1, 'Footer');
            p.ResetColor();
            """);

        // A later run in the same engine and a brand new engine both find the panel by id.
        RunScript(first, "globals.found = panels.Exists('WHO') && panels.Get(' who ').GetLine(1);");
        Assert.Equal("Users online: 3", first.Globals["found"]);

        var second = new ScriptEnvironment();
        second.RegisterObject("panels", panels);
        RunScript(second, """
            let again = panels.CreateTool('who', 'Renamed');
            again.SetLine(3, 'Updated');
            globals.missing = panels.Get('nope') == null;
            globals.count = panels.Count;
            globals.firstId = panels.GetIds()[0];
            """);
        Assert.Equal(true, second.Globals["missing"]);
        Assert.Equal(1, Convert.ToInt32(second.Globals["count"]));
        Assert.Equal("who", second.Globals["firstId"]);

        var panel = panels.Get("who")!;
        Assert.True(panel.IsToolWindow);
        Assert.Equal("Renamed", panel.Title);
        Assert.Equal("Users online: 3\r\nLast caller: Sysop\r\nUpdated\r\nFooter", panel.GetText());

        Assert.True(panels.Close("who"));
        Assert.False(panels.Exists("who"));
        Assert.False(panels.Close("who"));
        Assert.Empty(dock.Layout.Descendents().OfType<LayoutAnchorable>());
    });

    [Fact]
    public void RepeatedScriptsReplaceSecondLineOnFixedGrid() => RunSta(() =>
    {
        var dock = new DockingManager
        {
            Layout = new LayoutRoot { RootPanel = new LayoutPanel(new LayoutDocumentPane()) }
        };
        var panels = new PanelScriptCommands(dock);
        var environment = new ScriptEnvironment();
        environment.RegisterObject("panels", panels);

        for (int run = 0; run < 100; run++)
        {
            RunScript(environment, """
                if (!panels.Exists('who')) {
                    let p = panels.CreateTool('who', "Who's Online");
                    p.Resize(24, 80);
                    p.SetText('Users online: 3');
                }
                let p2 = panels.Get('who');
                if (p2 != null) {
                    p2.SetLine(2, 'Last caller: Sysop');
                }
                """);

            var panel = panels.Get("who")!;
            Assert.Equal("Users online: 3", panel.GetLine(1));
            Assert.Equal("Last caller: Sysop", panel.GetLine(2));
            panel.ClearLine(2);
            if (run % 2 == 0)
            {
                panels.Close("who");
            }
        }
    });

    [Fact]
    public void ScriptsAppendToNewPanelsDuringDockLayout() => RunSta(() =>
    {
        var dock = new DockingManager
        {
            Theme = new Mosaic.UI.Wpf.AvalonDock.Themes.MosaicTheme(),
            Layout = new LayoutRoot { RootPanel = new LayoutPanel { Children = { new LayoutDocumentPane(),
                new LayoutAnchorablePane(new LayoutAnchorable
                {
                    Title = "Existing tool", Content = new System.Windows.Controls.TextBlock { Text = "Tool" }
                }) { DockWidth = new System.Windows.GridLength(420), DockMinWidth = 300 } } } }
        };
        var window = new System.Windows.Window
        {
            Content = dock, Width = 1000, Height = 700,
            Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false
        };
        try
        {
            window.Resources.MergedDictionaries.Add(new Mosaic.UI.Wpf.Themes.ThemeManager
            {
                Theme = Mosaic.UI.Wpf.MosaicThemeMode.Blue, Native = true, SystemColors = true
            });
            dock.Layout.Descendents().OfType<LayoutAnchorable>().Single().ToggleAutoHide();
            dock.Add(new System.Windows.Controls.ContentControl { Content = "Script editor" }, "Script editor");
            window.Show();
            var panels = new PanelScriptCommands(dock);
            var environment = new ScriptEnvironment();
            environment.RegisterObject("panels", panels);
            for (int run = 0; run < 30; run++)
            {
                RunScript(environment, """
                    if (!panels.Exists('who')) {
                        let p = panels.CreateTool('who', "Who's Online");
                        p.SetText('Users online: 3');
                    }
                    let p2 = panels.Get('who');
                    if (p2 != null) {
                        p2.AddLine();
                        p2.AddText('Last caller: Sysop');
                    }
                    """);
                DrainLayout();
                var panel = panels.Get("who")!;
                Assert.True(panel.GetLine(1) == "Users online: 3",
                    $"Run {run}: grid={panel.Rows}x{panel.Columns}, panel={panel.ActualWidth}x{panel.ActualHeight}, view={panel.Terminal.TextArea.TextView.ActualWidth}x{panel.Terminal.TextArea.TextView.ActualHeight}, text={panel.GetText()}");
                Assert.Equal("Last caller: Sysop", panel.GetLine(2));
                Assert.True(panel.Terminal.VerticalOffset < panel.Terminal.TextArea.TextView.DefaultLineHeight,
                    $"Run {run}: vertical offset={panel.Terminal.VerticalOffset}, grid={panel.Rows}x{panel.Columns}");
                panels.Close("who");
                DrainLayout();
            }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void ScriptsDockDataListsBesideTheTerminalWithoutTakingFocus() => RunSta(() =>
    {
        var existingTool = new LayoutAnchorable { Title = "Existing tool", Content = new System.Windows.Controls.TextBlock() };
        var dock = new DockingManager
        {
            Layout = new LayoutRoot { RootPanel = new LayoutPanel { Children = { new LayoutDocumentPane(), new LayoutAnchorablePane(existingTool) } } }
        };
        var panels = new PanelScriptCommands(dock);
        var environment = new ScriptEnvironment();
        environment.RegisterObject("panels", panels);

        // Without an Application a DataList must be built on the UI thread; in the app scripts can call new DataList().
        var grid = new Mosaic.UI.Wpf.Controls.Scripting.DataList.DataList("Unit", "Count");
        environment.Globals["grid"] = grid;

        // A trigger runs this every time troop counts scroll by; only the first run docks a panel.
        for (int run = 1; run <= 3; run++)
        {
            RunScript(environment, $$"""
                let troops = panels.GetContent('troops') ?? panels.Dock('troops', 'Troops', globals.grid);
                troops.Clear();
                troops.Add('Fighters', {{run * 10}});
                troops.Add('Shields', {{run * 5}});
                """);
        }

        Assert.Equal(2, grid.Count);
        Assert.Equal(30, Convert.ToInt32(grid[0][1]));
        Assert.Same(grid, panels.GetContent("troops"));
        Assert.Null(panels.Get("troops"));

        // The panel joins the existing right-hand pane as a selected tab, but is not activated.
        var host = dock.Layout.Descendents().OfType<LayoutAnchorable>().Single(a => a.ContentId == "panel:troops");
        Assert.Same(existingTool.Parent, host.Parent);
        Assert.True(host.IsSelected);
        Assert.False(host.IsActive);
        Assert.Equal("Troops", host.Title);
        Assert.Same(host.Content, grid.ListView.Parent);

        Assert.True(panels.Show("TROOPS"));
        Assert.True(host.IsActive);
        Assert.False(panels.Show("nope"));

        // Another side, and re-docking an id with new content swaps it in place.
        RunScript(environment, "panels.Dock('log', 'Log', globals.grid, 'bottom');");
        var log = dock.Layout.Descendents().OfType<LayoutAnchorable>().Single(a => a.ContentId == "panel:log");
        Assert.Equal(AnchorSide.Bottom, ((LayoutAnchorablePane)log.Parent).GetSide());
        Assert.Same(log.Content, grid.ListView.Parent);
        Assert.Null(host.Content is System.Windows.Controls.Border border ? border.Child : null);

        var replacement = new Mosaic.UI.Wpf.Controls.Scripting.DataList.DataList("Name");
        panels.Dock("log", "Renamed", replacement);
        Assert.Same(log.Content, replacement.ListView.Parent);
        Assert.Equal("Renamed", log.Title);
        Assert.Same(replacement, panels.GetContent("log"));

        Assert.Throws<ArgumentException>(() => panels.Dock("bad", "Bad", grid, "middle"));
        Assert.Throws<ArgumentException>(() => panels.Dock("bad", "Bad", "not a control"));
        Assert.Throws<InvalidOperationException>(() => panels.CreateTool("log", "Terminal"));

        panels.CloseAll();
        Assert.Equal(0, panels.Count);
        Assert.Same(existingTool, dock.Layout.Descendents().OfType<LayoutAnchorable>().Single());
    });

    [Fact]
    public void DockingManagerFindsShowsAndReusesContentById() => RunSta(() =>
    {
        var dock = new DockingManager { Layout = new LayoutRoot { RootPanel = new LayoutPanel(new LayoutDocumentPane()) } };
        int built = 0;
        System.Windows.FrameworkElement Build() { built++; return new System.Windows.Controls.TextBlock(); }

        var tool = dock.GetOrAddToolWindow("stats", Build, "Stats", AnchorableShowStrategy.Left, activate: false, canHide: true);
        Assert.Same(tool, dock.GetOrAddToolWindow("stats", Build, "Ignored"));
        Assert.Equal(1, built);
        Assert.Equal("Stats", tool.Title);
        Assert.Equal(AnchorSide.Left, ((LayoutAnchorablePane)tool.Parent).GetSide());

        tool.Hide();
        Assert.True(tool.IsHidden);
        Assert.Same(tool, dock.FindContent("stats"));
        Assert.True(dock.ShowContent("stats"));
        Assert.False(tool.IsHidden);
        Assert.True(tool.IsActive);
        Assert.False(dock.ShowContent("missing"));

        var document = dock.GetOrAdd("notes", Build, "Notes");
        Assert.Same(document, dock.GetOrAdd("notes", Build, "Notes"));
        Assert.Equal(2, built);
        Assert.Throws<InvalidOperationException>(() => dock.GetOrAdd("stats", Build, "Stats"));
        Assert.Throws<InvalidOperationException>(() => dock.GetOrAddToolWindow("notes", Build, "Notes"));
    });

    private static void DrainLayout()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static void RunScript(ScriptEnvironment environment, string code)
    {
        var frame = new DispatcherFrame();
        Task run = environment.ExecuteAsync(code);
        run.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        run.GetAwaiter().GetResult();
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The STA test did not finish.");
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}

