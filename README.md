# Windows Privacy Platform

**Current release:** **Version 1.0** (final)  
**Previous:** Prototype v0.9.5 → v0.9 → v0.8 → …

Local, **read-only** privacy and security **knowledge explorer** for Windows — professional desktop application with optional CLI/TUI.

> **Understand first. Change later.**

Windows Privacy Platform helps people see *what* is configured, *what the raw value means*, *which layer* appears to control it, *where the observation came from*, *why that layer wins*, and *how confident we are* — without modifying the system.

It is not a registry cleaner, optimizer, tweaker, score engine, compliance suite, or one-click hardener.

---

## Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF)
```

Presentation hosts (App, CLI, TUI) never contain Windows configuration logic. Backend architecture is frozen for the 1.0 line.

---

## Version 1.0

Desktop application designed as a Windows management console (Event Viewer / Device Manager / Services family):

- Classic **File / View / Tools / Help** menu bar  
- WPP brand tile, mode control (Inspect active / Modify disabled), search, Scan  
- Hierarchical sidebar with left-accent selection and visible group rules  
- **Full-width** content workspace (no artificial MaxWidth)  
- Clickable breadcrumbs (Home and domain segments navigate)  
- Machine Overview: operational sections (Attention · Identity · Security · Scan · evidence)  
- Domain lists: column headers, subcategory groups, proportional columns, status badges  
- Conflicts: single-column readable rows (title, path, effective value, reason)  
- Setting Detail: property-sheet label/value primary state; expanders for layers and long guidance  
- Knowledge Explorer and Search use the same dense list pattern  
- Window geometry and sidebar collapse remembered under LocalApplicationData  
- Keyboard: F5 / Ctrl+R scan, Ctrl+F search, Esc clears search  
- Async read-only scan; same knowledge engine as v0.9.5  
- CLI and TUI retained  

---

## Building

Requirements: .NET 8 SDK, Windows.

```powershell
cd Source
dotnet build -c Release
```

Expected: **0 Warning(s), 0 Error(s)**.

---

## Running

### Desktop (primary)

```powershell
cd Source\WindowsPrivacyPlatform.App
dotnet run -c Release
```

### CLI / TUI

```powershell
cd Source\WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
dotnet run -c Release -- --tui
```

---

## Safety

This product does **not** write to the registry, change services/tasks/packages/policies/firewall rules, request elevation, remediate, score, or send product telemetry.

Mode control exposes Inspect only. Modify remains a disabled future scaffold with an explanatory message.

---

## Documentation

- [`Status/Current_Status.md`](Status/Current_Status.md)  
- [`Status/Architecture.md`](Status/Architecture.md)  
- [`Status/AI_Handoff.md`](Status/AI_Handoff.md)  
- [`Status/History/v1.0.md`](Status/History/v1.0.md)  

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
