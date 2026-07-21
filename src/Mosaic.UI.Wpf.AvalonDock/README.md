# AvalonDock for Mosaic UI WPF

`Mosaic.UI.Wpf.AvalonDock` provides Mosaic-themed styling for AvalonDock, a WPF document and tool-window layout system. It lets applications create IDE-style docking surfaces with documents, tool panes, floating windows, auto-hide panes, and draggable layout chrome while following Mosaic UI theme tokens.

## Screenshot

TODO: Add a screenshot of the Mosaic-themed AvalonDock demo.

## Getting Started

Add the AvalonDock namespace and assign `MosaicTheme` to the `DockingManager.Theme` property. From there, define a `LayoutRoot` with document panes and anchorable tool panes.

```xml
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ad="https://github.com/blakepell/MosaicUIWpf"
    xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui">
    <Grid Margin="12">
        <Border
            BorderBrush="{DynamicResource {x:Static mosaic:MosaicTheme.ControlBorderBrush}}"
            BorderThickness="1">
            <ad:DockingManager>
                <ad:DockingManager.Theme>
                    <ad:MosaicTheme />
                </ad:DockingManager.Theme>

                <ad:LayoutRoot>
                    <ad:LayoutPanel Orientation="Horizontal">
                        <ad:LayoutAnchorablePane DockWidth="230">
                            <ad:LayoutAnchorable
                                Title="Solution Explorer"
                                CanClose="False"
                                ContentId="solutionExplorer">
                                <TreeView BorderThickness="0">
                                    <TreeViewItem Header="MyApp" IsExpanded="True">
                                        <TreeViewItem Header="Views" />
                                        <TreeViewItem Header="Models" />
                                    </TreeViewItem>
                                </TreeView>
                            </ad:LayoutAnchorable>
                        </ad:LayoutAnchorablePane>

                        <ad:LayoutDocumentPane>
                            <ad:LayoutDocument
                                Title="Overview"
                                ContentId="overview"
                                IsSelected="True">
                                <TextBlock
                                    Margin="12"
                                    Text="This document is hosted in a Mosaic-themed AvalonDock surface."
                                    TextWrapping="Wrap" />
                            </ad:LayoutDocument>
                        </ad:LayoutDocumentPane>

                        <ad:LayoutAnchorablePane DockWidth="260">
                            <ad:LayoutAnchorable Title="Properties" ContentId="properties">
                                <TextBlock Margin="12" Text="Property content goes here." />
                            </ad:LayoutAnchorable>
                        </ad:LayoutAnchorablePane>
                    </ad:LayoutPanel>

                    <ad:LayoutRoot.BottomSide>
                        <ad:LayoutAnchorSide>
                            <ad:LayoutAnchorGroup>
                                <ad:LayoutAnchorable Title="Output" ContentId="output">
                                    <TextBlock Margin="12" Text="Auto-hide panes use Mosaic colors too." />
                                </ad:LayoutAnchorable>
                            </ad:LayoutAnchorGroup>
                        </ad:LayoutAnchorSide>
                    </ad:LayoutRoot.BottomSide>
                </ad:LayoutRoot>
            </ad:DockingManager>
        </Border>
    </Grid>
</Window>
```

## Editor Documents

`LayoutMarkdownEditor` and `LayoutSyntaxEditor` are ready-to-dock, saveable document models. Both track unsaved edits, append `*` to the tab title, expose file metadata, and provide `LoadFile`, `NewDocument`, `Save`, `SaveAsync`, and `SaveAsAsync` operations through `ISaveable`.

```csharp
var document = new LayoutSyntaxEditor("Example.cs");
document.Editor.Language = SyntaxLanguage.CSharp;
document.AdditionalToolBarItems.Add(new Button { Content = "Build" });
documentPane.Children.Add(document);
```

The syntax document exposes both its `Editor` and `ToolBar` fields publicly. Items added to `AdditionalToolBarItems` are placed after the built-in save button. `Ctrl+S` saves and `Ctrl+Shift+S` opens Save As. `LayoutMarkdownEditor` hosts Mosaic's complete `MarkdownEditor`, including its formatting and save toolbar.
