param(
    [string]$ModsPath = 'E:\Games\Steam\steamapps\common\Stardew Valley\Mods',
    [switch]$SkipGameLaunch
)

$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'Rebuild-Mod.ps1')
& (Join-Path $PSScriptRoot 'Copy-Mod.ps1') -ModsPath $ModsPath

if ($SkipGameLaunch) {
    Write-Output 'Game launch skipped.'
    return
}

$gameDirectory = Split-Path -Parent (Resolve-Path -LiteralPath $ModsPath).Path
$smapiExe = Join-Path $gameDirectory 'StardewModdingAPI.exe'
if (-not (Test-Path -LiteralPath $smapiExe -PathType Leaf)) {
    throw "SMAPI executable not found: $smapiExe"
}

Write-Output "Launching Stardew Valley through SMAPI: $smapiExe"
Push-Location -LiteralPath $gameDirectory
try {
    & $smapiExe
    $gameExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($gameExitCode -ne 0) {
    throw "Stardew Valley through SMAPI exited with code $gameExitCode."
}
