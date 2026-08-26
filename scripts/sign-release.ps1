#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ExecutablePath
)

$ErrorActionPreference = "Stop"
$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$certificatePath = $env:WPP_SIGN_CERT_PATH
$certificateThumbprint = $env:WPP_SIGN_CERT_THUMBPRINT
$certificatePassword = $env:WPP_SIGN_CERT_PASSWORD

if ([string]::IsNullOrWhiteSpace($certificatePath) -and
    [string]::IsNullOrWhiteSpace($certificateThumbprint)) {
    Write-Warning "Authenticode signing skipped: no certificate path or thumbprint was provided."
    return
}

$kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
$signTool = Get-ChildItem -LiteralPath $kitsRoot -Filter "signtool.exe" -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "\\x64\\signtool\.exe$" } |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if ($null -eq $signTool) {
    throw "signtool.exe was not found. Install the Windows SDK before enabling signing."
}

$arguments = @("sign", "/fd", "SHA256", "/td", "SHA256", "/tr", "http://timestamp.digicert.com")
if (-not [string]::IsNullOrWhiteSpace($certificatePath)) {
    $resolvedCertificate = (Resolve-Path -LiteralPath $certificatePath).Path
    $arguments += @("/f", $resolvedCertificate)
    if (-not [string]::IsNullOrWhiteSpace($certificatePassword)) {
        $arguments += @("/p", $certificatePassword)
    }
}
else {
    $arguments += @("/sha1", $certificateThumbprint, "/sm")
}
$arguments += $resolvedExecutable

& $signTool.FullName @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Authenticode signing failed with exit code $LASTEXITCODE."
}

& $signTool.FullName verify /pa /all $resolvedExecutable
if ($LASTEXITCODE -ne 0) {
    throw "Authenticode signature verification failed with exit code $LASTEXITCODE."
}

Write-Host "Authenticode signature verified: $resolvedExecutable"
