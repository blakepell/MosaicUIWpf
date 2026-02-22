# Mosaic UI WPF Architecture Blueprint

This document defines the baseline architecture for building enterprise-grade, embeddable WPF controls and themes.

## 1) Architecture and Interoperability

### Assembly and package shape
- Use a single control assembly for runtime (`Mosaic.UI.Wpf`) and keep demos/samples in separate apps.
- Keep a stable root CLR namespace (`Mosaic.UI.Wpf.*`); do not expose internal helper namespaces in public XAML mappings.
- Version with SemVer for package compatibility guidance; reserve assembly version jumps for binary breaks.
- Keep control templates and theme tokens in the source assembly; avoid app-level assumptions.

### XAML namespace mappings
- Define one canonical URI for public consumption: `http://schemas.apexgate.net/wpf/mosaic-ui`.
- Add explicit `XmlnsDefinition` entries for public namespaces (`Controls`, `Themes`, `Converters`, `Behaviors`, `Extensions`).
- Add `XmlnsCompatibleWith` and legacy `XmlnsDefinition` mappings for old URIs to preserve existing consumer XAML.

### Resource-scope isolation (playing nice)
- Theme dictionaries must mutate only their own `MergedDictionaries`; never remove items from `Application.Current.Resources`.
- Keep opt-in native style overrides behind explicit configuration (`Native=true`).
- Prefer keyed styles for shared typography/utility resources to avoid forcing implicit defaults into host applications.

## 2) Control Design: CustomControl vs UserControl

### Use `CustomControl` when
- The control must be fully themeable/lookless.
- Consumers need to restyle/retemplate the control across multiple themes.
- You need template contracts (`TemplatePart`, visual states, automation peer).

### Use `UserControl` when
- Building app composition shells or tightly coupled UI fragments.
- The intent is not cross-theme retemplating by consumers.

### Dependency Properties / Events / Commands
- DPs must be bindable, documented, and use metadata options intentionally (`BindsTwoWayByDefault` where appropriate).
- Prefer routed events for control-state notifications (`RoutingStrategy.Bubble`).
- Provide command hooks for user interactions (`Command`, `CommandParameter`) so MVVM works without code-behind event wiring.
- Preserve backward-compatible CLR events when evolving an existing API.

## 3) Styling, Theming, and Visual Polish

### Resource dictionary composition
- `ThemeManager` composition order:
1. Foundation tokens (typography, shared primitives)
2. Theme colors (`Light` / `Dark` / `HighContrast`)
3. Optional system color overrides
4. Generic control templates
5. Optional native control styles

### Multi-theme architecture
- Store one dictionary per theme mode plus optional system-color dictionary.
- Switch themes by replacing only managed dictionaries; leave host resources untouched.
- Keep brushes as `DynamicResource` references to tokenized colors for runtime updates.

### Typography and color tokens
- Publish token keys (e.g., `MosaicTheme.FontFamily`, `MosaicTheme.FontSizeNormal`, accent/status brushes).
- Use tokens in control templates rather than hard-coded values.

## 4) Developer Experience and Tooling

### Design-time
- Keep default templates discoverable via `Generic.xaml`.
- Ensure controls render with sensible defaults in designer (`DefaultStyleKey`, defensive template handling).
- Avoid runtime-only assumptions in constructors that break design surface.

### Metadata and docs
- Add XML docs for all public controls, DPs, events, and command surfaces.
- Use `[Category]`, `[Description]`, `[DefaultEvent]`, `[DefaultProperty]`, and `[TemplatePart]` on reusable controls.
- Keep public API names predictable and consistent across controls.

## 5) Accessibility and Performance

### Baseline accessibility
- Provide automation peers for interactive custom controls.
- Expose appropriate automation patterns (toggle/invoke/value where applicable).
- Support keyboard interaction parity with mouse interaction.
- Raise automation property changed events on state transitions.

### Performance guardrails
- Avoid deep visual trees and expensive per-frame work in templates.
- Cache transformed bitmaps/resources for theme-dependent rendering.
- Unsubscribe template part event handlers when reapplying templates.
- Prefer local updates over global resource churn during theme changes.

## Current Implementation Notes

Implemented in this repository:
- Scoped `ThemeManager` dictionary management.
- Added `HighContrast` theme mode and dictionaries.
- Added foundation typography tokens and keyed text styles.
- Hardened assembly XAML namespace mappings with legacy compatibility.
- Upgraded `ToggleSwitch` with routed event, command support, keyboard handling, and UI automation peer.
- Added demo "Theme Lab" controls for runtime theme/native/system-color switching.
