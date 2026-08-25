# Windows Privacy Platform

**Version 2.1.0**

Evidence-driven Windows privacy and security inspection with deliberately constrained, one-setting-at-a-time modification.

> **Understand first. Change deliberately. Never invent certainty.**

## What it is

Windows Privacy Platform is a local .NET 8/WPF application for understanding native Windows configuration. It starts with a clear choice between **Inspect** (recommended, read-only, no administrator rights) and **Modify** (administrator elevation plus an explicit session authorization).

It is not an optimizer, debloater, privacy score, generic registry editor, bulk hardening tool, or cloud service.

## v2.1 coverage

The catalog contains **257 setting-specific entries** across Windows privacy, security, servicing, applications, and system inventory. Important surfaces include:

- ConsentStore and AppPrivacy permissions, diagnostic data, activity, location, speech, advertising, clipboard, and cloud content
- Windows Recall, Windows Copilot, Edge, Widgets, Search, Family Safety, and accessibility settings synchronization
- Microsoft Defender, Attack Surface Reduction rules, SmartScreen, Windows Hello, UAC, BitLocker policy, local security policy, and firewall profiles
- Windows Update and WSUS, Storage Sense, encrypted DNS, network isolation, and Wi-Fi random-address state
- curated read-only anchors for relevant services, scheduled tasks, AppX packages, and Windows capabilities

Every catalog item carries complete decision-support text, value/source evidence, and an explicit distinction between configured, absent, unobserved, and failed collection states.

## Safety contract

- Inspect mode is always read-only and never elevates automatically.
- Modify mode requires UAC elevation and one explicit authorization for the session.
- A setting is writable only when its catalog entry has a complete, explicitly whitelisted `WritableTarget`; discovery metadata never grants write access.
- Every write performs pre-read → user confirmation → typed write → independent value-and-kind read-back verification → local audit logging.
- BitLocker and UAC master switches, firewall, services, scheduled tasks, packages, capabilities, ASR rules, and local security policy remain observation-only.
- External tools run only through timeout-bound `SafeProcessRunner`; there is no arbitrary command surface.

Live BitLocker protection status is queried only in an elevated process. A normal Inspect session reports **Requires Modify mode to observe** rather than inventing a state.

Full details: [Status/Safety_Model.md](Status/Safety_Model.md).

## Evidence states

| State | Meaning |
|---|---|
| **Unknown** | Evidence is insufficient; the app does not infer a state. |
| **Not configured** | The exact location was checked and the value was absent. |
| **Not observed** | This scan did not collect the setting. |
| **Error / Access denied** | Collection failed; this is never treated as absence. |

## Architecture

```text
Models → Core → Logging → KnowledgeBase → Validator → Scanner → App (WPF)
```

The WPF application is the product; there is no supported CLI surface. See [Status/Architecture.md](Status/Architecture.md) for the collector, binder, narrative, and write-boundary design.

## Build and test

```powershell
cd Source
dotnet restore WindowsPrivacyPlatform.sln
dotnet build WindowsPrivacyPlatform.sln -c Release
dotnet test WindowsPrivacyPlatform.sln -c Release
```

Publish a framework-dependent Windows x64 build from the repository root:

```powershell
dotnet publish ".\Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj" `
  -c Release -r win-x64 --self-contained false -o ".\publish"
```

## Local data

- Window preferences: `%LocalAppData%\WindowsPrivacyPlatform\window.prefs`
- Audit logs: the same local application-data directory (`changes.log`, `auth.log`)

The app has no telemetry, cloud backend, or network phone-home.

## Project information

See [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md), and the [v2.1 status](Status/Current_Status.md).

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
