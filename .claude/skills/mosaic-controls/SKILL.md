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

The waveform visualizer controls are also mapped to the canonical URI, or can be referenced with:
```xml
xmlns:waveform="clr-namespace:Mosaic.UI.Wpf.Controls.WaveformVisualizer;assembly=Mosaic.UI.Wpf"
```

AvalonDock is shipped as a separate package/project and uses its own XAML URI:
```xml
xmlns:ad="https://github.com/blakepell/MosaicUIWpf"
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
| `AccentButton` | `Button` | Input | Button with built-in accent color variants | [AccentButton.md](controls/AccentButton.md) |
| `AdaptiveImage` | `Image` | Media | Pixel-level HSL theme adaptation for images (light/dark mode) | [AdaptiveImage.md](controls/AdaptiveImage.md) |
| `AsteriskTextBlock` | `Control` | Display | Displays masked text with a configurable mask character (default: `●`) | [AsteriskTextBlock.md](controls/AsteriskTextBlock.md) |
| `AudioPlayer` | `Control` | Media | Audio transport control with playlist, seek slider, playback events, and MediaPlayer backing | [AudioPlayer.md](controls/AudioPlayer.md) |
| `AudioPlayerSpectrumAnalyzer` | `FrameworkElement` | Media | WASAPI loopback spectrum analyzer intended to pair with AudioPlayer | [AudioPlayer.md](controls/AudioPlayer.md) |
| `AutoCompleteBox` | `Control` | Input | Searchable autocomplete combo box with async provider support | [AutoCompleteBox.md](controls/AutoCompleteBox.md) |
| `Avatar` | `Button` | Display | User avatar with image source or fallback initials | [Avatar.md](controls/Avatar.md) |
| `Badge` | `ContentControl` | Display | Small inline status/count badge | [Badge.md](controls/Badge.md) |
| `BindablePasswordBox` | `ContentControl` | Input | Password field with a bindable `Password` string property | [BindablePasswordBox.md](controls/BindablePasswordBox.md) |
| `ContentPanel` | `ContentControl` | Layout | Themed content container with header, footer, separator, and corner styling | [ContentPanel.md](controls/ContentPanel.md) |
| `ChatThread` | `UserControl` | Data | Chat conversation view with sent/received message templates | [ChatThread.md](controls/ChatThread.md) |
| `CheckBoxList` | `ListBox` | Input | Multi-select list that shows checkboxes beside each item | [CheckBoxList.md](controls/CheckBoxList.md) |
| `ClipBorder` | `Border` | Layout | Border that clips child content to its `CornerRadius` | [ClipBorder.md](controls/ClipBorder.md) |
| `ColorPicker` | `UserControl` | Input | Visual color picker with hex input and preset colors | [ColorPicker.md](controls/ColorPicker.md) |
| `DatePicker` | `Control` | Input | Mosaic date picker with popup month calendar and nullable selected date | [DatePicker.md](controls/DatePicker.md) |
| `DocumentContainer` | `TabControl` | Navigation | Closable, reorderable document tabs with active-document tracking | [DocumentContainer.md](controls/DocumentContainer.md) |
| `EditableTextBlock` | `Control` | Input | Inline editable text that toggles between view/edit mode | [EditableTextBlock.md](controls/EditableTextBlock.md) |
| `FavoriteCheckBox` | `CheckBox` | Input | Symbol-based favorite/star toggle with configurable brushes | [FavoriteCheckBox.md](controls/FavoriteCheckBox.md) |
| `FileDropper` | `Control` | Input | File drag-and-drop target with file type validation and command/event hooks | [FileDropper.md](controls/FileDropper.md) |
| `Files` | `Control` | Data | Directory file list with shell icons, sorting, selection, watcher refresh, and activation event | [Files.md](controls/Files.md) |
| `FlipPanel` | `ContentControl` | Display | Animated flip panel with front and back content faces | [FlipPanel.md](controls/FlipPanel.md) |
| `GravatarImage` | `Image` | Media | Loads and displays a Gravatar avatar from an email address | [GravatarImage.md](controls/GravatarImage.md) |
| `HexColorTextBox` | `ComboBox` | Input | Editable hex color field (`#RGB`, `#RRGGBB`, `#AARRGGBB`) with shade drop-down | [HexColorTextBox.md](controls/HexColorTextBox.md) |
| `Hyperlink` | `ContentControl` | Navigation | Clickable link that opens a URI or executes an `ICommand` | [Hyperlink.md](controls/Hyperlink.md) |
| `InertiaScrollViewer` | `ScrollViewer` | Layout | ScrollViewer with animated inertia/momentum on mouse wheel | [InertiaScrollViewer.md](controls/InertiaScrollViewer.md) |
| `InfoCard` | `ContentControl` | Display | Card with animated accent bar, title, header, body, and footer | [InfoCard.md](controls/InfoCard.md) |
| `InputWaveformVisualizer` | `WaveformVisualizerBase` | Media | Live waveform from a selectable Windows audio input device | [WaveformVisualizer.md](controls/WaveformVisualizer.md) |
| `LabeledSeparator` | `ContentControl` | Display | Horizontal separator with an embedded text label | [LabeledSeparator.md](controls/LabeledSeparator.md) |
| `LoopbackWaveformVisualizer` | `WaveformVisualizerBase` | Media | Live waveform from the default Windows audio render device via WASAPI loopback | [WaveformVisualizer.md](controls/WaveformVisualizer.md) |
| `MarkdownEditor` | `UserControl` | Input | Markdown editor built on SyntaxEditor with toolbar, snippets, save helpers, and preview/copy actions | [MarkdownEditor.md](controls/MarkdownEditor.md) |
| `MarkdownViewer` | `Control` | Display | WPF-native Markdown renderer backed by a copyable FlowDocument/RichTextBox | [MarkdownViewer.md](controls/MarkdownViewer.md) |
| `MessageBox` | static class | Dialog | Themed drop-in replacement for System.Windows.MessageBox Show overloads | [MessageBox.md](controls/MessageBox.md) |
| `NumericTextBox` | `TextBox` | Input | Text box that accepts only numeric input with configurable decimal places | [NumericTextBox.md](controls/NumericTextBox.md) |
| `ProgressRing` | `Control` | Display | Indeterminate animated ring/spinner controlled by IsActive | [ProgressRing.md](controls/ProgressRing.md) |
| `PropertyGrid` | `Control` | Data | Object property inspector using `TypeDescriptor` with category grouping | [PropertyGrid.md](controls/PropertyGrid.md) |
| `RadialProgressBar` | `ProgressBar` | Display | Circular progress bar with fill, pie, shape, and indeterminate modes | [RadialProgressBar.md](controls/RadialProgressBar.md) |
| `RelativePanel` | `Panel` | Layout | Arranges children relative to each other via attached properties | [RelativePanel.md](controls/RelativePanel.md) |
| `ScalingTextBlock` | `TextBlock` | Display | TextBlock that automatically lowers font size to fit available width | [ScalingTextBlock.md](controls/ScalingTextBlock.md) |
| `SearchBox` | `TextBox` | Input | Text box with watermark, clear button, and `SearchExecuted` event | [SearchBox.md](controls/SearchBox.md) |
| `SettingsItem` | `ContentControl` | Display | Settings row with icon, title, description, and slot for a control | [SettingsItem.md](controls/SettingsItem.md) |
| `ShadowPanel` | `ContentControl` | Layout | Content container with configurable drop shadow effect | [ShadowPanel.md](controls/ShadowPanel.md) |
| `ShadowTextBlock` | `TextBlock` | Display | TextBlock with automatically applied drop shadow | [ShadowTextBlock.md](controls/ShadowTextBlock.md) |
| `Shield` | `Control` | Display | GitHub-style shield/badge with label and value sections | [Shield.md](controls/Shield.md) |
| `SideMenu` | `UserControl` | Navigation | Collapsible navigation side menu with search and drag-drop reorder | [SideMenu.md](controls/SideMenu.md) |
| `SimpleStackPanel` | `Panel` | Layout | Efficient stack panel with uniform `Spacing` between children | [SimpleStackPanel.md](controls/SimpleStackPanel.md) |
| `SmallPanel` | `Panel` | Layout | Overlay panel that stacks children in the same bounds | [SmallPanel.md](controls/SmallPanel.md) |
| `SplitButton` | `ContentControl` | Input | Button with a separate drop-down chevron that opens a `ContextMenu` | [SplitButton.md](controls/SplitButton.md) |
| `SplitPanel` | `Control` | Layout | Two-pane resizable container with a draggable GridSplitter and bindable split ratio | [SplitPanel.md](controls/SplitPanel.md) |
| `StopwatchDisplay` | `ContentControl` | Display | Real-time stopwatch with Start/Stop/Reset methods | [StopwatchDisplay.md](controls/StopwatchDisplay.md) |
| `StringListEditor` | `ContentControl` | Input | Editable list of strings with add/remove and duplicate/validation support | [StringListEditor.md](controls/StringListEditor.md) |
| `SymbolRating` | `Control` | Input | Star/symbol rating control with hover preview and deselect | [SymbolRating.md](controls/SymbolRating.md) |
| `SyntaxEditor` | `TextEditor` | Input | AvalonEdit-based code editor with Mosaic themes, bundled syntax highlighting, search, JSON commands, and line editing commands | [SyntaxEditor.md](controls/SyntaxEditor.md) |
| `TabControl` | `TabControl` | Navigation | Mosaic-themed tab control with top/bottom active indicator | [TabControl.md](controls/TabControl.md) |
| `TagBox` | `Control` | Input | Token/tag entry box with removable chips, duplicate control, and cancellable change events | [TagBox.md](controls/TagBox.md) |
| `ToggleButton` | `ToggleButton` | Input | Mosaic-themed toggle button with checked/unchecked background | [ToggleButton.md](controls/ToggleButton.md) |
| `ToggleSwitch` | `Control` | Input | Mobile-style on/off toggle switch with custom colors and MVVM | [ToggleSwitch.md](controls/ToggleSwitch.md) |
| `TwoPaneView` | `Control` | Layout | Adaptive two-pane layout (side-by-side or stacked) | [TwoPaneView.md](controls/TwoPaneView.md) |
| `ValidationSummaryPanel` | `ItemsControl` | Data | Validation error collector and display panel with auto-hide | [ValidationSummaryPanel.md](controls/ValidationSummaryPanel.md) |
| `VersionTextBlock` | `TextBlock` | Display | Displays version string from a configurable assembly source | [VersionTextBlock.md](controls/VersionTextBlock.md) |
| `VT52Terminal` | `TextEditor` | Terminal | Full ANSI/VT100/VT220/xterm terminal emulator (AvalonEdit-based) | [VT52Terminal.md](controls/VT52Terminal.md) |
| `WindowTitleBar` | `UserControl` | Windowing | Custom Mosaic window title bar with min/max/close and custom content slots | [WindowTitleBar.md](controls/WindowTitleBar.md) |

---

## Integration Packages And Behaviors

These are not all main-assembly controls, but they are part of the Mosaic usage surface and have demo examples.

| Item | Kind | Namespace / URI | Description | Details |
|---|---|---|---|---|
| `DockingManager` / `MosaicTheme` | Separate package/project | `https://github.com/blakepell/MosaicUIWpf` | Mosaic-themed AvalonDock fork/integration for IDE-style document and tool-window docking | [AvalonDock.md](controls/AvalonDock.md) |
| `AvalonEditVtTerminalBehavior` | Behavior | `Mosaic.UI.Wpf.Behaviors` | Applies a retro VT/CRT visual skin to AvalonEdit `TextEditor` controls | [AvalonEditBehaviors.md](controls/AvalonEditBehaviors.md) |
| `AvalonTextEditorBindingBehavior` | Behavior | `Mosaic.UI.Wpf.Behaviors` | Adds bindable text, selection, selected text, and caret offset to AvalonEdit `TextEditor` | [AvalonEditBehaviors.md](controls/AvalonEditBehaviors.md) |
| `AvalonEditPropertiesBehavior` | Behavior | `Mosaic.UI.Wpf.Behaviors` | Binds caret brush and hyperlink enablement for AvalonEdit `TextEditor` | [AvalonEditBehaviors.md](controls/AvalonEditBehaviors.md) |

## Support Controls

These public controls exist for native style dictionaries and low-level template composition. Prefer the higher-level controls in the main inventory unless you are editing Mosaic's native styles or need this exact primitive.

| Control | Base Class | Primary Use | Details |
|---|---|---|---|
| `SliderRepeatButton` | `RepeatButton` | Slider track repeat buttons with corner orientation metadata | [SupportControls.md](controls/SupportControls.md) |
| `SystemDropShadowChrome` | `Decorator` | WPF-style chrome shadow rendering for popups and native templates | [SupportControls.md](controls/SupportControls.md) |
| `WDScrollViewer` | `ScrollViewer` | Native `TreeView` template scrolling with optional wheel animation | [SupportControls.md](controls/SupportControls.md) |

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
- **CustomControls** (most controls) support re-templating; `UserControl`-based controls (`ColorPicker`, `ChatThread`, `MarkdownEditor`, `SideMenu`, `WindowTitleBar`) do not.
- **`VT52Terminal`** requires the separate sub-namespace `Mosaic.UI.Wpf.Controls.VT52Terminal`.
- **`InputWaveformVisualizer`** and **`LoopbackWaveformVisualizer`** live in `Mosaic.UI.Wpf.Controls.WaveformVisualizer`, but are mapped to the canonical Mosaic XAML URI.
- **AvalonDock** lives in `Mosaic.UI.Wpf.AvalonDock` / package `MosaicUIWpf.AvalonDock` and uses `xmlns:ad="https://github.com/blakepell/MosaicUIWpf"`.
- **`AdaptiveImage`** requires `ThemeManager` in the DI container (`AppServices`).
- **`PropertyGrid`** reads `[Category]`, `[Description]`, and `[PropertyGridAttribute]` attributes on the target object.
- **`SideMenu`** uses `ContentTypeIsSingleton` + `AppServices` DI for singleton view navigation in the demo.
- Public support controls such as `WDScrollViewer`, `SliderRepeatButton`, and `SystemDropShadowChrome` are primarily used by native theme dictionaries; see [SupportControls.md](controls/SupportControls.md).
