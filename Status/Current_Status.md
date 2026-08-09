# Windows Privacy Platform
## Current Status — Version 1.5

**Document role:** Authoritative live snapshot.

**Last updated:** 2026-08-09  
**Current development version:** **1.5**  
**Previous:** Version 1.4  
**Runtime target:** Windows 11; net8.0-windows  
**Safety posture:** Read-only by default. Modify mode is elevation scaffold only — **no writes**. Auth/change events log to dedicated files.

---

## 1. Product identity

Local Windows privacy and security knowledge explorer. Philosophy: **Understand first. Change later.**

---

## 2. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

---

## 3. Version 1.5 complete

### Elevation / Modify mode scaffold
- `ElevationService` (WindowsIdentity / WindowsPrincipal).
- Requires elevated process + explicit confirmation.
- No registry or system writes.
- Logs: `%LocalAppData%\WindowsPrivacyPlatform\Logs\auth.log` and `changes.log`.

### Logging
- `AuditEventType.Auth` / `Change`.
- File sinks for auth and change events.

### Presentation
- Setting detail: expanders removed; Why/Impact/Misconception/Layers/Related always visible.
- Hierarchy unchanged.
- Version string v1.5; App Version 1.5.0.

### Catalog / collectors
- PolicyCollector expanded with deep Windows Update (deferrals, WUServer, TargetRelease, ManagePreviewBuilds, ElevateNonAdmins, DualScan, etc.), Defender (Network Protection, Controlled Folder Access, CloudBlockLevel, DisableBlockAtFirstSeen, ScriptScanning, Catchup scans), SmartScreen, Clipboard history/cross-device.
- Matching catalog entries and ValueSemantics are aligned for binding.
- Relationships extended for new conflict/override pairs where applicable.

---

## 4. Safety

Inspect default. Modify is session authorization gate only. No write paths implemented.

---

## 5. Build

```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.App
dotnet run -c Release
```
