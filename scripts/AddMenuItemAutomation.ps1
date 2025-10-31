<#
.SYNOPSIS
    Automates the addition of new control examples to the MosaicWpfDemo application.
    
.DESCRIPTION
    This script creates example XAML/C# files and registers them in both the .csproj file 
    and MainWindow.xaml, eliminating manual file creation and configuration.
    
.PARAMETER ExampleName
    The name of the control to document (e.g., "MyControl" for "MyControlExample")
    
.PARAMETER ControlType
    The full type name of the control being documented (e.g., "MyControl")
    
.PARAMETER IconPath
    The relative path to the icon asset (e.g., "/Assets/my-control-48.png")
    
.PARAMETER ProjectRoot
    The root path of the MosaicWpfDemo project (default: current directory)
    
.PARAMETER CreateFiles
    Switch to create the XAML and C# template files if they don't exist
    
.PARAMETER ApplyChanges
    Switch to apply changes to actual files (without this, shows preview only)
    
.EXAMPLE
    # Preview only (no files created or modified)
    .\AddMenuItemAutomation.ps1 -ExampleName "MyControl" -ControlType "MyControl" -IconPath "/Assets/my-control-48.png"
    
.EXAMPLE
    # Create files and apply all changes
    .\AddMenuItemAutomation.ps1 -ExampleName "MyControl" -ControlType "MyControl" -IconPath "/Assets/my-control-48.png" -CreateFiles -ApplyChanges

    # Example
    ../../AddMenuItemAutomation.ps1 -ExampleName "MusicPlayer" -ControlType "MusicPlayer" -IconPath "/Assets/circle-48.png" -CreateFiles -ApplyChanges
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ExampleName,
    
    [Parameter(Mandatory = $true)]
    [string]$ControlType,
    
    [Parameter(Mandatory = $true)]
    [string]$IconPath,
    
    [string]$ProjectRoot = (Get-Location).Path,
    
    [switch]$CreateFiles,
    
    [switch]$ApplyChanges
)

# Define paths
$examplesDir = Join-Path $ProjectRoot "Views\Examples"
$xamlFile = Join-Path $examplesDir "${ExampleName}Example.xaml"
$codeFile = Join-Path $examplesDir "${ExampleName}Example.xaml.cs"
$csprojFile = Join-Path $ProjectRoot "MosaicWpfDemo.csproj"
$mainWindowFile = Join-Path $ProjectRoot "MainWindow.xaml"

# Validate project structure
if (-not (Test-Path $csprojFile)) {
    Write-Error "Project file not found: $csprojFile"
    exit 1
}

if (-not (Test-Path $mainWindowFile)) {
    Write-Error "MainWindow.xaml not found: $mainWindowFile"
    exit 1
}

# Create template content
function Get-XamlTemplate {
    param([string]$ExampleName, [string]$ControlType)
    
    return @"
<UserControl
    x:Class="MosaicWpfDemo.Views.Examples.${ExampleName}Example"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:local="clr-namespace:MosaicWpfDemo.Views.Examples"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    d:DesignHeight="450"
    d:DesignWidth="800"
    mc:Ignorable="d">
    <Grid>
        <StackPanel Margin="20" Orientation="Vertical">
            <TextBlock
                Margin="0,0,0,10"
                FontSize="14"
                FontWeight="Bold"
                Foreground="{DynamicResource {x:Static SystemColors.ControlTextBrush}}"
                Text="${ControlType} Example" />
            
            <!-- TODO: Add your ${ControlType} control demonstration here -->
            <TextBlock Foreground="{DynamicResource {x:Static SystemColors.GrayTextBrush}}" Text="Add ${ControlType} examples here" />
        </StackPanel>
    </Grid>
</UserControl>
"@
}

function Get-CodeBehindTemplate {
    param([string]$ExampleName)
    
    return @"
/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ${ExampleName}Example
    {
        public ${ExampleName}Example()
        {
            InitializeComponent();
        }
    }
}
"@
}

function Get-MenuItemEntry {
    param([string]$ExampleName, [string]$ControlType, [string]$IconPath)
    
    return @"
                <!--  ${ExampleName}  -->
                <mosaic:SideMenuItem
                    ContentType="{x:Type views:ShellView}"
                    ContentTypeIsSingleton="True"
                    ImageSource="$IconPath"
                    Text="$ExampleName">
                    <mosaic:SideMenuItem.ParameterCollection>
                        <mosaic:SideMenuParameterCollection>
                            <mosaic:SideMenuParameter Key="Title" Value="$ExampleName" />
                            <mosaic:SideMenuParameter Key="XamlFile" Value="MosaicWpfDemo.LinkedSources.${ExampleName}Example.xaml" />
                            <mosaic:SideMenuParameter Key="CodeFile" Value="MosaicWpfDemo.LinkedSources.${ExampleName}Example.xaml.cs" />
                            <mosaic:SideMenuParameter Key="DocumentationType" Value="{x:Type mosaic:$ControlType}" />
                            <mosaic:SideMenuParameter Key="ExampleType" Value="{x:Type example:${ExampleName}Example}" />
                            <mosaic:SideMenuParameter Key="ImageSource" Value="$IconPath" />
                        </mosaic:SideMenuParameterCollection>
                    </mosaic:SideMenuItem.ParameterCollection>
                </mosaic:SideMenuItem>
"@
}

# Generate .csproj entries
$csprojXamlEntry = @"
		<EmbeddedResource Include="Views\Examples\${ExampleName}Example.xaml">
			<Link>LinkedSources\${ExampleName}Example.xaml</Link>
		</EmbeddedResource>
"@

$csprojCodeEntry = @"
		<EmbeddedResource Include="Views\Examples\${ExampleName}Example.xaml.cs">
			<Link>LinkedSources\${ExampleName}Example.xaml.cs</Link>
		</EmbeddedResource>
"@

# Generate XAML menu item entry using the function to ensure proper evaluation
$menuItemEntry = Get-MenuItemEntry -ExampleName $ExampleName -ControlType $ControlType -IconPath $IconPath

# Show preview
Write-Host "=== PREVIEW ===" -ForegroundColor Cyan
Write-Host ""

if ($CreateFiles -and (-not (Test-Path $xamlFile) -or -not (Test-Path $codeFile))) {
    Write-Host "Files to create:" -ForegroundColor Yellow
    if (-not (Test-Path $xamlFile)) {
        Write-Host "  + $xamlFile" -ForegroundColor Green
    }
    if (-not (Test-Path $codeFile)) {
        Write-Host "  + $codeFile" -ForegroundColor Green
    }
    Write-Host ""
}

Write-Host ".csproj entries to add:" -ForegroundColor Yellow
Write-Host $csprojXamlEntry
Write-Host $csprojCodeEntry
Write-Host ""

Write-Host "MainWindow.xaml menu item to add:" -ForegroundColor Yellow
Write-Host $menuItemEntry
Write-Host ""

if (-not $ApplyChanges) {
    if ($CreateFiles) {
        Write-Host "Run with -ApplyChanges flag to create files and update configuration" -ForegroundColor Green
    } else {
        Write-Host "Run with -CreateFiles -ApplyChanges flags to create files and update configuration" -ForegroundColor Green
    }
    exit 0
}

# Create example files if requested
if ($CreateFiles) {
    Write-Host "Creating example files..." -ForegroundColor Cyan
    
    if (-not (Test-Path $examplesDir)) {
        New-Item -ItemType Directory -Path $examplesDir | Out-Null
        Write-Host "✓ Created directory: $examplesDir" -ForegroundColor Green
    }
    
    $xamlTemplate = Get-XamlTemplate -ExampleName $ExampleName -ControlType $ControlType
    $codeTemplate = Get-CodeBehindTemplate -ExampleName $ExampleName
    
    if (Test-Path $xamlFile) {
        Write-Host "⚠ XAML file already exists, skipping: $xamlFile" -ForegroundColor Yellow
    } else {
        Set-Content -Path $xamlFile -Value $xamlTemplate -NoNewline
        Write-Host "✓ Created: $xamlFile" -ForegroundColor Green
    }
    
    if (Test-Path $codeFile) {
        Write-Host "⚠ Code-behind file already exists, skipping: $codeFile" -ForegroundColor Yellow
    } else {
        Set-Content -Path $codeFile -Value $codeTemplate -NoNewline
        Write-Host "✓ Created: $codeFile" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Updating project configuration..." -ForegroundColor Cyan

# Update .csproj
[xml]$csproj = Get-Content $csprojFile
$ns = $csproj.DocumentElement.NamespaceURI
$nsManager = New-Object System.Xml.XmlNamespaceManager($csproj.NameTable)
$nsManager.AddNamespace("msbuild", $ns)

# Find the last EmbeddedResource ItemGroup
$itemGroups = $csproj.SelectNodes("//msbuild:ItemGroup[msbuild:EmbeddedResource]", $nsManager)
if ($itemGroups.Count -eq 0) {
    Write-Error "Could not find EmbeddedResource ItemGroup in .csproj"
    exit 1
}

$lastItemGroup = $itemGroups[$itemGroups.Count - 1]

# Create XAML resource element
$xamlResource = $csproj.CreateElement("EmbeddedResource", $ns)
$xamlResource.SetAttribute("Include", "Views\Examples\${ExampleName}Example.xaml")
$xamlLink = $csproj.CreateElement("Link", $ns)
$xamlLink.InnerText = "LinkedSources\${ExampleName}Example.xaml"
$xamlResource.AppendChild($xamlLink) | Out-Null
$lastItemGroup.AppendChild($xamlResource) | Out-Null

# Create code-behind resource element
$codeResource = $csproj.CreateElement("EmbeddedResource", $ns)
$codeResource.SetAttribute("Include", "Views\Examples\${ExampleName}Example.xaml.cs")
$codeLink = $csproj.CreateElement("Link", $ns)
$codeLink.InnerText = "LinkedSources\${ExampleName}Example.xaml.cs"
$codeResource.AppendChild($codeLink) | Out-Null
$lastItemGroup.AppendChild($codeResource) | Out-Null

$csproj.Save($csprojFile)
Write-Host "✓ Updated: $csprojFile" -ForegroundColor Green

# Update MainWindow.xaml (append before closing SideMenu.MenuItems tag)
$mainWindowContent = Get-Content $mainWindowFile -Raw
$insertionPoint = $mainWindowContent.LastIndexOf("</mosaic:SideMenu.MenuItems>")

if ($insertionPoint -lt 0) {
    Write-Error "Could not find SideMenu.MenuItems closing tag in MainWindow.xaml"
    exit 1
}

# Insert the menu item with proper line breaks
$newContent = $mainWindowContent.Substring(0, $insertionPoint) + [Environment]::NewLine + $menuItemEntry + [Environment]::NewLine + $mainWindowContent.Substring($insertionPoint)
Set-Content -Path $mainWindowFile -Value $newContent -NoNewline -Encoding UTF8
Write-Host "✓ Updated: $mainWindowFile" -ForegroundColor Green

Write-Host ""
Write-Host "Done! Menu item and files created successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Build the project to verify compilation"
Write-Host "  2. Edit the XAML files to add your control demonstrations"
Write-Host "  3. Run the demo app and select your new menu item"