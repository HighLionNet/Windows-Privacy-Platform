#Requires -Version 5.1
# Builds and optionally launches the checkout containing this script.
# Synchronization is fast-forward-only and never discards local changes.
[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [switch]$NoSync,
    [switch]$NoLaunch
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = $PSScriptRoot
}
$repositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$projectPath = Join-Path $repositoryRoot "Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj"
$runPath = Join-Path $repositoryRoot ".artifacts\run"
$executablePath = Join-Path $runPath "WindowsPrivacyPlatform.exe"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Application project not found beneath the repository root. Clone the repository, then run this script from that checkout."
}

if (-not $NoSync) {
    $remote = (& git -C $repositoryRoot remote get-url origin 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($remote)) {
        throw "No origin remote is configured. Use -NoSync to build this checkout without pulling."
    }

    $pending = (& git -C $repositoryRoot status --porcelain)
    if (-not [string]::IsNullOrWhiteSpace(($pending -join ""))) {
        throw "The checkout has local changes. Commit or stash them, or use -NoSync; nothing was discarded."
    }

    & git -C $repositoryRoot pull --ff-only
    if ($LASTEXITCODE -ne 0) {
        throw "Fast-forward synchronization failed; the checkout was left intact."
    }
}

$resolvedRunPath = [System.IO.Path]::GetFullPath($runPath)
$resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($repositoryRoot)
if (-not $resolvedRunPath.StartsWith($resolvedRepositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to replace build output outside the repository."
}

if (Test-Path -LiteralPath $runPath) {
    Remove-Item -LiteralPath $runPath -Recurse -Force
}
New-Item -ItemType Directory -Path $runPath -Force | Out-Null

& dotnet publish $projectPath -c Release -r win-x64 --self-contained false -o $runPath --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }
if (-not (Test-Path -LiteralPath $executablePath)) { throw "Published executable is missing." }

Write-Host "Ready: $executablePath"
if (-not $NoLaunch) {
    Start-Process -FilePath $executablePath -WorkingDirectory $runPath
}
