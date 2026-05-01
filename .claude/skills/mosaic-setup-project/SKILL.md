---
name: mosaic-setup-project
description: "Wire Mosaic UI into a WPF project. Converts App.xaml/App.xaml.cs to MosaicApp, adds ThemeManager to resources, applies WindowChromeBehavior and MosaicTheme brushes to MainWindow, and creates Common/AppSettings.cs + Common/AppViewModel.cs if they do not exist."
argument-hint: "Root namespace of the target WPF project (e.g. 'MyApp'). Defaults to the assembly name."
---

# Mosaic Setup Project

Wires Mosaic UI for WPF into an existing WPF application. The skill touches four files and optionally creates two:

1. `App.xaml` — change root element to `wpf:MosaicApp`, add `ThemeManager` to resources
2. `App.xaml.cs` — inherit from `MosaicApp<AppSettings, AppViewModel>`, call `base.OnStartup` / `base.OnExit`
3. `MainWindow.xaml` — add `WindowChromeBehavior` attached properties, Mosaic background/foreground brushes, and the theme-toggle button in the title bar
4. `MainWindow.xaml.cs` — add minimize/maximize/close/drag helpers and theme-toggle handler
5. `Common/AppSettings.cs` *(create if missing)*
6. `Common/AppViewModel.cs` *(create if missing)*

---

## Step 1 — Gather Information

Before editing anything:

- **Root namespace** — read `App.xaml` (`x:Class` prefix) or ask the user. All generated `namespace` declarations must match.
- **Assembly name** — read the `.csproj` (`<AssemblyName>` or project file name stem). Used for the `xmlns:wpf` and `xmlns:mosaic` assembly references. If the assembly name differs from the root namespace, note it.
- **Existing `Common/AppSettings.cs` and `Common/AppViewModel.cs`** — check with Glob. If they already exist, skip Step 4 and Step 5 but still verify they implement `IAppSettings` (AppSettings) and extend `ObservableObject` (AppViewModel).
- **Existing window chrome or title bar** — read `MainWindow.xaml` to understand the current title bar structure before editing.

---

## Step 2 — Update `App.xaml`

Replace the root element from `<Application ...>` to `<wpf:MosaicApp ...>` and add the required namespaces and `ThemeManager`. The final file must look like this (substitute `{RootNamespace}` and `{AssemblyName}`):

```xml
<wpf:MosaicApp
    x:Class="{RootNamespace}.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
    xmlns:vm="clr-namespace:{RootNamespace}.Common"
    xmlns:wpf="clr-namespace:Mosaic.UI.Wpf;assembly={AssemblyName}"
    x:TypeArguments="vm:AppSettings, vm:AppViewModel"
    Exit="App_OnExit"
    Startup="App_OnStartup"
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

> If `App.xaml` already has a `<ResourceDictionary.MergedDictionaries>` block, insert the `<mosaic:ThemeManager ... />` entry into it rather than replacing the whole block.

---

## Step 3 — Update `App.xaml.cs`

Replace the class declaration so `App` inherits `MosaicApp<AppSettings, AppViewModel>` and calls the base lifecycle methods:

```csharp
using Mosaic.UI.Wpf;
using {RootNamespace}.Common;
using System.Windows;

namespace {RootNamespace}
{
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
```

Preserve any existing `using` directives and event handlers that are not replaced above.

---

## Step 4 — Update `MainWindow.xaml`

Add to the `<Window>` root element (merge with any existing attributes; do not duplicate):

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly={AssemblyName}"
xmlns:theme="http://schemas.apexgate.net/wpf/mosaic-ui"
Background="{DynamicResource {x:Static theme:MosaicTheme.WindowBackgroundBrush}}"
Foreground="{DynamicResource {x:Static theme:MosaicTheme.WindowForegroundBrush}}"
theme:WindowChromeBehavior.CaptionHeight="0"
theme:WindowChromeBehavior.CornerRadius="10"
theme:WindowChromeBehavior.GlassFrameThickness="0"
theme:WindowChromeBehavior.IsEnabled="True"
theme:WindowChromeBehavior.ResizeBorderThickness="5"
theme:WindowChromeBehavior.UseAeroCaptionButtons="True"
Loaded="MainWindow_OnLoaded"
```

Add a custom title bar as the first row of the root `<Grid>` (Row 0, height 35). The title bar is a `<Border>` with a `<DockPanel>` that contains:
- System buttons (minimize / maximize-restore / close) docked right, styled with `{x:Static theme:MosaicTheme.WindowTitleBarButtonStyle}` (close uses `WindowTitleBarCloseButtonStyle`)
- An optional theme-toggle button using the half-circle path icon
- App icon (`/Assets/collage-48.png`) and title `TextBlock` on the left
- `MouseLeftButtonDown="TitleBar_MouseLeftButtonDown"` on the `<Border>` for drag / double-click maximize

If `MainWindow.xaml` already has a custom title bar, preserve it and only add the missing `WindowChromeBehavior` attributes and namespace declarations.

Reference title bar template:

```xml
<Border
    Grid.Row="0"
    Grid.Column="0"
    Grid.ColumnSpan="2"
    Background="{DynamicResource {x:Static SystemColors.WindowBrush}}"
    MouseLeftButtonDown="TitleBar_MouseLeftButtonDown">
    <DockPanel Height="35" LastChildFill="True">
        <StackPanel VerticalAlignment="Center" DockPanel.Dock="Right" Orientation="Horizontal">
            <Button Width="48" Click="ButtonToggleTheme_OnClick"
                    Style="{DynamicResource {x:Static theme:MosaicTheme.WindowTitleBarButtonStyle}}"
                    ToolTip="Toggle Theme">
                <Button.ContentTemplate>
                    <DataTemplate>
                        <Viewbox Width="16" Height="16">
                            <Canvas Width="512" Height="512">
                                <Path Data="M8 256c0 137 111 248 248 248s248-111 248-248S393 8 256 8 8 119 8 256zm248 184V72c101.7 0 184 82.3 184 184 0 101.7-82.3 184-184 184z"
                                      Fill="{DynamicResource {x:Static SystemColors.WindowTextBrushKey}}" />
                            </Canvas>
                        </Viewbox>
                    </DataTemplate>
                </Button.ContentTemplate>
            </Button>
            <Button HorizontalContentAlignment="Center" VerticalContentAlignment="Center"
                    Click="Minimize_Click" Content="&#xE921;" FontFamily="Segoe MDL2 Assets"
                    Style="{DynamicResource {x:Static theme:MosaicTheme.WindowTitleBarButtonStyle}}"
                    ToolTip="Minimize" />
            <Button x:Name="MaxRestoreButton" HorizontalContentAlignment="Center" VerticalContentAlignment="Center"
                    Click="MaximizeRestore_Click" Content="&#xE922;" FontFamily="Segoe MDL2 Assets"
                    Style="{DynamicResource {x:Static theme:MosaicTheme.WindowTitleBarButtonStyle}}"
                    ToolTip="Maximize/Restore" />
            <Button HorizontalContentAlignment="Center" VerticalContentAlignment="Center"
                    Click="Close_Click" Content="&#xE8BB;" FontFamily="Segoe MDL2 Assets"
                    Style="{DynamicResource {x:Static theme:MosaicTheme.WindowTitleBarCloseButtonStyle}}"
                    ToolTip="Close" />
        </StackPanel>
        <Image Width="16" Height="16" Margin="8,0,4,0" VerticalAlignment="Center" Source="/Assets/collage-48.png" />
        <Border Height="35" Background="Transparent">
            <TextBlock Margin="4,0,8,0" VerticalAlignment="Center"
                       FontFamily="Segoe UI" FontSize="12"
                       Foreground="{DynamicResource {x:Static SystemColors.WindowTextBrushKey}}"
                       Text="{Binding Title, RelativeSource={RelativeSource AncestorType=Window}}" />
        </Border>
    </DockPanel>
</Border>
```

---

## Step 5 — Update `MainWindow.xaml.cs`

Ensure the code-behind contains these handlers (add only what is missing; preserve everything else):

```csharp
using System.Windows;
using System.Windows.Input;
using Argus.Memory;
using Mosaic.UI.Wpf.Themes;

namespace {RootNamespace}
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateMaximizeIcon();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) { }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ToggleMaximize();
            else DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e) =>
            ToggleMaximize();

        private void Close_Click(object sender, RoutedEventArgs e) =>
            Close();

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            UpdateMaximizeIcon();
        }

        private void UpdateMaximizeIcon()
        {
            if (MaxRestoreButton == null) return;
            MaxRestoreButton.Content = WindowState == WindowState.Maximized ? "\xE923" : "\xE922";
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            UpdateMaximizeIcon();
        }

        private void ButtonToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            theme.ToggleTheme();
        }
    }
}
```

---

## Step 6 — Create `Common/AppSettings.cs` (if missing)

Path: `Common/AppSettings.cs` relative to the project root.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace {RootNamespace}.Common
{
    public partial class AppSettings : ObservableObject, IAppSettings
    {
        [property: Category("File System")]
        [property: DisplayName("Application Data Folder")]
        [property: Description("The application data folder.")]
        [property: ReadOnly(true)]
        [ObservableProperty]
        private string? _applicationDataFolder;

        [property: Category("File System")]
        [property: DisplayName("Client Settings")]
        [JsonIgnore]
        [ObservableProperty]
        private LocalSettings? _clientSettings = new();

        [property: Category("UI")]
        [property: DisplayName("Theme")]
        [property: Browsable(false)]
        [ObservableProperty]
        private MosaicThemeMode _theme = MosaicThemeMode.Light;

        [property: Category("UI")]
        [property: DisplayName("Font Size")]
        [ObservableProperty]
        private double _fontSize = 12.0;

        [property: Category("UI")]
        [property: DisplayName("Window View States")]
        [ObservableProperty]
        private ObservableCollection<WindowViewState> _windowViewStates = new();
    }
}
```

---

## Step 7 — Create `Common/AppViewModel.cs` (if missing)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace {RootNamespace}.Common
{
    public partial class AppViewModel : ObservableObject
    {
        [ObservableProperty]
        private AppSettings _appSettings = new();

        [ObservableProperty]
        private string _title = "{RootNamespace}";
    }
}
```

---

## Step 8 — Verify

Run:
```
dotnet build <path-to-csproj> -c Debug
```

Confirm **0 errors**. Common issues:

| Symptom | Fix |
|---|---|
| `MosaicApp` not found | Verify `xmlns:wpf` uses the correct assembly name in `App.xaml` |
| `AppSettings`/`AppViewModel` type not found | Check namespace matches root namespace; ensure `x:TypeArguments` values match |
| `WindowChromeBehavior` XAML parse error | Ensure `xmlns:theme="http://schemas.apexgate.net/wpf/mosaic-ui"` is on the `<Window>` element |
| `MaxRestoreButton` not found | The `x:Name="MaxRestoreButton"` button must exist in `MainWindow.xaml` |
| `AppServices` not found | Add `using Argus.Memory;` — it comes from the `Argus.Core` NuGet package |

---

## Quick Checklist

- [ ] `App.xaml` root element is `wpf:MosaicApp` with `x:TypeArguments`
- [ ] `ThemeManager` is in `Application.Resources > MergedDictionaries`
- [ ] `App.xaml.cs` inherits `MosaicApp<AppSettings, AppViewModel>` and calls `base.OnStartup` / `base.OnExit`
- [ ] `MainWindow.xaml` has `WindowChromeBehavior` attributes and Mosaic `Background`/`Foreground` brushes
- [ ] `MainWindow.xaml.cs` has title bar event handlers and theme-toggle handler
- [ ] `Common/AppSettings.cs` exists and implements `IAppSettings`
- [ ] `Common/AppViewModel.cs` exists
- [ ] Build passes with 0 errors
