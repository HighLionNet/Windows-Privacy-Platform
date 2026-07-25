# Windows Privacy Platform
## Current Status — Version 1.2

**Document role:** Authoritative live snapshot.

**Last updated:** 2026-07-25  
**Current development version:** **1.2** (enterprise GUI refinement)  
**Previous archived milestone:** Version 1.1  
**Runtime target:** Windows 11; Scanner / CLI / App target `net8.0-windows`  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation. No scores.  

---

## 1. Product identity

Windows Privacy Platform is a **local, read-only Windows privacy and security knowledge explorer** with a professional desktop application and optional CLI/TUI.

Philosophy: **Understand first. Change later.**

Presentation standard: enterprise management console (Event Viewer / Device Manager / Services / XDR console family) — property sheets, dense lists, progressive disclosure.

---

## 2. Architecture

Eight projects, one-way dependencies:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF presentation)
```

Backend architecture remains frozen. v1.2 is presentation-only.

---

## 3. Version 1.2 presentation

### Shell

- Classic File / View / Tools / Help menu bar  
- Toolbar: WPP tile, title, scan time, Mode, search, Scan  
- Sidebar 240px; group labels with domain color identity  
- Left-accent selection; dark section rules  
- Clickable breadcrumbs; full-width workspace  
- Status bar: Objects, Conflicts, Validation, Inspect · Read-only  
- Version **v1.2**  

### Views

- **Machine Overview** — attention (conflicts only), Identity + Security + Scan as **property sheets**, domain tiles  
- **Domain** — column list (Setting / Observed→effective / Status), subcategory headers  
- **Conflicts** — single-column rows (title, path, Effective [source], reason)  
- **Setting Detail** — Effective state first (large); short Summary always visible; Why/Impact, Knowledge, Layers, Related behind expanders  
- **Knowledge / Search** — dense list pattern  
- **About** — property-sheet product facts  

### Typography & density

- Page titles 20px; primary values 15px; body 13px with line-height  
- Comfortable row and sheet padding for long sessions  
- Progressive disclosure so detail pages stay short as catalog grows  

---

## 4. Safety

Unchanged: no writes; no elevation; no remediation; no scores; no product telemetry; Unknown first-class.

---

## 5. Limitations

1. Light theme only (dark / high-contrast → v1.5)  
2. Secure Boot / TPM / BitLocker often Unknown without elevation  
3. Firewall profile-level  
4. No scan history / comparison / export  
5. MDM/baseline incomplete  

---

## 6. Build and run

```powershell
cd Source
dotnet build -c Release

cd WindowsPrivacyPlatform.App
dotnet run -c Release

cd ..\WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --tui
```
