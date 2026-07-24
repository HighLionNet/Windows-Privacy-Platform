# Windows Privacy Platform
## Current Status — Version 1.0 (final)

**Document role:** Authoritative live snapshot.

**Last updated:** 2026-07-24  
**Current development version:** **1.0** (final management-console presentation pass complete)  
**Previous archived milestone:** Prototype v0.9.5  
**Runtime target:** Windows 11; Scanner / CLI / App target `net8.0-windows`  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation. No scores.

---

## 1. Product identity

Windows Privacy Platform is a **local, read-only Windows privacy and security knowledge explorer** with a professional desktop application and optional CLI/TUI.

Philosophy: **Understand first. Change later.**

Presentation standard for 1.0 final: first-party Windows management console (Event Viewer / Device Manager / GPO / Services family) — not a card dashboard or Fluent showcase.

---

## 2. Architecture

Eight projects, one-way dependencies:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF presentation)
```

| Project | Role |
|---------|------|
| **Models** | Catalog, ValueSemantics, MachineOverview, resolution, explanations, SettingsQuery, NavigationBuilder |
| **Core** | Primitives |
| **Logging** | Audit |
| **KnowledgeBase** | In-memory store |
| **Validator** | Structural + unique ObjectId |
| **Scanner** | Collectors, binders, PolicyPrecedenceResolver |
| **CLI** | Console / TUI host |
| **App** | WPF desktop host (presentation only) |

Backend architecture remains frozen.

---

## 3. Version 1.0 capabilities (final presentation)

- Professional WPF shell with hierarchical navigation, group separators, left-accent selection  
- **Full-width content workspace** (no artificial MaxWidth constraint)  
- Section rules, header rules, bordered list containers, property groups throughout  
- Application mode: Inspect (active) / Modify (disabled scaffold + explanation)  
- Machine Overview: operational sections (Attention · Identity · Security · Scan · evidence expanders)  
- Domain / Conflicts / Search / Knowledge: dense bottom-border list rows with column headers and status badges  
- Setting Detail: property-sheet label/value grid for primary state; sections with rules; related as list rows  
- Window size / position / sidebar collapse remembered under LocalApplicationData  
- Keyboard: F5 / Ctrl+R scan, Ctrl+F search, Esc clears search  
- Status bar uses operational language (Objects, Conflicts, Validation, Inspect · Read-only)  
- Async scan via ScanService  
- Full v0.9.5 knowledge engine underneath  
- CLI and TUI retained  

---

## 4. Safety

Unchanged: no registry/service/task/policy/firewall writes; no elevation; no remediation; no scores; no product telemetry; Unknown first-class. Mode control does not enable writes.

---

## 5. Limitations

1. Light theme only (dark / high-contrast deferred to v1.5)  
2. Secure Boot / TPM / BitLocker often Unknown without elevation  
3. Firewall profile-level  
4. No scan history / comparison / export  
5. MDM/baseline incomplete  
6. Root KnowledgeBase folder is non-build duplicate (Source is authoritative)  

---

## 6. Build and run

```powershell
cd Source
dotnet build -c Release

# Desktop (primary)
cd WindowsPrivacyPlatform.App
dotnet run -c Release

# CLI / TUI
cd ..\WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --tui
```
