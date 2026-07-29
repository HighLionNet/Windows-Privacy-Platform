# Windows Privacy Platform
## Current Status — Version 1.4

**Document role:** Authoritative live snapshot.

**Last updated:** 2026-07-29  
**Current development version:** **1.4** (GUI information architecture + list-oriented indexes)  
**Previous archived milestone:** Version 1.3  
**Runtime target:** Windows 11; Scanner / CLI / App target `net8.0-windows`  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation. No scores.  

---

## 1. Product identity

Windows Privacy Platform is a **local, read-only Windows privacy and security knowledge explorer** with a professional desktop application and optional CLI/TUI.

Philosophy: **Understand first. Change later.**

Presentation standard: enterprise management console (Event Viewer / Device Manager / Services / XDR console family) — property sheets, dense list indexes, page hierarchy. Not a card dashboard.

---

## 2. Architecture

Eight projects, one-way dependencies:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF presentation)
```

Backend architecture remains frozen. v1.4 is presentation-only.

---

## 3. Version 1.4 presentation

### Information hierarchy

```
Home
 └── Domain          (category index)
      └── Category   (setting list)
           └── Setting detail
```

### Shell

- Classic File / View / Tools / Help menu bar  
- Toolbar: WPP tile, title, scan time, Mode, search, Scan  
- Sidebar 240px; group labels with domain color identity  
- Left-accent selection; darker section rules  
- Clickable breadcrumbs including Domain and Category  
- Status bar: Objects, Conflicts, Validation, Inspect · Read-only  
- Version **v1.4**  

### Views

- **Machine Overview** — Identity + Security + Scan property sheets; conflict attention when present  
- **Domain** — category index (Category / Settings / Attention). No setting cards.  
- **Category** — setting list (Setting / Current / Effective / Status). No options, no repeated labels.  
- **Setting Detail** — State property sheet; Available values when present; Summary; secondary knowledge in expanders  
- **Conflicts / Knowledge / About** — list / property-sheet pattern  

### Typography & density

- Page titles 20px; primary values 15px; body 13px  
- Cascadia Code / Consolas for raw values  
- List rows with column headers; alternating row background on category lists  
- Borders for grouping and selection only — not around every value  

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
6. Visual verification of WPF requires a Windows host (this agent environment is Linux)  

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
