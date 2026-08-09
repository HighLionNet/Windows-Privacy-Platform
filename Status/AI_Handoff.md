# Windows Privacy Platform
## AI / Engineer Handoff Document

**Applies to:** **Version 1.6**  
**Last updated:** 2026-08-09  

**Read this file, then `Status/Current_Status.md` before changing code.**

---

## Vision

Understand first. Change deliberately — confirmed, elevated, audited.

---

## Rules

1. Solution must compile.  
2. Do not redesign project layout without approval.  
3. **Writes only through PolicyChangeService** when ElevationService.IsModifyAuthorized.  
4. Models free of OS I/O.  
5. ValueSemantics live in catalog.  
6. Category list owns change buttons; detail page is explanation-only.  
7. Never invent Enabled/disabled from Unknown.  
8. Update Status after verification.  

---

## Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

App services:
- `ElevationService` — UAC relaunch + session authorize (auth.log)
- `PolicyChangeService` — confirmed registry set/delete (changes.log)

---

## v1.6 notes

- CategoryView: tall cards, short blurb, value buttons
- SettingDetailPage: status + single explanation paragraph
- Modify writes DWORD/string/clear for resolved DiscoveryMethod paths
- Firewall/service paths refused

---

## Build

```
cd Source
dotnet build -c Release
```
