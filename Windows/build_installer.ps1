param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "Windows\R2Trans.Windows\R2Trans.Windows.csproj"
$publishDir = Join-Path $root "Windows\publish\$Runtime"
$distDir = Join-Path $root "Windows\dist"

if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -o $publishDir

if ($SkipInstaller) {
    Write-Host "Published app: $publishDir"
    return
}

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) }

if (-not $isccCandidates) {
    throw "Inno Setup 6 was not found. Install it from https://jrsoftware.org/isdl.php or rerun with -SkipInstaller."
}

$env:R2TRANS_PUBLISH_DIR = $publishDir
& $isccCandidates[0] (Join-Path $root "Windows\Installer\R2Trans.iss")

Write-Host "Installer output: $distDir"
