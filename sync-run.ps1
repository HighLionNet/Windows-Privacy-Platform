#Requires -Version 5.1
# Fully sync Windows Privacy Platform from GitHub and run the newest build.
# Fixed launch path: <repo-root>\run\WindowsPrivacyPlatform.exe
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\sync-run.ps1
#   powershell -ExecutionPolicy Bypass -File .\sync-run.ps1 -NoLaunch

param(
    [string]$RepoRoot = $PSScriptRoot,
    [switch]$NoLaunch,
    [switch]$KeepLocalChanges
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host ("=== " + $msg + " ===") -ForegroundColor Cyan
}
function Write-Ok([string]$msg) {
    Write-Host ("  OK  " + $msg) -ForegroundColor Green
}
function Write-Warn([string]$msg) {
    Write-Host ("  WARN  " + $msg) -ForegroundColor Yellow
}
function Write-Fail([string]$msg) {
    Write-Host ("  FAIL  " + $msg) -ForegroundColor Red
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = "C:\Windows Privacy Platform"
}
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$SourceDir = Join-Path $RepoRoot "Source"
$Sln = Join-Path $SourceDir "WindowsPrivacyPlatform.sln"
$AppCsproj = Join-Path $SourceDir "WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj"
$RunDir = Join-Path $RepoRoot "run"
$ExePath = Join-Path $RunDir "WindowsPrivacyPlatform.exe"

Write-Host "Windows Privacy Platform - sync + run" -ForegroundColor White
Write-Host ("Repo: " + $RepoRoot)

if (-not (Test-Path -LiteralPath $Sln)) {
    Write-Fail ("Solution not found: " + $Sln)
    Write-Host "Clone first:"
    Write-Host '  git clone https://github.com/HighLionNet/Windows-Privacy-Platform.git "C:\Windows Privacy Platform"'
    exit 1
}
if (-not (Test-Path -LiteralPath $AppCsproj)) {
    Write-Fail ("App project not found: " + $AppCsproj)
    exit 1
}

# 1) Stop running instances so files are not locked
Write-Step "Stop running WPP processes"
$procs = @(Get-Process -Name "WindowsPrivacyPlatform" -ErrorAction SilentlyContinue)
if ($procs.Count -gt 0) {
    foreach ($p in $procs) {
        Write-Host ("  Stopping PID " + $p.Id + " ...")
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 1
    Write-Ok ("Stopped " + $procs.Count + " process(es)")
}
else {
    Write-Ok "No running process"
}

# 2) Sync git to origin/main
Write-Step "Sync git to origin/main"
Push-Location $RepoRoot
try {
    git rev-parse --is-inside-work-tree 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw ("Not a git repository: " + $RepoRoot)
    }

    git fetch origin
    if ($LASTEXITCODE -ne 0) { throw "git fetch origin failed" }

    $branch = (git rev-parse --abbrev-ref HEAD).Trim()
    if ($branch -ne "main") {
        Write-Warn ("On branch '" + $branch + "' - checking out main")
        git checkout main
        if ($LASTEXITCODE -ne 0) { throw "git checkout main failed" }
    }

    if (-not $KeepLocalChanges) {
        git reset --hard origin/main
        if ($LASTEXITCODE -ne 0) { throw "git reset --hard origin/main failed" }
        git clean -fd
        if ($LASTEXITCODE -ne 0) { throw "git clean -fd failed" }
        Write-Ok "Hard-reset to origin/main (local changes discarded)"
    }
    else {
        git pull --ff-only origin main
        if ($LASTEXITCODE -ne 0) { throw "git pull --ff-only failed (local changes may block pull)" }
        Write-Ok "Fast-forward pull to origin/main"
    }

    $sha = (git rev-parse --short HEAD).Trim()
    $msg = (git log -1 --pretty=format:"%s").Trim()
    Write-Ok ("HEAD = " + $sha + " - " + $msg)
}
finally {
    Pop-Location
}

# 3) Clean old outputs
Write-Step "Clean previous build outputs"
if (Test-Path -LiteralPath $RunDir) {
    Remove-Item -LiteralPath $RunDir -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $RunDir -Force | Out-Null

Get-ChildItem -Path $SourceDir -Directory -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -eq "bin" -or $_.Name -eq "obj" } |
    ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
Write-Ok "Cleaned bin/obj and run folder"

# 4) Publish Release win-x64 into run folder
Write-Step "Publish Release (win-x64) to run folder"
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCmd) {
    Write-Fail "dotnet SDK not found on PATH. Install .NET 8 SDK."
    exit 1
}

& dotnet publish $AppCsproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o $RunDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Fail ("dotnet publish failed (exit " + $LASTEXITCODE + ")")
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $ExePath)) {
    Write-Fail ("Expected binary missing after publish: " + $ExePath)
    exit 1
}

$fi = Get-Item -LiteralPath $ExePath
Write-Ok ("Published: " + $fi.FullName)
Write-Ok ("Size: " + [math]::Round($fi.Length / 1KB, 1) + " KB  Modified: " + $fi.LastWriteTime)

try {
    $ver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ExePath)
    if ($ver.ProductVersion) { Write-Ok ("ProductVersion: " + $ver.ProductVersion) }
    if ($ver.FileVersion) { Write-Ok ("FileVersion: " + $ver.FileVersion) }
}
catch {
    # version info is optional
}

# 5) Launch
if ($NoLaunch) {
    Write-Step "Done (NoLaunch)"
    Write-Host "Run manually:" -ForegroundColor White
    Write-Host ("  " + $ExePath)
    exit 0
}

Write-Step "Launch"
Start-Process -FilePath $ExePath -WorkingDirectory $RunDir
Write-Ok ("Started: " + $ExePath)
Write-Host ""
Write-Host "Shortcut target for next time:" -ForegroundColor White
Write-Host ("  " + $ExePath)
Write-Host ""
Write-Host "Re-run this script after every git update to guarantee the newest build." -ForegroundColor DarkGray
