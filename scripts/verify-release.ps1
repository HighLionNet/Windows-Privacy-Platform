#Requires -Version 5.1
[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$ArchivePath)

$ErrorActionPreference = 'Stop'
$archive = (Resolve-Path -LiteralPath $ArchivePath).Path
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try {
    if ($zip.Entries.Count -eq 0 -or $zip.Entries.Count -gt 500) { throw 'Archive entry count is outside the release boundary.' }
    $required = @('WindowsPrivacyPlatform.exe', 'WindowsPrivacyPlatform.dll', 'START_HERE.md', 'RELEASE_MANIFEST.txt')
    $names = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($entry in $zip.Entries) {
        $name = $entry.FullName.Replace('\', '/')
        if ([string]::IsNullOrWhiteSpace($name) -or [System.IO.Path]::IsPathRooted($name) -or $name -match '(^|/)\.\.(/|$)') {
            throw "Unsafe archive path: $name"
        }
        if ($entry.Length -gt 268435456) { throw "Unexpectedly large archive entry: $name" }
        $extension = [System.IO.Path]::GetExtension($name).ToLowerInvariant()
        if ($extension -in @('.pdb', '.pfx', '.p12', '.snk', '.cs', '.xaml', '.ps1', '.user', '.suo')) {
            throw "Development or secret-bearing file type in archive: $name"
        }
        if ([System.IO.Path]::GetFileName($name) -match '(?i)(secret|credential|password|token)') {
            throw "Secret-like filename in archive: $name"
        }
    }
    foreach ($name in $required) {
        if ($names -notcontains $name) { throw "Required release file missing: $name" }
    }
}
finally { $zip.Dispose() }

$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
Write-Host "Release archive static verification passed: $($names.Count) files; SHA256 $hash"
