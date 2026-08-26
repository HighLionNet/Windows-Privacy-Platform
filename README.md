# Windows Privacy Platform

<img src="Source/WindowsPrivacyPlatform.App/Assets/WindowsPrivacyPlatform.png" alt="Windows Privacy Platform shield mark" width="128" height="128">

Windows Privacy Platform is a local Windows inspection and configuration console. It correlates privacy preferences, administrative policy, endpoint-security controls, services, scheduled tasks, packages, optional features, capabilities, and firewall posture without reducing the device to a misleading score.

The product is designed for a simple operating rule: inspect broadly, explain clearly, and change only a short list of explicitly authorized targets.

## What the current release delivers

- A full device scan with separate **Settings** and read-only **System Inventory** workspaces.
- Clear evidence states for configured, not configured, not observed, unsupported, access denied, and unknown results.
- Structured explanations covering mechanics, day-to-day impact, decision guidance, tradeoffs, and common misconceptions.
- Edition/build applicability for Windows 10 and Windows 11 rather than pretending every control exists everywhere.
- Curated, independently verified changes for registry policy, firewall profiles, selected diagnostic services and tasks, selected removable inbox apps, and selected optional Windows features.
- Permanent observation-only handling for BitLocker, User Account Control, and arbitrary firewall rules, with direct links to the appropriate Windows management surface.
- No application telemetry, cloud account, bulk hardening profile, generic command runner, or privacy score.

## Run a downloaded release

1. Install the [.NET 8 Desktop Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if it is not already present.
2. Download `WindowsPrivacyPlatform-win-x64.zip` from the repository's latest GitHub Release.
3. Extract the entire archive to a permanent folder. Do not run the executable from inside the zip.
4. Open `WindowsPrivacyPlatform.exe` and choose **Inspect** or **Modify**. Modify mode requests Windows elevation when a selected operation requires it.
5. Accept the one-time shortcut offer if you want Desktop and Start Menu entries that point to the extracted executable.

The archive includes `START_HERE.md` with the same deployment guidance. There is no installer and the release does not silently persist services, drivers, or background agents.

## Safety contract

Every writable object is deny-by-default. Discovery data never grants mutation rights. A change must have a complete typed target in a source-controlled allowlist and follows:

```text
pre-read → explicit confirmation → typed operation → independent read-back → local audit
```

Failure at any stage is reported as failure; a textual match with the wrong registry type is not accepted. Dynamic inventory is always read-only. See [Status/Safety_Model.md](Status/Safety_Model.md) for the full contract.

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
