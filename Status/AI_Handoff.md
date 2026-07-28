# Windows Privacy Platform
## AI / Engineer Handoff Document

**Applies to:** **Version 1.3** (GUI navigation clarity + mid-cyber presentation)  
**Last updated:** 2026-07-28  

**Read this file, then `Status/Current_Status.md`, `Status/Architecture.md`, `Status/Roadmap.md`, and prior `Status/History/v1.2.md` before changing code.**

---

## 1. Vision

Help humans **understand** Windows privacy and security configuration through transparent, trustworthy, **read-only** explanations and evidence.

**Understand first. Change later.**

Presentation standard: enterprise management console with mid-cyber density — **property sheets, setting cards, progressive disclosure**. Navigation is page-based (no expanders for primary discovery).

---

## 2. Mandatory rules

1. Solution must compile (**0 errors, 0 warnings** preferred).  
2. Never redesign the project layout without explicit approval.  
3. **No write paths, no elevation, no remediation.**  
4. Models free of OS I/O.  
5. PolicyPrecedenceResolver is the only place for layer precedence.  
6. ValueSemantics live in catalog.  
7. CLI / TUI / **App** are presentation-only.  
8. Never invent Enabled/disabled from Unknown.  
9. Never add a privacy/security score.  
10. Update Status after verification; History append-only when a new History file is authorized.  
11. Do not reintroduce nested `PropertyGroup`/`Card` stacks around every paragraph. Prefer setting cards + PropertySheet + expanders for secondary detail.  
12. Detail pages must use progressive disclosure so they remain usable as the catalog grows.  
13. Prefer page navigation over expanders for primary discovery (v1.3).  

---

## 3. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

App is a WPF presentation host. `ScanService` composes the same pipeline as CLI. Backend is frozen. v1.3 added `OptionDisplay` on `SettingDetailView` (presentation data only).

---

## 4. Milestone pointer

| Version | Role |
|---------|------|
| v0.9.5 | Knowledge maturity |
| v1.0 | Desktop GUI + management-console direction — see `History/v1.0.md` |
| v1.1 | Kill stacked-card UI; MMC property sheets — see `History/v1.1.md` |
| v1.2 | Enterprise refinement: typography, progressive disclosure — see `History/v1.2.md` |
| **1.3** | Domain setting cards + sketch-aligned detail + Home declutter + mid-cyber styling |
| Next (v1.5) | Themes, export/snapshots design, deeper domain knowledge |

---

## 5. Presentation notes (1.3)

- Classic **File / View / Tools / Help** menu bar.  
- Content host is **full width**.  
- Separators use dark rules (`BrushBorderStrong`).  
- Sidebar 240px; group labels use domain color identity; left accent selection.  
- Breadcrumbs: Home and domain segments navigate.  
- **Home:** Identity / Security / Scan property sheets only; conflict attention when present. No domain tiles, no expanders.  
- **Domain:** stacked setting cards (current, effective, options from ValueSemantics, left accent by conflict/unknown). Subcategory headers when present. Click opens detail.  
- **Detail:** left = current + effective + source/confidence/reason; right = options table; Summary visible; secondary knowledge in expanders.  
- Cascadia Code / Consolas for raw values.  
- Mid-cyber palette: stronger borders/headers, higher contrast, still enterprise.  

---

## 6. Build

```
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.App
dotnet run -c Release
```

Expected: 0 Warning(s), 0 Error(s).

---

## END OF HANDOFF
