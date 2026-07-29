# Windows Privacy Platform
## AI / Engineer Handoff Document

**Applies to:** **Version 1.4** (GUI information architecture + list-oriented indexes)  
**Last updated:** 2026-07-29  

**Read this file, then `Status/Current_Status.md`, `Status/Architecture.md`, `Status/Roadmap.md`, and prior `Status/History/v1.3` notes / `v1.2.md` before changing code.**

---

## 1. Vision

Help humans **understand** Windows privacy and security configuration through transparent, trustworthy, **read-only** explanations and evidence.

**Understand first. Change later.**

Presentation standard: enterprise management console — **property sheets, list indexes, page hierarchy**. Navigation is page-based. Do **not** reintroduce stacked setting cards on domain or category pages.

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
11. **Do not reintroduce stacked setting cards** on Domain or Category pages. Indexes are list rows; detail is property-sheet.  
12. Detail pages use progressive disclosure for secondary knowledge only.  
13. Preferred hierarchy: **Home → Domain → Category → Setting detail**.  
14. Column headers carry meaning on list pages — do not repeat "Current setting:" / "Effective state:" labels inside every row.  

---

## 3. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

App is a WPF presentation host. `ScanService` composes the same pipeline as CLI. Backend is frozen.

v1.4 presentation-only: DomainView (category index), CategoryView (setting list), refined SettingDetailPage, breadcrumb category segment.

---

## 4. Milestone pointer

| Version | Role |
|---------|------|
| v0.9.5 | Knowledge maturity |
| v1.0 | Desktop GUI + management-console direction — see `History/v1.0.md` |
| v1.1 | Kill stacked-card UI; MMC property sheets — see `History/v1.1.md` |
| v1.2 | Enterprise refinement: typography, progressive disclosure — see `History/v1.2.md` |
| v1.3 | Domain setting cards + sketch-aligned detail + Home declutter + mid-cyber styling |
| **1.4** | Information architecture: Domain→Category→Setting; list indexes; remove setting cards from indexes |
| Next (v1.5) | Themes, export/snapshots design, deeper domain knowledge |

---

## 5. Presentation notes (1.4)

- Classic **File / View / Tools / Help** menu bar.  
- Content host is **full width**.  
- Sidebar 240px; group labels use domain color identity; left accent selection.  
- Breadcrumbs: Home › Group › Domain › Category › Setting (clickable where navigable).  
- **Home:** Identity / Security / Scan property sheets only; conflict attention when present.  
- **Domain:** category index list (Category / Settings / Attention columns). Click opens Category.  
- **Category:** setting index list (Setting / Current / Effective / Status columns). Click opens detail.  
- **Detail:** single State property sheet (Current, Effective, Source, Confidence, Reason); Available values table when ValueSemantics exist; Summary visible; secondary knowledge in expanders.  
- Cascadia Code / Consolas for raw values.  
- Mid-cyber palette retained; restrained, enterprise.  
- **Do not** put options tables, full provenance, or educational paragraphs on Domain or Category pages.  

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
