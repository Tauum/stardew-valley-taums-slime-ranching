param(
    [string]$ModsPath = 'E:\Games\Steam\steamapps\common\Stardew Valley\Mods'
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$buildOutput = Join-Path $projectRoot 'bin\Debug\net6.0'
$modFolder = Join-Path $ModsPath 'taums-slime-ranching'

if (-not (Test-Path -LiteralPath $ModsPath -PathType Container)) {
    throw "Stardew Mods folder not found: $ModsPath"
}

$files = @(
    @{ Source = (Join-Path $projectRoot 'manifest.json'); Name = 'manifest.json' },
    @{ Source = (Join-Path $buildOutput 'taums-slime-ranching.dll'); Name = 'taums-slime-ranching.dll' },
    @{ Source = (Join-Path $buildOutput 'taums-slime-ranching.deps.json'); Name = 'taums-slime-ranching.deps.json' }
)

foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath $file.Source -PathType Leaf)) {
        throw "Required mod file not found: $($file.Source). Run the rebuild job first."
    }
}

New-Item -ItemType Directory -Path $modFolder -Force | Out-Null

foreach ($file in $files) {
    Copy-Item -LiteralPath $file.Source -Destination (Join-Path $modFolder $file.Name) -Force
}

$assetDirectory = Join-Path $projectRoot 'assets'
if (Test-Path -LiteralPath $assetDirectory -PathType Container) {
    Copy-Item -LiteralPath $assetDirectory -Destination $modFolder -Recurse -Force
}

$installedConfig = Join-Path $modFolder 'config.json'
if (-not (Test-Path -LiteralPath $installedConfig)) {
    Copy-Item -LiteralPath (Join-Path $projectRoot 'config.json') -Destination $installedConfig
}

Write-Output "Copied the mod to $modFolder. Existing config.json and data were preserved."
