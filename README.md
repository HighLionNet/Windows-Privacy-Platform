# Windows Privacy Platform

**Version 2.1**

Evidence-driven Windows privacy and configuration inspection and management.

> **Understand first. Change deliberately. Never invent certainty.**

---

## What it is

A local WPF tool for inspecting and carefully changing **native Windows** privacy and security configuration.

It is **not**:

- an optimizer or “debloater”
- a privacy score product
- a generic registry / service / task / firewall editor
- a one-click hardening suite

## What it does

- **Inspect (default):** ConsentStore app permissions, Group Policy / registry-backed policies, Defender, SmartScreen, Windows Update policies, AppPrivacy, UAC and BitLocker **policy** observation, services, scheduled tasks, capabilities, identity, firewall **profile** state.
- **Modify (explicit):** After UAC elevation and session authorize, only catalog settings with an explicit `WritableTarget` can be changed — **one setting at a time**. Every change is pre-read, confirmed, written with the catalog type, and verified by independent read-back.

### Evidence states

| State | Meaning |
|-------|---------|
| **Unknown** | Insufficient evidence. Never treated as configured or absent. |
| **Not configured** | Location was checked; value is absent. |
| **Not observed** | This scan did not collect this setting. |
| **Error / Access denied** | Collection failed. Not the same as absent. |

### Hard rules

- No privacy “score”. No optimizer. No bulk apply. No rollback system. No profiles.
- No application telemetry. No cloud backend.
- Firewall **rules** are observation-only.
- Services and scheduled tasks are observation-focused (no generic kill/edit UI).
- BitLocker low-level operations are not exposed as a generic editor; policy observation is available.
- Modification is **deny-by-default**: only explicit catalog `WritableTarget` entries are writable. `DiscoveryMethod` never grants write permission.

Primary target: **Windows 11**. Windows 10 is supported where the same implementation is naturally correct.

---

## Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → App (WPF)
```

CLI is not part of the product. The WPF GUI is the product.

Safety details: `Status/Safety_Model.md`

---

## Building

```powershell
cd Source
dotnet restore WindowsPrivacyPlatform.sln
dotnet build WindowsPrivacyPlatform.sln -c Release
dotnet test WindowsPrivacyPlatform.sln -c Release
```

### Publish (win-x64)

```powershell
dotnet publish ".\Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj" -c Release -r win-x64 --self-contained false -o ".\publish"
```

For accurate HKLM reads and Modify mode, run elevated or use the in-app Modify elevation path.

---

## Local data

- Window prefs: `%LocalAppData%\WindowsPrivacyPlatform\window.prefs`
- Audit logs: under the same app data folder (`changes.log`, `auth.log`)

---

## Contributing / security

See `CONTRIBUTING.md` and `SECURITY.md`.

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
