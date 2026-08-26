#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}
$repositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$projectPath = Join-Path $repositoryRoot "Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj"
$artifactRoot = Join-Path $repositoryRoot ".artifacts\release"
$publishPath = Join-Path $artifactRoot "publish"
$zipPath = Join-Path $artifactRoot "WindowsPrivacyPlatform-win-x64.zip"
$checksumPath = "$zipPath.sha256"
$startHerePath = Join-Path $repositoryRoot "RELEASE_README.md"
$executablePath = Join-Path $publishPath "WindowsPrivacyPlatform.exe"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Application project not found beneath the repository root."
}
if (-not (Test-Path -LiteralPath $startHerePath)) {
    throw "Release instructions are missing."
}

$resolvedArtifactRoot = [System.IO.Path]::GetFullPath($artifactRoot)
$resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($repositoryRoot)
if (-not $resolvedArtifactRoot.StartsWith($resolvedRepositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to write release output outside the repository."
}

if (Test-Path -LiteralPath $artifactRoot) {
    Remove-Item -LiteralPath $artifactRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

& dotnet restore (Join-Path $repositoryRoot "Source\WindowsPrivacyPlatform.sln")
if ($LASTEXITCODE -ne 0) { throw "Restore failed with exit code $LASTEXITCODE." }

$sourceRevision = "local"
if (Get-Command git -ErrorAction SilentlyContinue) {
    $candidate = (& git -C $repositoryRoot rev-parse --short=12 HEAD 2>$null)
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($candidate)) {
        $sourceRevision = $candidate.Trim()
    }
}

& dotnet publish $projectPath -c Release -r win-x64 --self-contained false `
    -p:SourceRevisionId=$sourceRevision -o $publishPath --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }

& (Join-Path $PSScriptRoot "sign-release.ps1") -ExecutablePath $executablePath

Copy-Item -LiteralPath $startHerePath -Destination (Join-Path $publishPath "START_HERE.md")
Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $zipPath -CompressionLevel Optimal

if (-not (Test-Path -LiteralPath $zipPath)) {
    throw "Release archive was not created."
}

$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
Set-Content -LiteralPath $checksumPath -Value "$($hash.Hash)  WindowsPrivacyPlatform-win-x64.zip" -Encoding Ascii
Write-Host "Release archive: $zipPath"
Write-Host "Checksum file: $checksumPath"
Write-Host "SHA256: $($hash.Hash)"
