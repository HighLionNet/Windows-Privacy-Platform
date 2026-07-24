# Windows Privacy Platform
## Current Status — Version 1.0 (final)

**Document role:** Authoritative live snapshot.

**Last updated:** 2026-07-24  
**Current development version:** **1.0** (final management-console presentation complete)  
**Previous archived milestone:** Prototype v0.9.5  
**Runtime target:** Windows 11; Scanner / CLI / App target `net8.0-windows`  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation. No scores.  
**Build:** `dotnet build -c Release` — **0 Warning(s), 0 Error(s)**

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

### Shell

- Classic **File / View / Tools / Help** menu bar (Scan, Exit, Overview, Conflicts, Knowledge, Toggle navigation, Search, About)  
- Toolbar: WPP brand tile, title, scan timestamp, Mode (Inspect / Modify disabled), search box, Scan button  
- Hierarchical sidebar: Machine · Privacy · Security · Windows · Applications · Knowledge · About  
- Left-accent selection; dark group section rules (`#6D6D6D`)  
- Collapsible navigation pane (View menu); state remembered  
- Clickable breadcrumbs: Home and domain segments navigate  
- **Full-width** content workspace (no MaxWidth)  
- Status bar: Objects, Conflicts, Validation, Inspect · Read-only  
- Window size / position remembered under `%LocalAppData%\WindowsPrivacyPlatform`  

### Views

- **Machine Overview** — Attention (when conflicts exist), Identity (3-column), Security posture, Scan meta, evidence expanders, domain shortcuts  
- **Domain** — bordered list, column header (Setting / Observed-effective / Status), subcategory group headers, proportional columns  
- **Conflicts** — single-column readable rows (title, path, Effective, full reason below)  
- **Setting Detail** — property-sheet label/value grid; section rules; layers and long guidance behind expanders; related as list rows  
- **Knowledge Explorer / Search** — same dense list pattern  
- **About** — mode, safety, keyboard, project metadata  

### Engine and hosts

- Async scan via `ScanService` (same pipeline as CLI)  
- Full v0.9.5 knowledge engine  
- Keyboard: F5 / Ctrl+R scan, Ctrl+F search, Esc clears search  
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
