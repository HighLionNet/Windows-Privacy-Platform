# Windows Privacy Platform
## AI / Engineer Handoff Document

**Applies to:** **Version 1.0** (final management-console presentation complete)  
**Last updated:** 2026-07-24  

**Read this file, then `Status/Current_Status.md`, `Status/Architecture.md`, `Status/Roadmap.md`, and `Status/History/v1.0.md` before changing code.**

---

## 1. Vision

Help humans **understand** Windows privacy and security configuration through transparent, trustworthy, **read-only** explanations and evidence.

**Understand first. Change later.**

Presentation standard: first-party Windows management console information architecture (Event Viewer / Device Manager / Services / GPO family).

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

---

## 3. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

App is a WPF presentation host. `ScanService` composes the same pipeline as CLI. Backend is frozen for the 1.0 line.

---

## 4. Milestone pointer

| Version | Role |
|---------|------|
| v0.9.5 | Knowledge maturity |
| **1.0** | Desktop GUI + final management-console presentation on frozen backend — see `History/v1.0.md` |
| Next (v1.5) | Themes, export/snapshots design, deeper domain knowledge, relationship visualization |

---

## 5. Presentation notes (1.0 final)

- Classic **File / View / Tools / Help** menu bar is part of the shell.  
- Content host is **full width** (no MaxWidth).  
- Separators use dark rules (`BrushBorderStrong` / `#6D6D6D`) — keep them visible.  
- Sidebar: left accent selection + group section rules.  
- Breadcrumbs: Home and domain segments must remain navigable.  
- Conflicts view is **single-column** (do not restore a cramped right-hand reason column).  
- Domain lists use proportional columns and subcategory group headers.  
- Detail page uses property-sheet label/value layout.  
- Do not reintroduce card stacks or constrained reading width without explicit product decision.  

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
