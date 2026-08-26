# Windows Privacy Platform

<img src="Source/WindowsPrivacyPlatform.App/Assets/WindowsPrivacyPlatform.png" alt="Windows Privacy Platform shield mark" width="128" height="128">

Windows Privacy Platform is a focused, local privacy and security policy hub for Windows 10 and Windows 11. It makes the settings that affect app access, data sharing, Defender, Firewall, networking, remote access, and Microsoft apps understandable without becoming a generic registry editor or diagnostics suite.

The operating rule is simple: show only relevant, editable policy in Settings; observe the wider operating system in a separate, read-only System Explorer; deny every write that is not explicitly authorized.

## What the current release delivers

- A concise **Privacy & Security Overview** that identifies high-attention policies, review items, and protections actually observed. It never treats unknown values as safe and does not invent a synthetic score.
- Focused **Settings** categories containing only editable privacy and security policy.
- A fast, searchable, purpose-grouped **System Explorer** for read-only services, tasks, packages, Windows features, capabilities, and firewall rules.
- Clear evidence states for configured, not configured, not observed, unsupported, access denied, and unknown results.
- Direct registry/policy paths, compact descriptions, and plain-language option effects.
- Edition/build applicability for Windows 10 and Windows 11 rather than pretending every control exists everywhere.
- Curated, independently verified registry-policy changes, including the twelve bounded firewall-profile controls.
- No service, scheduled-task, package, optional-feature, BitLocker, UAC, or arbitrary firewall-rule mutation.
- No application telemetry, cloud account, bulk hardening profile, generic command runner, background agent, or remediation scripts.

## Run a downloaded release

1. Install the [.NET 8 Desktop Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if it is not already present.
2. Download `WindowsPrivacyPlatform-win-x64.zip` from the repository's latest GitHub Release.
3. Extract the entire archive to a permanent folder. Do not run the executable from inside the zip.
4. Open `WindowsPrivacyPlatform.exe` and choose **Inspect** or **Modify**. Modify mode requests Windows elevation for machine policy.
5. Accept the one-time shortcut offer if you want Desktop and Start Menu entries that point to the extracted executable.

The archive includes `START_HERE.md` with the same deployment guidance. There is no installer and the release does not silently persist services, drivers, or background agents.

## Safety contract

Every writable object is deny-by-default. Discovery data never grants mutation rights. A change must have a complete typed target in a source-controlled allowlist and follows:

```text
pre-read → explicit confirmation → typed operation → independent read-back → local audit
```

Failure at any stage is reported as failure; a textual match with the wrong registry type is not accepted. System Explorer is always read-only, and the modification engine rejects every non-registry target. See [Status/Safety_Model.md](Status/Safety_Model.md) for the full contract.

## Build from source

Requirements: Windows, PowerShell, and the .NET 8 SDK.

```powershell
dotnet restore .\Source\WindowsPrivacyPlatform.sln
dotnet build .\Source\WindowsPrivacyPlatform.sln -c Release --no-restore
dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release --no-build
.\scripts\build-release.ps1
```

The release archive is written beneath `.artifacts\release`. `sync-run.ps1` is a safe fast-forward-only convenience workflow; it refuses to discard a dirty checkout.

## Optional Authenticode signing

Release builds remain reproducible without a certificate and print a clear unsigned warning. To sign the published executable, provide either a PFX path or a machine-store thumbprint:

```powershell
$env:WPP_SIGN_CERT_PATH = 'D:\secure\code-signing.pfx'
$env:WPP_SIGN_CERT_PASSWORD = '<secret>'
# Or: $env:WPP_SIGN_CERT_THUMBPRINT = '<sha1-thumbprint>'
.\scripts\build-release.ps1
```

GitHub Actions also accepts `WPP_SIGN_CERT_BASE64`, `WPP_SIGN_CERT_PATH`, `WPP_SIGN_CERT_THUMBPRINT`, and `WPP_SIGN_CERT_PASSWORD` as repository secrets. No certificate material is stored in this repository.

## Local data

Window preferences, the one-time shortcut decision, and local audit logs are stored beneath `%LocalAppData%\WindowsPrivacyPlatform`. The app does not send them anywhere.

See [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md), and [Status/Architecture.md](Status/Architecture.md) for engineering and security details.

Maintained by HighLionNet. [Project repository](https://github.com/HighLionNet/Windows-Privacy-Platform).
