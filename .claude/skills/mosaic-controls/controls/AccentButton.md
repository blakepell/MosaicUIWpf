# AccentButton

**Base class:** `Button`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/AccentButton/AccentButton.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/AccentButtonExample.xaml`

## Description

A themed `Button` that changes its accent color based on the `AccentButtonType` enum. Useful for primary/affirmative or destructive actions where the button color should communicate intent.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `AccentButtonType` | `AccentButtonType` | `Default` | Determines the button's accent color scheme. |

`AccentButtonType` values:

| Value | Description |
|---|---|
| `Default` | Standard themed button appearance. |
| `ThemeAccent` | Uses the current theme's accent color. |
| `Gray` | Neutral gray accent. |
| `FluentGreen` | Fluent-style green (affirmative actions). |
| `FluentRed` | Fluent-style red (destructive actions). |

Standard `Button` members apply (`Content`, `Command`, `CommandParameter`, `Click`, etc.).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:AccentButton Content="Save" AccentButtonType="FluentGreen" />
<mosaic:AccentButton Content="Delete" AccentButtonType="FluentRed" />
<mosaic:AccentButton Content="Primary" AccentButtonType="ThemeAccent" Command="{Binding SaveCommand}" />
```

## Notes

- Inherits all `Button` behavior; only the visual accent differs by `AccentButtonType`.
- Accent colors track the active Mosaic theme when `ThemeAccent` is used.
