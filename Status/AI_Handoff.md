# Windows Privacy Platform
## AI / Engineer Handoff Document

**Applies to:** **Version 1.2** (enterprise GUI refinement)  
**Last updated:** 2026-07-25  

**Read this file, then `Status/Current_Status.md`, `Status/Architecture.md`, `Status/Roadmap.md`, and `Status/History/v1.2.md` before changing code.**

---

## 1. Vision

Help humans **understand** Windows privacy and security configuration through transparent, trustworthy, **read-only** explanations and evidence.

**Understand first. Change later.**

Presentation standard: enterprise management console (Event Viewer / Device Manager / Services / XDR console family) — **property sheets, dense lists, progressive disclosure**.

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
10. Update Status after verification; History append-only.  
11. Do not reintroduce nested `PropertyGroup`/`Card` stacks around every paragraph. Prefer `PropertySheet` + plain section text + expanders for secondary detail.  
12. Detail pages must use progressive disclosure so they remain usable as the catalog grows.  

---

## 3. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

App is a WPF presentation host. `ScanService` composes the same pipeline as CLI. Backend is frozen.

---

## 4. Milestone pointer

| Version | Role |
|---------|------|
| v0.9.5 | Knowledge maturity |
| v1.0 | Desktop GUI + management-console direction — see `History/v1.0.md` |
| v1.1 | Kill stacked-card UI; MMC property sheets — see `History/v1.1.md` |
| **1.2** | Enterprise refinement: typography, progressive disclosure, scalable IA — see `History/v1.2.md` |
| Next (v1.5) | Themes, export/snapshots design, deeper domain knowledge |

---

## 5. Presentation notes (1.2)

- Classic **File / View / Tools / Help** menu bar.  
- Content host is **full width**.  
- Separators use dark rules (`BrushBorderStrong` / `#6D6D6D`).  
- Sidebar 240px; group labels use domain color identity; left accent selection.  
- Breadcrumbs: Home and domain segments navigate.  
- **Home:** Identity / Security / Scan as single `PropertySheet` tables.  
- **Detail:** Effective state first and prominent; short Summary always visible; Why/Impact, knowledge, layers, related behind expanders.  
- **Conflicts:** single-column rows only.  
- Domain entry tiles on Home: `DomainTile` (left accent, flat).  
- Typography: page titles ~20px, primary values ~15px, body ~13px.  
- Do not reintroduce card stacks or dump all knowledge on the detail page without disclosure.  

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
