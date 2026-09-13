/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Scripting;
using Mosaic.UI.Wpf.Scripting.ScriptCommands;
using Mosaic.UI.Wpf.Themes;
using Tenray.Topaz;
using Xunit;

namespace Mosaic.UI.Wpf.Tests;

public class ScriptingTests
{
    [Fact]
    public async Task DefaultsAndCustomBridgesExecuteRepeatedlyWithoutCrossEnvironmentPollution()
    {
        var environment = new ScriptEnvironment();
        Assert.All(new[] { "process", "hash", "clipboard", "http", "screenshot", "mouse", "environ", "log", "ai", "regex", "ui", "JSON", "StringBuilder", "globals" },
            alias => Assert.Contains(alias, environment.Registrations.Keys));
        var bridge = new AppCommands();
        environment.RegisterObject("app", bridge);
        environment.RegisterType(typeof(AppValue));
        const string code = "let value = new AppValue(); app.Name = hash.DecodeBase64(hash.EncodeBase64(value.Name)); app.Add(2);";
        await environment.ExecuteAsync(code);
        await environment.ExecuteAsync(code);
        Assert.Equal("Example", bridge.Name);
        Assert.Equal(4, bridge.Total);
        Assert.DoesNotContain("app", new ScriptEnvironment().Registrations.Keys);
    }

    [Fact]
    public async Task SuppliedEngineRetainsValuesAndAllowsExplicitDefaultSetup()
    {
        var engine = new TopazEngine();
        var bridge = new AppCommands();
        engine.SetValue("app", bridge);
        var environment = new ScriptEnvironment(engine);
        Assert.Same(engine, environment.Engine);
        Assert.Empty(environment.Registrations);
        environment.RegisterCompletionType("app", typeof(AppCommands));
        await environment.ExecuteAsync("app.Add(3);");
        Assert.Equal(3, bridge.Total);
        Assert.Contains("hash", new ScriptEnvironment(engine, includeDefaults: true).Registrations.Keys);
    }

    [Fact]
    public void CompletionIncludesOverloadsPropertiesInheritedMembersGlobalsAndConstructors()
    {
        var environment = new ScriptEnvironment();
        environment.RegisterObject("app", new AppCommands());
        environment.RegisterType(typeof(AppValue));
        var members = ScriptCompletion.GetMembers(environment, "app");
        var add = Assert.Single(members, m => m.Text == "Add");
        Assert.Contains("Int32 amount", add.Description.ToString());
        Assert.Contains("String amount", add.Description.ToString());
        Assert.Contains("Adds to the total", add.Description.ToString());
        Assert.Contains(members, m => m.Text == "Name");
        Assert.Contains(members, m => m.Text == "PauseAsync");
        Assert.DoesNotContain(members, m => m.Text is "Secret" or "GetType" or "get_Name");
        Assert.Contains(ScriptCompletion.GetModules(environment, true), m => m.Text == "AppValue");
        environment.Globals["answer"] = 42;
        Assert.Contains(ScriptCompletion.GetMembers(environment, "globals"), m => m.Text == "answer" && m.Description.ToString()!.Contains("42"));
        environment.RegisterObject("app", new HashScriptCommands());
        Assert.DoesNotContain(ScriptCompletion.GetMembers(environment, "app"), m => m.Text == "Add");
    }

    [Fact]
    public async Task CancellationInterruptsCpuScriptAndLeavesEngineReusable()
    {
        var environment = new ScriptEnvironment();
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => environment.ExecuteAsync("while (true) {}", source.Token).WaitAsync(TimeSpan.FromSeconds(5)));
        await environment.ExecuteAsync("let value = 1;");
    }

    [Theory]
    [InlineData(MosaicThemeMode.Light)]
    [InlineData(MosaicThemeMode.Dark)]
    [InlineData(MosaicThemeMode.HighContrast)]
    public void TemplateUsesMosaicEditorAndUpdatesHighlighting(MosaicThemeMode theme) => RunStaAsync(() =>
    {
        var control = Realize(new ScriptEditorControl { Text = "app.Add(2);" }, theme);
        Assert.IsType<SyntaxEditor>(control.Editor);
        var baseline = control.Editor!.SyntaxHighlighting;
        control.Environment!.RegisterObject("app", new AppCommands());
        Assert.NotSame(baseline, control.Editor.SyntaxHighlighting);
        Assert.Contains(control.Editor.SyntaxHighlighting.MainRuleSet.Rules, r => r.Regex.IsMatch("app"));
        Assert.DoesNotContain(control.Editor.SyntaxHighlighting.MainRuleSet.Rules, r => r.Regex.IsMatch("appExtended"));
        control.Editor.Theme = theme == MosaicThemeMode.Light ? MosaicThemeMode.Dark : MosaicThemeMode.Light;
        Assert.Contains(control.Editor.SyntaxHighlighting.MainRuleSet.Rules, r => r.Regex.IsMatch("app"));
        Assert.Equal("app.Add(2);", control.Text);
        return Task.CompletedTask;
    });

    [Fact]
    public void BindingSaveSnapshotAndExecutionLifecycleRemainUsable() => RunStaAsync(async () =>
    {
        var model = new TextBox { Text = "let value = 1;" };
        var control = Realize(new ScriptEditorControl { DataContext = model }, MosaicThemeMode.Dark);
        control.SetBinding(ScriptEditorControl.TextProperty, new Binding("Text") { Source = model, Mode = BindingMode.TwoWay });
        control.MarkSaved();
        Assert.False(control.IsModified);
        control.Editor!.Text = "let value = 2;";
        Assert.Equal(control.Text, model.Text);
        Assert.Same(model, control.DataContext);
        var saving = new TaskCompletionSource();
        control.SaveTextAsync = (_, _) => saving.Task;
        var save = control.SaveAsync();
        control.Text = "let value = 3;";
        saving.SetResult();
        await save;
        Assert.True(control.IsModified);
        int executed = 0;
        control.Executed += (_, _) => { Assert.False(control.IsRunning); executed++; };
        await control.RunAsync();
        Assert.Equal(1, executed);
        control.Text = "throw new Error('test');";
        await Assert.ThrowsAnyAsync<Exception>(() => control.RunAsync());
        Assert.NotNull(control.LastError);
        Assert.False(control.IsRunning);
        control.Text = "let value = 4;";
        await control.RunAsync();
        Assert.Null(control.LastError);
        Assert.Equal(3, executed);
    });

    [Fact]
    public void MethodCompletionInsertsSingleParenthesesAndPositionsCaret() => RunStaAsync(() =>
    {
        var environment = new ScriptEnvironment();
        environment.RegisterObject("app", new AppCommands());
        ICompletionData method = ScriptCompletion.GetMembers(environment, "app").Single(m => m.Text == "Add");
        var editor = new SyntaxEditor { Text = "app.Ad" };
        method.Complete(editor.TextArea, new TextSegment { StartOffset = 4, Length = 2 }, EventArgs.Empty);
        Assert.Equal("app.Add()", editor.Text);
        Assert.Equal(8, editor.CaretOffset);
        editor.Text = "app.Ad()";
        method.Complete(editor.TextArea, new TextSegment { StartOffset = 4, Length = 2 }, EventArgs.Empty);
        Assert.Equal("app.Add()", editor.Text);
        return Task.CompletedTask;
    });

    [Fact]
    public void CompletionWindowResourcesLoadAndTemplateTheList() => RunStaAsync(() =>
    {
        var resources = new ResourceDictionary { Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Scripting/ScriptCompletionWindow.xaml", UriKind.Absolute) };
        var list = new CompletionList { Style = Assert.IsType<Style>(resources["ScriptCompletionListStyle"]) };
        Assert.True(list.ApplyTemplate());
        Assert.NotNull(list.ListBox);
        Assert.IsType<Style>(resources[typeof(ToolTip)]);
        Assert.NotNull(Assert.IsType<DataTemplate>(resources["ScriptCompletionItemTemplate"]).LoadContent());
        Assert.NotNull(Assert.IsType<DataTemplate>(resources["ScriptCompletionDescriptionTemplate"]).LoadContent());
        return Task.CompletedTask;
    });

    [Fact]
    public void RetemplatingDetachesTheOldEditorAndUnloadingCancelsExecution() => RunStaAsync(async () =>
    {
        var control = Realize(new ScriptEditorControl { Text = "let count = 0;" }, MosaicThemeMode.Dark);
        var oldEditor = control.Editor!;
        var factory = new FrameworkElementFactory(typeof(SyntaxEditor), "PART_Editor");
        control.Template = new ControlTemplate(typeof(ScriptEditorControl)) { VisualTree = factory };
        control.ApplyTemplate();
        Assert.NotSame(oldEditor, control.Editor);
        oldEditor.Text = "old editor";
        Assert.Equal("let count = 0;", control.Text);
        control.Text = "while (true) {}";
        var run = control.RunAsync();
        Assert.True(control.IsRunning);
        control.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        await run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(control.IsRunning);
        control.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        control.Text = "let count = 1;";
        await control.RunAsync();
    });

    [Fact]
    public void CanonicalXamlHostsTheControlAsBothDockDocumentAndToolWindow() => RunStaAsync(() =>
    {
        var manager = (Mosaic.UI.Wpf.AvalonDock.DockingManager)System.Windows.Markup.XamlReader.Parse("""
            <ad:DockingManager xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:ad="https://github.com/blakepell/MosaicUIWpf"
                xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui">
                <ad:DockingManager.Theme><ad:MosaicTheme /></ad:DockingManager.Theme>
                <ad:LayoutRoot><ad:LayoutPanel>
                    <ad:LayoutDocumentPane><ad:LayoutDocument Title="Script" ContentId="script-document">
                        <mosaic:ScriptEditorControl Text="let value = 1;" />
                    </ad:LayoutDocument></ad:LayoutDocumentPane>
                    <ad:LayoutAnchorablePane><ad:LayoutAnchorable Title="Script console" ContentId="script-tool">
                        <mosaic:ScriptEditorControl Text="let value = 2;" />
                    </ad:LayoutAnchorable></ad:LayoutAnchorablePane>
                </ad:LayoutPanel></ad:LayoutRoot>
            </ad:DockingManager>
            """);
        var documentPane = (Mosaic.UI.Wpf.AvalonDock.Layout.LayoutDocumentPane)manager.Layout.RootPanel.Children[0];
        var toolPane = (Mosaic.UI.Wpf.AvalonDock.Layout.LayoutAnchorablePane)manager.Layout.RootPanel.Children[1];
        Assert.IsType<ScriptEditorControl>(documentPane.Children[0].Content);
        Assert.IsType<ScriptEditorControl>(toolPane.Children[0].Content);
        return Task.CompletedTask;
    });

    private static ScriptEditorControl Realize(ScriptEditorControl control, MosaicThemeMode theme)
    {
        control.Resources.MergedDictionaries.Add(new ThemeManager { Theme = theme });
        var dictionary = new ResourceDictionary { Source = new Uri("/Mosaic.UI.Wpf;component/Scripting/ScriptEditorControl.xaml", UriKind.Relative) };
        control.Style = (Style)dictionary[typeof(ScriptEditorControl)];
        control.Measure(new Size(800, 500));
        control.Arrange(new Rect(0, 0, 800, 500));
        control.ApplyTemplate();
        control.Editor!.FollowGlobalTheme = false;
        control.Editor.Theme = theme;
        return control;
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

    public class AppValue { public string Name => "Example"; }

    [ScriptModule(Name = "app")]
    public class AppCommands : UiScriptCommands
    {
        public int Total { get; private set; }
        public string Name { get; set; } = "";
        [ScriptHidden] public void Secret() { }
        [ScriptModuleMethod(Description = "Adds to the total")]
        public void Add(int amount) => Total += amount;
        public void Add(string amount) => Total += int.Parse(amount);
    }
}
