---
name: mosaic-controls
description: "Overview of all Mosaic UI for WPF controls. Use when: implementing a control, looking up properties/events, asking what controls exist, choosing the right control for a UI requirement."
argument-hint: "Optional: name of a specific control to focus on (e.g. 'ToggleSwitch', 'AutoCompleteBox')"
---

# Mosaic UI for WPF — Control Overview

Mosaic UI for WPF (`Mosaic.UI.Wpf`) is a lookless, theme-aware WPF control library. Controls are consumed via the canonical XML namespace:

```xml
xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
```

or the CLR form:

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"
```

The `VT52Terminal` control requires the sub-namespace:
```xml
xmlns:vt52="clr-namespace:Mosaic.UI.Wpf.Controls.VT52Terminal;assembly=Mosaic.UI.Wpf"
```

For theme tokens (brushes, font sizes, etc.), declare:
```xml
xmlns:themes="http://schemas.apexgate.net/wpf/mosaic-ui"
```
and reference tokens like `{DynamicResource {x:Static themes:MosaicTheme.ControlTextForegroundBrush}}`.

---

## Control Inventory

For detailed property/event/example documentation, read the individual file listed in the **Details** column.

| Control | Base Class | Category | Description | Details |
|---|---|---|---|---|
| `AdaptiveImage` | `Image` | Media | Pixel-level HSL theme adaptation for images (light/dark mode) | [AdaptiveImage.md](controls/AdaptiveImage.md) |
| `AsteriskTextBlock` | `Control` | Display | Displays masked text with a configurable mask character (default: `●`) | [AsteriskTextBlock.md](controls/AsteriskTextBlock.md) |
| `AutoCompleteBox` | `Control` | Input | Searchable autocomplete combo box with async provider support | [AutoCompleteBox.md](controls/AutoCompleteBox.md) |
| `Avatar` | `Button` | Display | User avatar with image source or fallback initials | [Avatar.md](controls/Avatar.md) |
| `Badge` | `ContentControl` | Display | Small inline status/count badge | [Badge.md](controls/Badge.md) |
| `BindablePasswordBox` | `ContentControl` | Input | Password field with a bindable `Password` string property | [BindablePasswordBox.md](controls/BindablePasswordBox.md) |
| `ChatThread` | `UserControl` | Data | Chat conversation view with sent/received message templates | [ChatThread.md](controls/ChatThread.md) |
| `CheckBoxList` | `ListBox` | Input | Multi-select list that shows checkboxes beside each item | [CheckBoxList.md](controls/CheckBoxList.md) |
| `ClipBorder` | `Border` | Layout | Border that clips child content to its `CornerRadius` | [ClipBorder.md](controls/ClipBorder.md) |
| `ColorPicker` | `UserControl` | Input | Visual color picker with hex input and preset colors | [ColorPicker.md](controls/ColorPicker.md) |
| `EditableTextBlock` | `Control` | Input | Inline editable text that toggles between view/edit mode | [EditableTextBlock.md](controls/EditableTextBlock.md) |
| `FlipPanel` | `ContentControl` | Display | Animated flip panel with front and back content faces | [FlipPanel.md](controls/FlipPanel.md) |
| `GravatarImage` | `Image` | Media | Loads and displays a Gravatar avatar from an email address | [GravatarImage.md](controls/GravatarImage.md) |
| `HexColorTextBox` | `ComboBox` | Input | Editable hex color field (`#RGB`, `#RRGGBB`, `#AARRGGBB`) with shade drop-down | [HexColorTextBox.md](controls/HexColorTextBox.md) |
| `Hyperlink` | `ContentControl` | Navigation | Clickable link that opens a URI or executes an `ICommand` | [Hyperlink.md](controls/Hyperlink.md) |
| `InertiaScrollViewer` | `ScrollViewer` | Layout | ScrollViewer with animated inertia/momentum on mouse wheel | [InertiaScrollViewer.md](controls/InertiaScrollViewer.md) |
| `InfoCard` | `ContentControl` | Display | Card with animated accent bar, title, header, body, and footer | [InfoCard.md](controls/InfoCard.md) |
| `LabeledSeparator` | `ContentControl` | Display | Horizontal separator with an embedded text label | [LabeledSeparator.md](controls/LabeledSeparator.md) |
| `MusicPlayer` | `UserControl` | Media | Music playback UI with track metadata and playback controls | [MusicPlayer.md](controls/MusicPlayer.md) |
| `NumericTextBox` | `TextBox` | Input | Text box that accepts only numeric input with configurable decimal places | [NumericTextBox.md](controls/NumericTextBox.md) |
| `PropertyGrid` | `Control` | Data | Object property inspector using `TypeDescriptor` with category grouping | [PropertyGrid.md](controls/PropertyGrid.md) |
| `RelativePanel` | `Panel` | Layout | Arranges children relative to each other via attached properties | [RelativePanel.md](controls/RelativePanel.md) |
| `SearchBox` | `TextBox` | Input | Text box with watermark, clear button, and `SearchExecuted` event | [SearchBox.md](controls/SearchBox.md) |
| `SettingsItem` | `ContentControl` | Display | Settings row with icon, title, description, and slot for a control | [SettingsItem.md](controls/SettingsItem.md) |
| `ShadowPanel` | `ContentControl` | Layout | Content container with configurable drop shadow effect | [ShadowPanel.md](controls/ShadowPanel.md) |
| `ShadowTextBlock` | `TextBlock` | Display | TextBlock with automatically applied drop shadow | [ShadowTextBlock.md](controls/ShadowTextBlock.md) |
| `Shield` | `Control` | Display | GitHub-style shield/badge with label and value sections | [Shield.md](controls/Shield.md) |
| `SideMenu` | `UserControl` | Navigation | Collapsible navigation side menu with search and drag-drop reorder | [SideMenu.md](controls/SideMenu.md) |
| `SimpleStackPanel` | `Panel` | Layout | Efficient stack panel with uniform `Spacing` between children | [SimpleStackPanel.md](controls/SimpleStackPanel.md) |
| `SmallPanel` | `Panel` | Layout | Overlay panel that stacks children in the same bounds | [SmallPanel.md](controls/SmallPanel.md) |
| `SplitButton` | `ContentControl` | Input | Button with a separate drop-down chevron that opens a `ContextMenu` | [SplitButton.md](controls/SplitButton.md) |
| `StopwatchDisplay` | `ContentControl` | Display | Real-time stopwatch with Start/Stop/Reset methods | [StopwatchDisplay.md](controls/StopwatchDisplay.md) |
| `StringListEditor` | `ContentControl` | Input | Editable list of strings with add/remove and duplicate/validation support | [StringListEditor.md](controls/StringListEditor.md) |
| `SymbolRating` | `Control` | Input | Star/symbol rating control with hover preview and deselect | [SymbolRating.md](controls/SymbolRating.md) |
| `TabControl` | `TabControl` | Navigation | Mosaic-themed tab control with top/bottom active indicator | [TabControl.md](controls/TabControl.md) |
| `ToggleButton` | `ToggleButton` | Input | Mosaic-themed toggle button with checked/unchecked background | [ToggleButton.md](controls/ToggleButton.md) |
| `ToggleSwitch` | `Control` | Input | Mobile-style on/off toggle switch with custom colors and MVVM | [ToggleSwitch.md](controls/ToggleSwitch.md) |
| `TwoPaneView` | `Control` | Layout | Adaptive two-pane layout (side-by-side or stacked) | [TwoPaneView.md](controls/TwoPaneView.md) |
| `ValidationSummaryPanel` | `ItemsControl` | Data | Validation error collector and display panel with auto-hide | [ValidationSummaryPanel.md](controls/ValidationSummaryPanel.md) |
| `VersionTextBlock` | `TextBlock` | Display | Displays version string from a configurable assembly source | [VersionTextBlock.md](controls/VersionTextBlock.md) |
| `VT52Terminal` | `TextEditor` | Terminal | Full ANSI/VT100/VT220/xterm terminal emulator (AvalonEdit-based) | [VT52Terminal.md](controls/VT52Terminal.md) |
| `WindowTitleBar` | `UserControl` | Windowing | Custom Mosaic window title bar with min/max/close and custom content slots | [WindowTitleBar.md](controls/WindowTitleBar.md) |

---

## Theme Tokens

Reference theme brushes via `DynamicResource` using `MosaicTheme` static keys:

```xml
xmlns:themes="http://schemas.apexgate.net/wpf/mosaic-ui"

<!-- Foreground / background -->
Foreground="{DynamicResource {x:Static themes:MosaicTheme.ControlTextForegroundBrush}}"
Background="{DynamicResource {x:Static themes:MosaicTheme.ControlBackgroundBrush}}"

<!-- Border -->
BorderBrush="{DynamicResource {x:Static themes:MosaicTheme.ControlBorderBrush}}"

<!-- Accent / status -->
Fill="{DynamicResource {x:Static themes:MosaicTheme.AccentBrush}}"
Fill="{DynamicResource {x:Static themes:MosaicTheme.SuccessBrush}}"
Fill="{DynamicResource {x:Static themes:MosaicTheme.WarningBrush}}"
Fill="{DynamicResource {x:Static themes:MosaicTheme.ErrorBrush}}"
Fill="{DynamicResource {x:Static themes:MosaicTheme.InfoBrush}}"

<!-- Font sizes -->
FontSize="{DynamicResource {x:Static themes:MosaicTheme.FontSizeSmall}}"
FontSize="{DynamicResource {x:Static themes:MosaicTheme.FontSizeMedium}}"
FontSize="{DynamicResource {x:Static themes:MosaicTheme.FontSizeLarge}}"
```

---

## App Setup

Consumer apps wire Mosaic by:
1. Using `MosaicApp<TSettings, TViewModel>` as the `Application` base in `App.xaml`.
2. Merging a `ThemeManager` into application resources.
3. Placing `mosaic:WindowTitleBar` + `WindowChromeBehavior` in `MainWindow`.

See the `mosaic-setup-project` skill for the full wiring procedure.

---

## Quick Tips

- **Always use `DynamicResource`** for theme tokens — switching themes requires live updates.
- **CustomControls** (most controls) support re-templating; `UserControl`-based controls (`ColorPicker`, `ChatThread`, `SideMenu`, `WindowTitleBar`) do not.
- **`VT52Terminal`** requires the separate sub-namespace `Mosaic.UI.Wpf.Controls.VT52Terminal`.
- **`AdaptiveImage`** requires `ThemeManager` in the DI container (`AppServices`).
- **`PropertyGrid`** reads `[Category]`, `[Description]`, and `[PropertyGridAttribute]` attributes on the target object.
- **`SideMenu`** uses `ContentTypeIsSingleton` + `AppServices` DI for singleton view navigation in the demo.
