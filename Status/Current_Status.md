# Windows Privacy Platform
## Current Status — Version 1.3

**Document role:** Authoritative live snapshot.

**Last updated:** 2026-07-28  
**Current development version:** **1.3** (GUI navigation clarity + mid-cyber presentation)  
**Previous archived milestone:** Version 1.2  
**Runtime target:** Windows 11; Scanner / CLI / App target `net8.0-windows`  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation. No scores.  

---

## 1. Product identity

Windows Privacy Platform is a **local, read-only Windows privacy and security knowledge explorer** with a professional desktop application and optional CLI/TUI.

Philosophy: **Understand first. Change later.**

Presentation standard: enterprise management console (Event Viewer / Device Manager / Services / XDR console family) with mid-cyber density for legibility — property sheets, setting cards, progressive disclosure.

---

## 2. Architecture

Eight projects, one-way dependencies:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF presentation)
```

Backend architecture remains frozen. v1.3 is presentation-only (plus a small presentation-data addition on SettingDetailView for options).

---

## 3. Version 1.3 presentation

### Shell

- Classic File / View / Tools / Help menu bar  
- Toolbar: WPP tile, title, scan time, Mode, search, Scan  
- Sidebar 240px; group labels with domain color identity  
- Left-accent selection; darker section rules  
- Clickable breadcrumbs; full-width workspace  
- Status bar: Objects, Conflicts, Validation, Inspect · Read-only  
- Version **v1.3**  

### Views

- **Machine Overview** — Identity + Security + Scan property sheets only; conflict attention when present; no domain tiles, no expanders  
- **Domain** — stacked setting cards: name, current setting, effective state, options table (ValueSemantics), left accent by conflict/unknown/normal; subcategory headers when present  
- **Setting Detail** — sketch layout: current + effective (left) + options table (right); Summary always visible; secondary knowledge in expanders  
- **Conflicts / Knowledge / About** — unchanged pattern  

### Typography & density

- Page titles 20px; primary values 15px; body 13px  
- Cascadia Code / Consolas for raw values and options  
- Stronger borders and header contrast (mid-cyber, not game)  

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
