/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace MosaicWpfDemo.Views
{
    public partial class GettingStartedView
    {
        private const string AppXaml = """
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
        """;

        private const string MainWindowXaml = """
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
        """;

        private const string NamespaceXaml = """
        xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
        """;

        public GettingStartedView()
        {
            InitializeComponent();

            this.AppXamlEditor.Text = AppXaml;
            this.MainWindowXamlEditor.Text = MainWindowXaml;
            this.NamespaceXamlEditor.Text = NamespaceXaml;
        }
    }
}
