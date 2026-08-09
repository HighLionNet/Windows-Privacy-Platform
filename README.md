# Windows Privacy Platform

**Version 2.0**

Evidence-driven Windows privacy and configuration inspection and management.

> **Understand first. Change deliberately. Never invent certainty.**

---

## What it does

- **Inspect (default):** Read-only inventory of ConsentStore permissions, Group Policy / registry policies, identity, services, packages, scheduled tasks, and firewall profile state.
- **Modify (explicit):** After UAC elevation and session authorize, catalog settings with an explicit `WritableTarget` can be changed. Every change is pre-read, confirmed, applied, and **only reported successful after independent read-back of the exact target**. Audited locally.

### Hard rules

| State | Meaning |
|-------|---------|
| **Unknown** | Insufficient evidence. Never treated as configured or absent. |
| **Not configured** | Location was checked; value is absent. |
| **Not observed** | This scan did not collect this setting. |
| **Error** | Collection failed. |

- No privacy “score”. No optimizer. No one-click tweaker.
- No telemetry from this application.
- Firewall **rules** are observation-only. Use Windows Firewall (`wf.msc`) for rule management.
- Modification is **deny-by-default**: only settings with an explicit catalog `WritableTarget` are writable.

---

## Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → App (WPF)
```

- **Models** — pure data (catalog, observations, ValueSemantics, WritableTarget). No OS I/O.
- **Scanner** — collectors are the only inventory read layer.
- **PolicyChangeService** — sole write path; requires elevation + explicit WritableTarget.
- **PolicyPrecedenceResolver** — sole precedence authority; Unknown never wins.

CLI was removed in v2.0. The WPF GUI is the product.

---

## Building

```powershell
cd Source
dotnet build WindowsPrivacyPlatform.sln -c Release
```

## Publishing (win-x64)

```powershell
dotnet publish ".\Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj" -c Release -r win-x64 --self-contained false -o ".\publish"
```

## Running

```powershell
cd Source\WindowsPrivacyPlatform.App
dotnet run -c Release
```

For accurate HKLM / service reads and Modify mode, run elevated or use the in-app Modify elevation path.

---

## Modify contract

1. Resolve explicit `WritableTarget` from catalog (not from Observation).
2. Read exact target.
3. User confirms.
4. Write exact target with catalog-defined RegistryValueKind.
5. Read exact target again.
6. Report success only if verified.
7. Refresh scan.

Unsupported types and settings without a WritableTarget are refused cleanly.

---

## Local data

- Window prefs: `%LocalAppData%\WindowsPrivacyPlatform\window.prefs`
- Audit logs: under the same app data folder (`changes.log`, `auth.log`)

No network calls. No cloud backend.

---

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
