# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

Mosaic UI for WPF is a WPF control library (`Mosaic.UI.Wpf`) plus a demo app (`MosaicWpfDemo`) and a couple of consumer/template apps (`MosaicTemplateApp`, `LanChat`). Targets `net10.0-windows`, `Nullable=enable`, `UseWPF=true`. Solution: `MosaicUIWpf.sln` at the repo root.

## Common Commands

```bash
# Build everything
dotnet build MosaicUIWpf.sln

# Build just the library or demo
dotnet build src/Mosaic.UI.Wpf/Mosaic.UI.Wpf.csproj -c Debug
dotnet build src/MosaicWpfDemo/MosaicWpfDemo.csproj -c Debug

# Run the demo (browse/try every control)
dotnet run --project src/MosaicWpfDemo/MosaicWpfDemo.csproj

# Pack the NuGet (GeneratePackageOnBuild is on; this also produces the .nupkg)
dotnet pack src/Mosaic.UI.Wpf/Mosaic.UI.Wpf.csproj -c Release
```

Tests live in `src/Mosaic.UI.Wpf.Tests` (xunit; WPF control tests run on manually-created STA threads). Run with `dotnet test src/Mosaic.UI.Wpf.Tests/Mosaic.UI.Wpf.Tests.csproj`.

## Architecture

### Library shape (`src/Mosaic.UI.Wpf`)

- One runtime assembly with a stable root namespace `Mosaic.UI.Wpf.*`. Public XAML consumers use the canonical URI `http://schemas.apexgate.net/wpf/mosaic-ui` (prefix `mosaic`). Legacy URIs (`...mosaic`, `mosaicgate.net/...`) are mapped via `XmlnsCompatibleWith` in `AssemblyInfo.cs` — preserve those when adding new namespaces.
- Controls live under `Controls/<ControlName>/`. Each folder contains exactly one `.cs` (the control class) and one `.xaml` (a `ResourceDictionary` with the default `Style` + `ControlTemplate`), plus optional helpers such as an `AutomationPeer` or `TemplateSelector`.
- `Themes/Generic.xaml` is the WPF default-style index; it merges every individual control XAML via `<ResourceDictionary Source="/Mosaic.UI.Wpf;component/Controls/{Control}/{Control}.xaml" />`. Add a new entry here whenever a new control XAML is created.
- Other top-level folders: `Behaviors`, `Converters`, `Extensions`, `Interfaces`, `Collections`, `Cache`, `Json`, `Common`, `Assets`.

### Control authoring conventions

Prefer `CustomControl` (lookless, themable, `TemplatePart` contracts, automation peer) over `UserControl`. Key patterns to follow:

- Declare template-part names as private `const string` fields prefixed with `PART_`, and annotate the class with `[TemplatePart(Name = PartFoo, Type = typeof(...))]`.
- Register all `DependencyProperty` fields with `FrameworkPropertyMetadata`; set `BindsTwoWayByDefault` where the property represents user-editable state.
- Decorate interactive DPs with `[Category]`, `[Description]`, `[DefaultEvent]`, and `[DefaultProperty]` for designer support.
- Prefer routed events (`RoutingStrategy.Bubble`) for control-state notifications and provide `Command`/`CommandParameter` hooks for MVVM use.
- Add an automation peer for every interactive control and keep keyboard parity with mouse interaction.
- Use tokens, not hard-coded colors or sizes (see Theming model below).

### Theming model (`src/Mosaic.UI.Wpf/Themes`)

`ThemeManager : ResourceDictionary` is the entry point. It mutates only its own `MergedDictionaries` and never `Application.Current.Resources` — this is a hard rule from `docs/ARCHITECTURE_BLUEPRINT.md` so the library can coexist with other UI kits. Composition order: `Foundation/` tokens (typography, window chrome) → theme colors (`Light`/`Dark`/`HighContrast`) → optional system color overrides → generic control templates → optional native control styles. Theme switches replace managed dictionaries; brushes are referenced via `DynamicResource` against tokenized keys.

**Theme token keys** are exposed as `static ComponentResourceKey` properties on `Mosaic.UI.Wpf.Themes.MosaicTheme`. Reference them in XAML like:
```xml
Background="{DynamicResource {x:Static themes:MosaicTheme.ControlBackgroundBrush}}"
```
Categories include: accents, selection, status (Info/Success/Warning/Error), window, control background/foreground/border, and hover/disabled variants. Check `MosaicTheme.cs` for the full list.

Native WPF control restyling is opt-in (`Native=true`). The native style XAMLs in `Themes/Native/` are deliberately `<Page Remove>`d from the build (so they impose nothing by default) and merged dynamically at runtime when the option is enabled.

### Consumer entry point

`MosaicApp<TSettings, TApplicationViewModel> : Application` (in `MosaicApp.cs`) is the recommended base class for Mosaic-themed WPF apps — it handles `ThemeManager` initialization and wires `AppSettings`/`AppViewModel`. `MosaicTemplateApp` is the canonical example.

### Demo app (`src/MosaicWpfDemo`)

Each control example is a `UserControl` under `Views/Examples/{Name}Example.xaml(.cs)`. The example XAML and code-behind are also added as `<EmbeddedResource>` items with `<Link>LinkedSources\...</Link>` so the running app can display the source code alongside the live control. Side menu entries in `MainWindow.xaml` use `<mosaic:SideMenuItem>` with a `SideMenuParameterCollection` that wires `XamlFile` / `CodeFile` (resource names — dotted form like `MosaicWpfDemo.LinkedSources.{Name}Example.xaml`), `DocumentationType` (`{x:Type mosaic:Foo}` — drives auto-generated XML doc display), and `ExampleType`. **Use the `add-demo-example` skill** when adding a new example — it knows the full four-step wiring (file scaffolding, csproj LinkedSources, MainWindow side menu entry, build verify).

## File header

Every `.cs` file starts with this block — match it exactly when creating new files:

```csharp
/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */
```

## Reference docs

- `README.md` — control inventory and screenshots.
- `docs/ARCHITECTURE_BLUEPRINT.md` — the authoritative spec for assembly shape, XAML namespace rules, theming composition order, control-design choices (CustomControl vs UserControl), accessibility baseline, and performance guardrails. Read this before non-trivial library changes.
