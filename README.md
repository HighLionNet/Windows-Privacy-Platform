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

- Professional **WPF** desktop application designed as a Windows management console (Event Viewer / Device Manager family)  
- Hierarchical navigation with group separators and left-accent selection  
- **Full-width** content workspace  
- Section / header rules and bordered list containers for visual structure  
- Machine Overview as operational sections (identity · security · scan · evidence)  
- Domain / Conflicts / Search / Knowledge pages use denser list rows with column headers  
- Setting Detail as property-sheet surface (label/value primary state; layers and long guidance behind expanders)  
- Application mode control: **Inspect** (active) / **Modify** (scaffold, disabled, future)  
- Collapsible sidebar, remembered window size/position and sidebar state  
- Keyboard: F5 / Ctrl+R scan, Ctrl+F search, Esc clears search  
- Async read-only scan  
- Same knowledge engine as v0.9.5 underneath  
- CLI and TUI retained  

---

## Building

Requirements: .NET 8 SDK, Windows.

```powershell
cd Source
dotnet build -c Release
```

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
