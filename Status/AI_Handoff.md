# Windows Privacy Platform
## AI / Engineer Handoff Document

**Applies to:** **Version 1.6.2**  
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
9. DiscoveryMethod for any writable setting must be a concrete hive + subkey + value name (no `...`, wildcards, or ServiceController).

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

## v1.6.2 notes

- Catalog: all ConsentStore DiscoveryMethod paths are full concrete registry locations.
- ContentDelivery path concrete.
- Firewall service / logging remain non-writable observation surfaces.
- CategoryView: tall cards, short blurb, value buttons; full rescan after verified change.
- SettingDetailPage: status + single explanation paragraph.
- Modify writes DWORD/string/clear for resolved DiscoveryMethod paths only.

---

## Remaining work (ordered)

1. Elevation-by-default option for accurate system-wide reads (Inspect still never writes).
2. Full firewall rule inventory (read-only; no mutation).
3. Evidence-pack export (JSON).
4. OSS surface: LICENSE, SECURITY.md, CONTRIBUTING.md, accurate README.
5. Validator / relationship edge integrity pass.

---

## Build

```
cd Source
dotnet build -c Release
```
