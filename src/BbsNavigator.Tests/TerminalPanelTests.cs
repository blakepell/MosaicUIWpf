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
using Mosaic.UI.Wpf.Scripting;
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
