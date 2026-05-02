# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

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

There is no test project in the solution.

## Architecture

### Library shape (`src/Mosaic.UI.Wpf`)

- One runtime assembly with a stable root namespace `Mosaic.UI.Wpf.*`. Public XAML consumers use the canonical URI `http://schemas.apexgate.net/wpf/mosaic-ui` (prefix `mosaic`). Legacy URIs (`...mosaic`, `mosaicgate.net/...`) are mapped via `XmlnsCompatibleWith` in `AssemblyInfo.cs` — preserve those when adding new namespaces.
- Controls live under `Controls/<ControlName>/` (one folder per control, often containing both code and a XAML resource dictionary). Default templates are reachable via `Themes/Generic.xaml`.
- Other top-level folders: `Behaviors`, `Converters`, `Extensions`, `Interfaces`, `Collections`, `Cache`, `Json`, `Common`, `Assets`.

### Theming model (`src/Mosaic.UI.Wpf/Themes`)

`ThemeManager : ResourceDictionary` is the entry point. It mutates only its own `MergedDictionaries` and never `Application.Current.Resources` — this is a hard rule from `docs/ARCHITECTURE_BLUEPRINT.md` so the library can coexist with other UI kits. Composition order: foundation tokens → theme colors (`Light`/`Dark`/`HighContrast`) → optional system color overrides → generic control templates → optional native control styles. Theme switches replace managed dictionaries; brushes are referenced via `DynamicResource` against tokenized keys (`MosaicTheme.FontFamily`, accent/status brushes, etc.). Native restyling is opt-in (`Native=true`); the native style XAMLs in `Themes/Native/` are deliberately `<Page Remove>`d from the build (see csproj) and merged dynamically.

When adding a control: prefer `CustomControl` (lookless, themable, `TemplatePart` contracts, automation peer) over `UserControl`. Use tokens, not hard-coded colors. Add an automation peer for interactive controls and keep keyboard parity with mouse.

### Demo app (`src/MosaicWpfDemo`)

Each control example is a `UserControl` under `Views/Examples/{Name}Example.xaml(.cs)`. The example XAML and code-behind are also added as `<EmbeddedResource>` items with `<Link>LinkedSources\...</Link>` so the running app can display the source code alongside the live control. Side menu entries in `MainWindow.xaml` use `<mosaic:SideMenuItem>` with a `SideMenuParameterCollection` that wires `XamlFile` / `CodeFile` (resource names — dotted form like `MosaicWpfDemo.LinkedSources.{Name}Example.xaml`), `DocumentationType` (`{x:Type mosaic:Foo}` — drives auto-generated XML doc display), and `ExampleType`. **Use the `add-demo-example` skill** when adding a new example — it knows the full four-step wiring (file scaffolding, csproj LinkedSources, MainWindow side menu entry, build verify).

## File header

Source files use a fixed header block (`Mosaic UI for WPF / @project lead Blake Pell / @license MIT`). Match the existing style when creating new `.cs` files.

## Reference docs

- `README.md` — control inventory and screenshots.
- `docs/ARCHITECTURE_BLUEPRINT.md` — the authoritative spec for assembly shape, XAML namespace rules, theming composition order, control-design choices (CustomControl vs UserControl), accessibility baseline, and performance guardrails. Read this before non-trivial library changes.
