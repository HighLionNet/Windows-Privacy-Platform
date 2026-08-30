# Windows Privacy Platform

<img src="Source/WindowsPrivacyPlatform.App/Assets/WindowsPrivacyPlatform.png" alt="Windows Privacy Platform shield mark" width="128" height="128">

Windows Privacy Platform is a local privacy, security, and network policy hub for Windows 10 and Windows 11. It shows the settings that actually control app access, diagnostics, Defender, Firewall, DNS, remote access, and Microsoft apps — without becoming a generic registry editor, debloater, or antivirus.

The operating rule is simple: show only curated, editable policy in Settings; observe the rest of the operating system in a separate, read-only Troubleshoot/Explore surface; deny every write that is not explicitly authorized.

Current product line: **2.6.1**. Source of truth is [`main`](https://github.com/HighLionNet/Windows-Privacy-Platform). Frozen snapshots live on `vX.Y.Z` branches.

## What 2.6.1 delivers

- Four exclusive sections: **Privacy**, **Security**, **Network**, and **Troubleshoot/Explore**.
- Persistent Hub destinations: Dashboard, Conflicts, Knowledge Explorer, App Settings, and About.
- Category-first navigation. Search and Dashboard findings land on a settings list; detail opens only when requested.
- Distinct evidence states: configured, not configured, not observed, unsupported, access denied, unknown, and error. Unknown is never treated as safe. There is no synthetic score.
- Curated registry-policy changes with one confirmation, independent value-and-kind read-back, and a local audit.
- Twelve bounded firewall-profile controls (enabled / inbound / outbound / notifications × domain / private / public). Individual firewall rules stay inventory.
- Fail-soft observation of Windows Security Center antivirus / EDR products. WPP does not fight, disable, or replace them.
- No bulk hardening profile, generic command runner, telemetry, cloud account, background agent, or silent persistence.

## Run a downloaded release

1. Install the [.NET 8 Desktop Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) if it is not already present.
2. Download `WindowsPrivacyPlatform-win-x64.zip` from the repository's latest GitHub Release for **2.6.1** (do not use 2.3.x archives).
3. Extract the entire archive to a permanent folder. Do not run the executable from inside the zip.
4. Open `WindowsPrivacyPlatform.exe` and choose **View-only** or **Administrator**. Administrator mode requests Windows elevation for machine policy.
5. Accept the one-time shortcut offer if you want Desktop and Start Menu entries that point to the extracted executable.

The archive includes `START_HERE.md` with the same deployment guidance. There is no installer. Removing the extracted folder uninstalls the app.

## Safety contract

Every writable object is deny-by-default. Discovery data never grants mutation rights. A change must have a complete typed target in a source-controlled allowlist and follows:

```text
allowlist compare → pre-read → explicit confirmation → typed operation → independent read-back of value and kind → local audit
```

Failure at any stage is reported as failure. A textual match with the wrong registry type is not accepted. System Explorer, live inventory, and arbitrary firewall rules cannot acquire a write target. See [Status/Safety_Model.md](Status/Safety_Model.md).

## Build from source

Requirements: Windows, PowerShell, and the .NET 8 SDK.

```powershell
dotnet restore .\Source\WindowsPrivacyPlatform.sln
dotnet build .\Source\WindowsPrivacyPlatform.sln -c Release --no-restore
dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release --no-build
.\scripts\build-release.ps1
.\scripts\verify-release.ps1 -ArchivePath .\.artifacts\release\WindowsPrivacyPlatform-win-x64.zip
```

The release archive is written beneath `.artifacts\release`. `sync-run.ps1` is a fast-forward-only convenience workflow; it refuses to discard a dirty checkout.

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

See [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md), and [Status/Architecture.md](Status/Architecture.md).

Maintained by HighLionNet. [Project repository](https://github.com/HighLionNet/Windows-Privacy-Platform).
