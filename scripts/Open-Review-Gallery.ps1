$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$generator = Join-Path $PSScriptRoot 'Generate-Review-Gallery.ps1'
$galleryPath = Join-Path $projectRoot 'preview\gallery.html'

& $generator
if (-not (Test-Path -LiteralPath $galleryPath -PathType Leaf)) {
    throw "Review gallery was not generated: $galleryPath"
}

Start-Process -FilePath $galleryPath
Write-Output "Opened the offline review gallery: $galleryPath"
