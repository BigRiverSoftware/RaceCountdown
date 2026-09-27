<#
.SYNOPSIS
    Builds the Windows app and registers it as a development (loose-file) MSIX package, then launches it.

.DESCRIPTION
    `dotnet build -t:Run` starts the exe unpackaged, which fails for an MSIX app. Visual Studio's F5 deploys a
    layout from the build's .appxrecipe; this script does the same from the command line so the packaged app
    (and, from Phase 5, its widget provider) can be tested without Visual Studio. Needs Developer Mode.

    Remove with:  Get-AppxPackage BigRiverSoftware.BathurstCountdown | Remove-AppxPackage
#>
param(
    [ValidateSet('Debug', 'Release')] [string] $Configuration = 'Debug',
    [switch] $NoLaunch
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path "$PSScriptRoot\..\.."
$project = Join-Path $root 'src\RaceCountdown\RaceCountdown.csproj'
$tfm = 'net10.0-windows10.0.19041.0'

dotnet build $project -f $tfm -c $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$bin = Join-Path $root "src\RaceCountdown\bin\$Configuration\$tfm\win-x64"
$recipe = Join-Path $bin 'RaceCountdown.build.appxrecipe'
$layout = Join-Path $bin 'AppxLayout'

# Copy every file the recipe puts in the package (including the manifest) to its package path.
[xml] $xml = Get-Content $recipe
$ns = @{ m = 'http://schemas.microsoft.com/developer/msbuild/2003' }
$files = Select-Xml -Xml $xml -Namespace $ns -XPath '//m:AppxPackagedFile | //m:AppXManifest' | ForEach-Object { $_.Node }

Get-AppxPackage BigRiverSoftware.BathurstCountdown | Remove-AppxPackage
if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
foreach ($file in $files) {
    $target = Join-Path $layout $file.PackagePath
    New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
    Copy-Item $file.Include $target -Force
}

Add-AppxPackage -Register (Join-Path $layout 'AppxManifest.xml') -ForceApplicationShutdown
$package = Get-AppxPackage BigRiverSoftware.BathurstCountdown
Write-Host "Registered $($package.PackageFullName) from $layout"

if (-not $NoLaunch) {
    Start-Process "shell:AppsFolder\$($package.PackageFamilyName)!App"
}
