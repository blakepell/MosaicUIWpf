<#
.SYNOPSIS
    Interactive menu for running the publish profiles found in this solution.

.DESCRIPTION
    Scans the solution folder for */Properties/PublishProfiles/*.pubxml files and presents
    them in an arrow-key navigable menu. Selecting an entry runs 'dotnet publish' for that
    project using the chosen profile, then returns to the menu with a success/failure result.

.NOTES
    Requires the .NET SDK on the PATH. Run with:  .\publish.ps1

    This file is intentionally pure ASCII. Windows PowerShell 5.1 decodes BOM-less files as
    ANSI, which mangles literal box-drawing characters -- and can turn bytes such as 0x94
    (part of the UTF-8 encoding of a check mark) into a curly quote that breaks the parser
    and makes it echo raw script text. The glyphs below are built from char codes instead.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# Try to switch the console to UTF-8 so the box-drawing glyphs render; fall back to ASCII.
$useUnicode = $false
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $useUnicode = $true
} catch { }

if ($useUnicode) {
    $g = @{
        TopLeft     = [string][char]0x2554
        TopRight    = [string][char]0x2557
        BottomLeft  = [string][char]0x255A
        BottomRight = [string][char]0x255D
        Horizontal  = [string][char]0x2550
        Vertical    = [string][char]0x2551
        SplitLeft   = [string][char]0x2560
        SplitRight  = [string][char]0x2563
        LightLeft   = [string][char]0x255F
        LightRight  = [string][char]0x2562
        LightLine   = [string][char]0x2500
        Marker      = [string][char]0x25BA
        Check       = [string][char]0x2714
        Cross       = [string][char]0x2718
    }
} else {
    $g = @{
        TopLeft = '+'; TopRight = '+'; BottomLeft = '+'; BottomRight = '+'
        Horizontal = '='; Vertical = '|'
        SplitLeft = '+'; SplitRight = '+'
        LightLeft = '+'; LightRight = '+'; LightLine = '-'
        Marker = '>'; Check = '[OK]'; Cross = '[!!]'
    }
}

function Get-PublishProfiles {
    param([string]$Path)

    Get-ChildItem -Path $Path -Recurse -Filter '*.pubxml' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -like '*\PublishProfiles' -and $_.FullName -notlike '*\node_modules\*' } |
        ForEach-Object {
            $profileFile = $_

            # ...\<ProjectDir>\Properties\PublishProfiles\<Name>.pubxml  ->  <ProjectDir>
            $projectDir = Split-Path (Split-Path $profileFile.DirectoryName -Parent) -Parent
            $project = Get-ChildItem -Path $projectDir -Filter '*.csproj' -File -ErrorAction SilentlyContinue |
                Select-Object -First 1

            if ($null -eq $project) { return }

            $publishDir = $null
            try {
                $xml = [xml](Get-Content -Path $profileFile.FullName -Raw)
                $publishDir = $xml.Project.PropertyGroup.PublishDir | Select-Object -First 1
            } catch { }

            [pscustomobject]@{
                ProjectName  = [System.IO.Path]::GetFileNameWithoutExtension($project.Name)
                ProjectPath  = $project.FullName
                ProfileName  = [System.IO.Path]::GetFileNameWithoutExtension($profileFile.Name)
                ProfilePath  = $profileFile.FullName
                PublishDir   = $publishDir
            }
        } |
        Sort-Object ProjectName, ProfileName
}

function Write-Menu {
    param(
        [object[]]$Items,
        [int]$SelectedIndex,
        [object]$LastResult
    )

    $title = 'Publish Profiles'
    $labels = @()
    foreach ($item in $Items) {
        $labels += ('{0}  ->  {1}' -f $item.ProjectName, $item.ProfileName)
    }
    $labels += 'Quit'

    # Width is driven by the longest line we need to display.
    $width = ($labels | Measure-Object -Property Length -Maximum).Maximum + 6
    if ($width -lt ($title.Length + 6)) { $width = $title.Length + 6 }

    $footer = 'Up/Down: move   Enter: publish   Q/Esc: quit'
    if ($width -lt ($footer.Length + 4)) { $width = $footer.Length + 4 }

    $inner = $width - 2

    Clear-Host
    Write-Host ($g.TopLeft + ($g.Horizontal * $inner) + $g.TopRight) -ForegroundColor DarkCyan
    Write-Host ($g.Vertical + ' ') -NoNewline -ForegroundColor DarkCyan
    Write-Host $title.PadRight($inner - 2) -NoNewline -ForegroundColor Cyan
    Write-Host (' ' + $g.Vertical) -ForegroundColor DarkCyan
    Write-Host ($g.SplitLeft + ($g.Horizontal * $inner) + $g.SplitRight) -ForegroundColor DarkCyan

    for ($i = 0; $i -lt $labels.Count; $i++) {
        if ($i -eq $Items.Count) {
            # Separator above the Quit entry.
            Write-Host ($g.LightLeft + ($g.LightLine * $inner) + $g.LightRight) -ForegroundColor DarkCyan
        }

        $selected = ($i -eq $SelectedIndex)
        $marker = if ($selected) { $g.Marker } else { ' ' }
        $text = ' {0} {1}' -f $marker, $labels[$i]

        Write-Host $g.Vertical -NoNewline -ForegroundColor DarkCyan
        if ($selected) {
            Write-Host $text.PadRight($inner) -NoNewline -ForegroundColor Black -BackgroundColor Cyan
        } else {
            Write-Host $text.PadRight($inner) -NoNewline -ForegroundColor Gray
        }
        Write-Host $g.Vertical -ForegroundColor DarkCyan
    }

    Write-Host ($g.BottomLeft + ($g.Horizontal * $inner) + $g.BottomRight) -ForegroundColor DarkCyan
    Write-Host ''
    Write-Host ('  ' + $footer) -ForegroundColor DarkGray

    if ($null -ne $LastResult) {
        Write-Host ''
        if ($LastResult.Success) {
            $msg = '  {0} {1} [{2}] published successfully in {3:N1}s.' -f $g.Check,
                $LastResult.ProjectName, $LastResult.ProfileName, $LastResult.Seconds
            Write-Host $msg -ForegroundColor Green
        } else {
            $msg = '  {0} {1} [{2}] FAILED (exit code {3}) after {4:N1}s.' -f $g.Cross,
                $LastResult.ProjectName, $LastResult.ProfileName, $LastResult.ExitCode, $LastResult.Seconds
            Write-Host $msg -ForegroundColor Red
        }
    }
}

function Invoke-PublishProfile {
    param([object]$Item)

    Clear-Host
    Write-Host ('Publishing {0} using profile "{1}"...' -f $Item.ProjectName, $Item.ProfileName) -ForegroundColor Cyan
    if ($Item.PublishDir) {
        Write-Host ('Output: {0}' -f $Item.PublishDir) -ForegroundColor DarkGray
    }
    Write-Host ''

    $start = Get-Date
    & dotnet publish $Item.ProjectPath "/p:PublishProfile=$($Item.ProfilePath)"
    $exitCode = $LASTEXITCODE
    $elapsed = ((Get-Date) - $start).TotalSeconds

    Write-Host ''
    if ($exitCode -eq 0) {
        Write-Host ('{0} Publish succeeded in {1:N1}s.' -f $g.Check, $elapsed) -ForegroundColor Green
    } else {
        Write-Host ('{0} Publish FAILED with exit code {1} after {2:N1}s.' -f $g.Cross, $exitCode, $elapsed) -ForegroundColor Red
    }

    Write-Host ''
    Write-Host 'Press any key to return to the menu...' -ForegroundColor DarkGray
    [Console]::ReadKey($true) | Out-Null

    [pscustomobject]@{
        ProjectName = $Item.ProjectName
        ProfileName = $Item.ProfileName
        Success     = ($exitCode -eq 0)
        ExitCode    = $exitCode
        Seconds     = $elapsed
    }
}

$profiles = @(Get-PublishProfiles -Path $root)

if ($profiles.Count -eq 0) {
    Write-Host 'No publish profiles (*.pubxml) were found in this solution.' -ForegroundColor Yellow
    return
}

$selected = 0
$quitIndex = $profiles.Count
$lastResult = $null

while ($true) {
    Write-Menu -Items $profiles -SelectedIndex $selected -LastResult $lastResult

    $key = [Console]::ReadKey($true)

    switch ($key.Key) {
        'UpArrow' {
            $selected--
            if ($selected -lt 0) { $selected = $quitIndex }
        }
        'DownArrow' {
            $selected++
            if ($selected -gt $quitIndex) { $selected = 0 }
        }
        'Home'   { $selected = 0 }
        'End'    { $selected = $quitIndex }
        'Escape' { Clear-Host; return }
        'Q'      { Clear-Host; return }
        'Enter'  {
            if ($selected -eq $quitIndex) {
                Clear-Host
                return
            }

            $lastResult = Invoke-PublishProfile -Item $profiles[$selected]
        }
    }
}
