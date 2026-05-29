# <span><img src="./docs/images/collage-48.png" alt="Mosaic Icon" height="48" style="vertical-align: middle;"> Mosaic UI for WPF</span>

Mosaic UI for WPF is a control library that seeks to provide compartmentalized controls that if needed could be easily extracted from this project and included in your own.

Almost every WPF UI kit contains a way to theme that's custom and unique to their framework, it makes mixing and matching controls sometimes cumbersome. This project is trying to provide enough themeing options that you could use it by itself but not tether all of the controls to it so you could include it, or parts of it in other projects that use other UI libraries.  I'll also be providing basic examples of how to use each control so you've got a copy/paste reference to start from and a working demo app to quickly browse and try out controls.

WPF is a great, mature technology that will be with us for a long time now that's part of .NET Core.  WPF's current and future longevity are one of its strengths.  If you're writing a hobby project you want to be around in 10 years, WPF is a great choice.  Credit to the dotnet developer team for bringing it the modern .NET stack.

If you find this project interesting or useful, please give it a star.

> Note: Expect frequent updates to the styling system during the early stages of this project.
> I’m currently exploring various theming patterns through a process of experimentation. I've concluded that customizing the default control templates for native WPF controls is necessary; these will be provided in a separate ResourceDictionary.
> Previously, I overrode SystemColors, but this approach proved too invasive—particularly for applications aiming to coexist with other UI frameworks without conflict.  Also, there are many stock controls that use static resources or hard coded colors so SystemColors gets you some of the way there, but not all of the way

A final note, if you have a useful reusable (mostly) compartmentalized control you want to share and be creditted for, feel free to put in a pull request and I'll look at it.

## Getting Started

### Install the NuGet Package

Install the package from [NuGet](https://www.nuget.org/packages/MosaicUIWpf) using the .NET CLI:

```bash
dotnet add package MosaicUIWpf
```

Or search for **MosaicUIWpf** in the Visual Studio NuGet Package Manager.

### Set Up Your Application

**1. Update `App.xaml`** to use `MosaicApp` and merge `ThemeManager` into your application resources:

```xml
<wpf:MosaicApp
    x:Class="YourApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
    xmlns:vm="clr-namespace:YourApp.Common"
    xmlns:wpf="clr-namespace:Mosaic.UI.Wpf;assembly=Mosaic.UI.Wpf"
    x:TypeArguments="vm:AppSettings, vm:AppViewModel"
    StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <mosaic:ThemeManager
                    Native="True"
                    SystemColors="True"
                    Theme="Dark" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</wpf:MosaicApp>
```

Set `Theme` to `"Light"`, `"Dark"`, or `"HighContrast"`. Set `Native="True"` to restyle built-in WPF controls. Set `SystemColors="True"` to integrate with the OS accent color.

**2. Update `MainWindow.xaml`** to apply the window chrome behavior and Mosaic theme brushes:

```xml
<Window
    x:Class="YourApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"
    xmlns:theme="http://schemas.apexgate.net/wpf/mosaic-ui"
    theme:WindowChromeBehavior.CaptionHeight="0"
    theme:WindowChromeBehavior.CornerRadius="10"
    theme:WindowChromeBehavior.IsEnabled="True"
    theme:WindowChromeBehavior.ResizeBorderThickness="5"
    Background="{DynamicResource {x:Static theme:MosaicTheme.WindowBackgroundBrush}}"
    Foreground="{DynamicResource {x:Static theme:MosaicTheme.WindowForegroundBrush}}">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="35" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <mosaic:WindowTitleBar Grid.Row="0" IconSource="/Assets/icon.png" />

        <!-- Your content here -->
    </Grid>
</Window>
```

**3. Use controls** anywhere in your XAML with the `mosaic` namespace (`http://schemas.apexgate.net/wpf/mosaic-ui`):

```xml
xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
```

For a complete wiring example, see `MosaicTemplateApp` in the repository.

## Dark Theme

![Dark Theme](./docs/images/screen-chat1.png)

## Light Theme

![Light Theme](./docs/images/screen-property-grid.png)

## Architecture Blueprint

See `docs/ARCHITECTURE_BLUEPRINT.md` for the control-library architecture, theming model, interoperability rules, accessibility baseline, and DX guidelines.

## Solution Projects

| Feature                | Description                                                                                                                            |
|------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| Mosaic.UI.Wpf          | The control library.                                                                                                                    |
| MosaicWpfDemo          | The main demo project that houses examples and snippets of each control in the library as well as usage of other types and behaviors.   |

## Included Controls

The following table lists the controls found in `src/Mosaic.UI.Wpf/Controls` and their class descriptions (from XML comments).

| Control | Description |
|---|---|
| AdaptiveImage | Image that adapts its colors to match app/OS theme. Intended purpose is to be used for icons to be able to adapt. |
| AsteriskTextBlock | A text block that displays asterisks for each character in its text property. |
| AutoCompleteBox | Represents an editable selection control that filters suggestions while the user types and commits a selected item. |
| Avatar | Represents a customizable avatar control that displays content with support for corner radius and template selection. |
| Badge | A badge component. |
| BindablePasswordBox | Represents a password input control that supports data binding for the password value. |
| ChatThread | A chat thread control which shows sent and received messages in a single thread format. |
| CheckBoxList | A ListBox variant that defaults to multiple selection and displays a checkbox beside each item. |
| ClipBorder | Border which allows clipping to its border. Useful especially when you need to clip to round corners. |
| ColorPicker | A color picker UserControl that allows users to select colors from presets or enter hex values. |
| EditableTextBlock | Represents a control that displays text in a non-editable mode and allows users to switch to an editable mode to modify the text. The control supports double-click editing, text trimming, and customizable appearance. |
| FlipPanel | A flip panel component that can display two different content sides and animate between them. |
| GravatarImage | Displays a Gravatar Image for a specified email address. |
| HexColorTextBox | A ComboBox-based control that allows editing and selecting colors using hex strings (supports #RGB, #RRGGBB, #AARRGGBB) and named brushes. |
| Hyperlink | Represents a hyperlink control that displays text and provides navigation functionality. |
| InertiaScrollViewer | Represents a scroll viewer that supports inertia-based scrolling animations. |
| InfoCard | An info card with a highlight color on the left hand side. |
| LabeledSeparator | A labeled separator. |
| NumericTextBox | TextBox that only allows digits, minus sign and a decimal point. |
| PropertyGrid | A control that displays the properties of an object in a grid format. |
| RelativePanel | Defines an area within which you can position and align child objects in relation to each other or the parent panel. |
| SearchBox | A custom TextBox tailored for searching or filtering. |
| SettingsItem | A settings item control. |
| ShadowPanel | A panel control that applies a drop shadow effect to its child content. Provides properties to control shadow elevation and density/thickness. |
| ShadowTextBlock | A TextBlock that automatically applies a configurable DropShadowEffect to its text via BlurRadius, ShadowDepth, and ShadowColor. |
| Shield | A shield component (shows a property and a value). |
| SideMenu | Represents a side menu control that displays a collection of menu items and allows item selection. |
| SimpleStackPanel | Arranges child elements into a single line that can be oriented horizontally or vertically that is more efficient that the normal StackPanel. |
| SmallPanel | Represents a custom panel that arranges its child elements in a single layer and ensures that each child is measured and arranged within the available space. |
| SplitButton | Represents a split button with a primary action surface and a separate drop-down surface that opens a context menu. |
| StopwatchDisplay | Represents a stopwatch control that provides functionality to display a stopwatch timer as UI element. |
| StringListEditor | A StringListEditor component. |
| SymbolRating | A symbol rating component. |
| SystemDropShadowChrome | Creates a theme specific look for drop shadow effects. |
| TabControl | Represents a tab control that allows users to switch between multiple tabs. |
| ToggleButton | Represents a button control that can switch between two states: checked and unchecked. This implementation looks like a theme styled switch. |
| ToggleSwitch | Represents a toggle switch control that allows users to switch between two states, such as "On" and "Off". |
| TwoPaneView | Represents a container with two views that size and position content in the available space, either side-by-side or top-bottom. |
| TypingProgress | Represents a control that visually indicates typing progress, typically used in chat or messaging scenarios. |
| ValidationSummaryPanel | A validation summary panel that displays all validation errors from child controls in a form. Supports both WPF's built-in validation and IDataErrorInfo/INotifyDataErrorInfo. |
| VersionTextBlock | A TextBlock that displays an assembly version. |
| VT52Terminal | A terminal emulator hosted inside an AvalonEdit TextEditor. Call Add(string) or Add(byte[]) with remote data; call SendKey/SendString for local keystrokes. Subscribe to Transmit to get bytes that the terminal sends back. |
| WindowTitleBar | A self-contained custom title bar control for borderless/chrome-less WPF windows. Automatically wires up drag-to-move, double-click maximize/restore, and the standard window system buttons. |

## Included Behaviors

The following table lists the behaviors found in `src/Mosaic.UI.Wpf/Behaviors`.

| Behavior | Description |
|---|---|
| AvalonEditBindingBehavior | AvalonEdit TextEditor binding behavior that allows for binding of the text property, the selected text property, the selection property, and the cursor position property. |
| AvalonEditCopyBehavior | A behavior that enables a Button to copy text from a specified AvalonEdit TextEditor to the clipboard. |
| AvalonEditPropertiesBehavior | A behavior that allows various common properties of an AvalonEdit TextEditor to be dynamically set or bound. |
| AvalonEditVtTerminalBehavior | A behavior that applies a retro VT/CRT terminal visual skin to an AvalonEdit TextEditor. |
| BrushModifier | Provides attached properties and utility methods for modifying the appearance of brushes used in WPF controls, such as lightening the background color of elements. |
| FrameworkElementZoomFontSizeOnMouseWheelBehavior | A behavior that enables zooming the font size of a FrameworkElement using the mouse wheel while the Ctrl key is held. |
| ItemsControlAutoScrollBehavior | Scrolls an ItemsControl to the last item when the collection changes. |
| ItemsControlFilterBehavior | A behavior that allows a TextBox to filter an ItemsControl. |
| TextBoxClearOnEscapeBehavior | Clears the contents of a TextBoxBase when the Escape key is pressed. |
| TextBoxCopyBehavior | A behavior that enables a Button to copy text from a specified TextBox to the clipboard. |
| WindowChromeBehavior | Attached behavior to apply and maintain WindowChrome settings on a Window. |
