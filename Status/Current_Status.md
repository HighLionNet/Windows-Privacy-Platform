# Windows Privacy Platform
## Current Status — Version 1.5 (in progress)

**Document role:** Authoritative live snapshot.

**Last updated:** 2026-08-09  
**Current development version:** **1.5** (catalog expansion + Modify elevation scaffold + detail cleanup)  
**Previous archived milestone:** Version 1.4  
**Runtime target:** Windows 11; Scanner / CLI / App target `net8.0-windows`  
**Safety posture:** Read-only by default. Modify mode is an elevation scaffold only — **no writes** are performed. Auth and change events log to dedicated files.

---

## 1. Product identity

Windows Privacy Platform is a **local Windows privacy and security knowledge explorer** with a professional desktop application and optional CLI/TUI.

Philosophy: **Understand first. Change later.**

Presentation standard: enterprise management console (Event Viewer / Device Manager / Services / XDR console family) — property sheets, dense list indexes, page hierarchy.

---

## 2. Architecture

Eight projects, one-way dependencies:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF presentation)
```

---

## 3. Version 1.5 changes (so far)

### Elevation / Modify mode scaffold
- `ElevationService` uses `WindowsIdentity` / `WindowsPrincipal` (no custom password store).
- Modify ComboBox is enabled. Entering Modify requires elevated process + explicit user confirmation.
- No registry or system writes are performed.
- Auth decisions logged to `%LocalAppData%\WindowsPrivacyPlatform\Logs\auth.log`.
- Change-scaffold events logged to `changes.log`.

### Logging
- `AuditEventType.Auth` and `AuditEventType.Change` added.
- `AuditLogger` writes dedicated auth.log and changes.log under LocalApplicationData.

### Presentation
- Setting detail: expanders removed. Why / Impact / Misconception / Layers / Related are always visible, minimal blocks.
- Layout hierarchy unchanged: Home → Domain → Category → Setting detail.
- Version string v1.5.

### Catalog / collectors
- Expansion of Windows Update, Defender, SmartScreen, and related high-value GPOs is the primary remaining work for this milestone (target ~30–50 additional curated entries with matching probes and ValueSemantics).

---

## 4. Safety

- Default mode remains Inspect / read-only.
- Modify mode is a session authorization gate only; write paths are not implemented.
- No remediation, no scores, no product telemetry.

---

## 5. Build and run

```powershell
cd Source
dotnet build -c Release

cd WindowsPrivacyPlatform.App
dotnet run -c Release
```
