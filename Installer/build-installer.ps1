<#
    Builds the Vespidae Wars MSI.

    Run from anywhere:   powershell -ExecutionPolicy Bypass -File Installer\build-installer.ps1

    Expects the Unity player to already be built into Builds\VespidaeWars\ and an icon.ico to sit
    next to this script. Checks both before starting, because WiX's own errors for a missing source
    folder are not obvious.
#>

$ErrorActionPreference = 'Stop'

$installerDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir   = Split-Path -Parent $installerDir
$playerDir    = Join-Path $projectDir 'Builds\VespidaeWars'
$iconFile     = Join-Path $installerDir 'icon.ico'
$sourceFile   = Join-Path $installerDir 'VespidaeWars.wxs'
$outputFile   = Join-Path $installerDir 'VespidaeWars-Setup.msi'

Write-Host ''
Write-Host 'Vespidae Wars - MSI build' -ForegroundColor Cyan
Write-Host '=========================' -ForegroundColor Cyan

# --- prerequisites -------------------------------------------------------------------------------

$wix = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wix) {
    $toolPath = Join-Path $env:USERPROFILE '.dotnet\tools\wix.exe'
    if (Test-Path $toolPath) {
        $wix = $toolPath
    } else {
        Write-Host 'WiX is not installed. Install it with:' -ForegroundColor Red
        Write-Host '  dotnet tool install --global wix --version 5.0.2'
        Write-Host '  wix extension add -g WixToolset.UI.wixext/5.0.2'
        Write-Host '  wix extension add -g WixToolset.Util.wixext/5.0.2'
        exit 1
    }
} else {
    $wix = $wix.Source
}

if (-not (Test-Path $playerDir)) {
    Write-Host "No Unity build found at: $playerDir" -ForegroundColor Red
    Write-Host 'Build the game from Unity first (File > Build Settings > Build).'
    exit 1
}

$exe = Join-Path $playerDir 'VespidaeWars.exe'
if (-not (Test-Path $exe)) {
    Write-Host "VespidaeWars.exe is missing from $playerDir" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $iconFile)) {
    Write-Host "No icon.ico found at: $iconFile" -ForegroundColor Red
    Write-Host 'Save the game icon there as a true .ico (256/48/32/16 px inside one file).'
    exit 1
}

$sizeMb = [math]::Round(((Get-ChildItem $playerDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1)
Write-Host ''
Write-Host "  player folder : $playerDir"
Write-Host "  payload size  : $sizeMb MB"
Write-Host "  icon          : $iconFile"
Write-Host "  output        : $outputFile"
Write-Host ''
Write-Host 'Compressing. A payload this size takes several minutes.' -ForegroundColor Yellow
Write-Host ''

# --- build ---------------------------------------------------------------------------------------

$started = Get-Date

# Paths inside the .wxs - icon.ico, License.rtf, ..\Builds - are resolved against the working
# directory, not against the .wxs file. Run from this folder so the script works no matter where it
# is invoked from.
Push-Location $installerDir
try {
    & $wix build $sourceFile -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext -arch x64 -o $outputFile
} finally {
    Pop-Location
}

if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host "WiX failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}

$elapsed = (Get-Date) - $started
$msiMb = [math]::Round(((Get-Item $outputFile).Length / 1MB), 1)

Write-Host ''
Write-Host 'Done.' -ForegroundColor Green
Write-Host "  $outputFile"
Write-Host "  $msiMb MB, built in $([math]::Round($elapsed.TotalMinutes, 1)) min"
Write-Host ''
Write-Host 'Unsigned installers trip Windows SmartScreen: More info > Run anyway.' -ForegroundColor DarkGray
