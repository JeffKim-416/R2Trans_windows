param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$InnoSetupCompiler,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "Windows\R2Trans.Windows\R2Trans.Windows.csproj"
$publishDir = Join-Path $root "Windows\publish\$Runtime"
$distDir = Join-Path $root "Windows\dist"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found. Install .NET 8 SDK for Windows Desktop builds."
}

[xml]$projectXml = Get-Content $project
$appVersion = ($projectXml.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if ([string]::IsNullOrWhiteSpace($appVersion)) {
    $appVersion = "0.1.0"
}

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
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

Write-Host "Published app: $publishDir"

if ($SkipInstaller) {
    return
}

if ($InnoSetupCompiler) {
    $isccCandidates = @($InnoSetupCompiler) | Where-Object { Test-Path $_ }
}
else {
    $isccOnPath = Get-Command iscc.exe -ErrorAction SilentlyContinue
    $isccCandidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        $isccOnPath.Source
    ) | Where-Object { $_ -and (Test-Path $_) }
}

if (-not $isccCandidates) {
    throw "Inno Setup 6 was not found. Install it from https://jrsoftware.org/isdl.php or rerun with -SkipInstaller."
}

$isccCompiler = @($isccCandidates)[0]
$env:R2TRANS_PUBLISH_DIR = $publishDir
$env:R2TRANS_APP_VERSION = $appVersion
$env:R2TRANS_RUNTIME = $Runtime
& $isccCompiler (Join-Path $root "Windows\Installer\R2Trans.iss")

Write-Host "Installer output: $distDir\R2TransSetup-$appVersion-$Runtime.exe"
