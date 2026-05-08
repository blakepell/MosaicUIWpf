# Copilot Instructions for MosaicUIWpf

## Build, test, and lint commands

```bash
# Build the full solution
dotnet build MosaicUIWpf.sln --nologo

# Build the main library or the demo app only
dotnet build src\Mosaic.UI.Wpf\Mosaic.UI.Wpf.csproj -c Debug --nologo
dotnet build src\MosaicWpfDemo\MosaicWpfDemo.csproj -c Debug --nologo

# Run the demo app
dotnet run --project src\MosaicWpfDemo\MosaicWpfDemo.csproj

# Pack the NuGet package for the control library
dotnet pack src\Mosaic.UI.Wpf\Mosaic.UI.Wpf.csproj -c Release --nologo
```

There is no automated test project in this repository today, so there is no `dotnet test` command or single-test command to run. There is also no dedicated lint or formatting command checked into the repo.

## High-level architecture

- `MosaicUIWpf.sln` contains one reusable control library (`src\Mosaic.UI.Wpf`) plus three WPF applications that consume it: `MosaicWpfDemo`, `MosaicTemplateApp`, and `LanChat`.
- `src\Mosaic.UI.Wpf` is the single runtime control assembly. Public XAML consumers should use the canonical URI `http://schemas.apexgate.net/wpf/mosaic-ui`; legacy Mosaic URIs are still mapped in `AssemblyInfo.cs` and must stay compatible when public namespaces are added.
- App startup is standardized around `MosaicApp<TSettings, TApplicationViewModel>`. Consumer apps use `<wpf:MosaicApp>` in `App.xaml`, merge a `ThemeManager` into application resources, and rely on `AppServices` for DI plus persisted `AppSettings.json` / `LocalSettings.json`.
- The theming system is centered on `ThemeManager`. It updates only its own `MergedDictionaries`, not `Application.Current.Resources`, so Mosaic can coexist with other UI kits. Dictionary composition order is typography tokens, optional system colors, theme colors, window chrome resources, generic control templates, then optional native WPF styles.
- `Themes\Generic.xaml` is the default template entry point for the library. Individual control dictionaries live under `Controls\<ControlName>\`.
- The demo app is wired around `MainWindow.xaml` side menu entries that navigate to `ShellView`. Each menu item passes `Title`, `XamlFile`, `CodeFile`, `DocumentationType`, `ExampleType`, and `ImageSource`. `ShellView` uses those parameters to instantiate the example control and load the embedded XAML/C# source snippets shown alongside the live example.

## Key conventions

- Prefer `CustomControl` for reusable library controls. Typical control folders contain the control code plus a XAML resource dictionary, and the control registers its default style with `DefaultStyleKeyProperty.OverrideMetadata`.
- Interactive custom controls are expected to declare `TemplatePart` contracts and expose an automation peer. Existing controls such as `AutoCompleteBox`, `ToggleSwitch`, and `SplitButton` are the reference pattern.
- Use `DynamicResource` tokens from `MosaicTheme` instead of hard-coded colors or fonts in control templates.
- Do not make theme changes by editing global application resources. `ThemeManager` must remain self-contained and swap only the dictionaries it owns.
- When adding public namespaces for XAML use, update `AssemblyInfo.cs` so the canonical Mosaic URI and both legacy URIs continue to resolve correctly.
- New `.cs` files should keep the existing Mosaic file header block (`Mosaic UI for WPF`, project lead, websites, copyright, MIT).
- Demo examples live in `src\MosaicWpfDemo\Views\Examples\{Name}Example.xaml(.cs)`. Both files must also be added to `src\MosaicWpfDemo\MosaicWpfDemo.csproj` as `EmbeddedResource` items linked under `LinkedSources\...`, because the running demo reads those embedded resources back to display source code.
- Demo menu entries in `src\MosaicWpfDemo\MainWindow.xaml` must use dotted resource names such as `MosaicWpfDemo.LinkedSources.{Name}Example.xaml` for `XamlFile` and `CodeFile`, plus a matching `DocumentationType` and `ExampleType`. When the task is “add a new demo/example”, use the repository skill `add-demo-example`.
