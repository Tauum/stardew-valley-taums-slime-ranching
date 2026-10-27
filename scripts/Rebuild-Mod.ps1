$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot 'taums-slime-ranching.csproj'

& dotnet build $projectFile -t:Rebuild -p:EnableModDeploy=false -p:EnableModZip=false
if ($LASTEXITCODE -ne 0) {
    throw "Mod rebuild failed with exit code $LASTEXITCODE."
}
