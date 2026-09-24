# Mosaic scripting

`ScriptEditorControl` ports the ApexGate script document into a reusable, templated control built around Mosaic's `SyntaxEditor`. It has no ApexGateUI or Actipro dependency. The default template, JavaScript definitions, and copied toolbar/document icons are in this folder. Merge Mosaic's `ThemeManager` in your application as usual.

```xml
<mosaic:ScriptEditorControl
    xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
    Text="{Binding Script, Mode=TwoWay}"
    Environment="{Binding Scripting}"
    FilePath="{Binding ScriptPath, Mode=TwoWay}" />
```

Omit `Environment` for a separate, preconfigured Topaz engine per control. The control preserves the application's `DataContext`. `Text`, `Environment`, `FilePath`, `IsReadOnly`, and `ToolBarVisibility` are dependency properties; `IsModified`, `IsRunning`, and `LastError` are read-only dependency properties. `Editor` exposes the inner `SyntaxEditor` after template application. A replacement template should provide `PART_Editor` of that type.

## Application commands and types

```csharp
using Mosaic.UI.Wpf.Scripting;
using Mosaic.UI.Wpf.Scripting.ScriptCommands;

public ScriptEnvironment Scripting { get; } = new();

// Run these registrations on the editor's UI thread, before executing scripts.
Scripting.RegisterObject("app", new ApplicationCommands());
Scripting.RegisterObject("ui", new ApplicationUiCommands()); // replaces the default bridge
Scripting.RegisterType(typeof(Customer), "Customer");         // supports new Customer()

[ScriptModule(Name = "app", Description = "Application commands")]
public class ApplicationCommands
{
    [ScriptModuleMethod(Description = "Refreshes customer data.")]
    public void Refresh() { /* application code */ }

    [ScriptHidden]
    public void InternalHelper() { }
}

public class ApplicationUiCommands : UiScriptCommands
{
    public string ApplicationName => "My application";
}
```

`RegisterModule(instance)` uses `ScriptModuleAttribute.Name` (or the type name). `RegisterObject(alias, instance)` accepts any public object and includes inherited public members. `RegisterType(type, alias)` exposes static members and constructors. Reusing an alias replaces its completion metadata as well as its engine value. Registrations added after the control loads refresh highlighting immediately. Different environments do not share completion registrations or globals.

Completion offers modules with Ctrl+Space/Ctrl+period, members after `.`, constructible types after `new `, live dictionary/global keys, and F1 snippets. Method descriptions include overload signatures, parameters and return types. `ScriptModuleMethodAttribute` supplies descriptions and signature hints; `ScriptHiddenAttribute` hides editor entries. These attributes do not rename members or restrict engine access. Completion is reflection-based on registered aliases; it does not infer arbitrary JavaScript expression types.

## Existing engine

```csharp
var environment = new ScriptEnvironment(myTopazEngine);
// An existing engine is preserved, with no implicit default registration.
environment.RegisterCompletionType("app", typeof(ApplicationCommands));
editor.Environment = environment;

// Alternatively, explicitly install the standard bridges into your engine:
editor.Environment = new ScriptEnvironment(myTopazEngine, includeDefaults: true);
```

Values installed directly through Topaz cannot be discovered automatically. Describe those using `RegisterCompletionType`, or register them through the environment's combined registration methods. For a pre-existing globals dictionary, pass it as the `instance` argument to `RegisterCompletionType` to enable key completion. `environment.Globals` is the dictionary installed by the default setup.

Default aliases: `process`, `hash`, `clipboard`, `http`, `screenshot`, `mouse`, `environ`, `log`, `ai`, `regex`, `ui`, `string`, `int`, `date`, `file`, `directory`, `double`, `math`, `guid`, `StringBuilder`, `JSON`, `globalThis`, and `globals`. The original System namespace and Argus/LINQ extension registrations are also included. AI uses the copied Ollama helper and its original local endpoint/model defaults; it makes requests only when a script calls it. UI and clipboard commands dispatch to the WPF application thread. Screenshots return caller-owned `System.Drawing.Bitmap` objects.

## Dock document or tool window

The same control can be the content of either Mosaic AvalonDock layout item. Each visible item needs its own control instance; they may share a `ScriptEnvironment` if desired.

```xml
<ad:DockingManager
    xmlns:ad="https://github.com/blakepell/MosaicUIWpf"
    xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui">
    <ad:DockingManager.Theme>
        <ad:MosaicTheme />
    </ad:DockingManager.Theme>
    <ad:LayoutRoot>
        <ad:LayoutPanel>
            <ad:LayoutDocumentPane>
                <ad:LayoutDocument Title="Script" ContentId="script-document"
                    IconSource="/Mosaic.UI.Wpf;component/Scripting/Assets/javascript-48.png">
                    <mosaic:ScriptEditorControl Text="{Binding Script}" Environment="{Binding Scripting}" />
                </ad:LayoutDocument>
            </ad:LayoutDocumentPane>
            <ad:LayoutAnchorablePane>
                <ad:LayoutAnchorable Title="Script console" ContentId="script-tool" CanClose="True"
                    IconSource="/Mosaic.UI.Wpf;component/Scripting/Assets/javascript-48.png">
                    <mosaic:ScriptEditorControl Environment="{Binding Scripting}" />
                </ad:LayoutAnchorable>
            </ad:LayoutAnchorablePane>
        </ad:LayoutPanel>
    </ad:LayoutRoot>
</ad:DockingManager>
```

## Execution and saving

F5 runs; F6/Shift+F5 stops; Ctrl+S saves; Ctrl+G inserts a GUID. Mosaic supplies search, editing context menus, commenting, and its normal editor keyboard behavior. `RunCommand`, `StopCommand`, `SaveCommand`, `CompletionCommand`, and `SnippetsCommand` can also be bound to host menus.

`RunAsync()` executes a text snapshot in a fresh lexical block so `let`/`const` declarations can be rerun. Execution uses a worker thread to keep the UI responsive. WPF `DispatcherObject` instances registered with `RegisterObject` retain their identity, and their property access and method calls are dispatched automatically to their owning UI thread. For example, after `environment.RegisterObject("win", mainWindow)`, scripts can use `win.Left = 0` and `win.Title = "Test"` directly. This applies to registered objects; other application bridges and unregistered objects reached through their members must handle their own UI dispatching. The `Executing` and `Executed` routed events run on the UI thread; `Executed` fires after state is reset, including on cancellation or failure. Programmatic `RunAsync()` propagates errors; toolbar commands capture them in `LastError` and display the message inline. Cancellation is cooperative: a blocking custom .NET call must return before Topaz can stop. Unloading the control requests cancellation and closes completion popups. Environments wrapping the same engine serialize their runs; direct external engine calls must be coordinated by the host. The control never disposes a supplied engine.

`LoadAsync(path)` loads a file and establishes its saved baseline. `SaveAsync()` writes to `FilePath`, or asks for a path if none is set. To write the text straight onto a model property, set `SaveObject` and `SaveToProperty` (the name of a public, writable `string` property); saving then assigns the text via reflection instead of writing a file:

```xml
<scripting:ScriptEditorControl SaveObject="{Binding SelectedMacro}" SaveToProperty="Script" />
```

For custom save logic, set a callback, which takes precedence over both:

```csharp
editor.SaveTextAsync = async (text, token) =>
{
    model.Script = text;
    await SaveModelAsync(model, token);
};
```

Call `MarkSaved()` after initially loading text through a binding. If text changes while a save is pending, `IsModified` remains true for the newer text. Hosts control document titles, close confirmation, and how failures are presented outside the editor.
