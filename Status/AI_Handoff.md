# Windows Privacy Platform
## AI / Engineer Handoff Document

**Applies to:** **Version 1.5** (in progress — elevation scaffold + detail cleanup; catalog expansion next)  
**Last updated:** 2026-08-09  

**Read this file, then `Status/Current_Status.md`, `Status/Architecture.md`, `Status/Roadmap.md` before changing code.**

---

## 1. Vision

Help humans **understand** Windows privacy and security configuration through transparent, trustworthy explanations and evidence.

**Understand first. Change later.**

Presentation standard: enterprise management console — **property sheets, list indexes, page hierarchy**. Do **not** reintroduce stacked setting cards on domain or category pages.

---

## 2. Mandatory rules

1. Solution must compile (**0 errors, 0 warnings** preferred).  
2. Never redesign the project layout without explicit approval.  
3. **No write paths are implemented.** Modify mode is an elevation authorization scaffold only.  
4. Models free of OS I/O.  
5. PolicyPrecedenceResolver is the only place for layer precedence.  
6. ValueSemantics live in catalog.  
7. CLI / TUI / **App** are presentation-only (except elevation gate).  
8. Never invent Enabled/disabled from Unknown.  
9. Never add a privacy/security score.  
10. Update Status after verification.  
11. **Do not reintroduce stacked setting cards** on Domain or Category pages.  
12. Detail pages: no expanders; minimal always-visible knowledge blocks.  
13. Preferred hierarchy: **Home → Domain → Category → Setting detail**.  
14. Column headers carry meaning on list pages.  

---

## 3. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

`ElevationService` (App) uses WindowsPrincipal; logs to auth.log / changes.log via AuditLogger.

---

## 4. Milestone pointer

| Version | Role |
|---------|------|
| **1.4** | Domain→Category→Setting hierarchy; list indexes |
| **1.5 (current)** | Modify elevation scaffold; file auth/change logs; detail expander removal; **catalog expansion in progress** |

---

## 5. Immediate next work

1. Expand ManagedObjectCatalog + PolicyCollector probes (~30–50 high-value privacy/security settings, strong focus on Windows Update depth + Defender/SmartScreen, relationships for conflicts).  
2. Matching ValueSemantics and RelationshipBinder edges.  
3. Keep layout identical; minimal further GUI polish only.  

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
