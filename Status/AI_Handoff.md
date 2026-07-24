# Windows Privacy Platform
## AI / Engineer Handoff Document

**Document status:** Authoritative continuity rules  
**Applies to:** Prototype **v0.9** (live)  
**Last updated:** 2026-07-24  
**Audience:** Next human or AI implementer who has never seen this repository  

**Read this file, then `Status/Current_Status.md`, then `Status/Architecture.md` and `Status/Roadmap.md`, before changing code.**  
Do not assume undocumented behavior from chat history.

---

## Document map

| Document | Use when |
|----------|----------|
| `Status/Current_Status.md` | Live capabilities, collectors, limitations |
| `Status/Architecture.md` | Pipeline, ValueSemantics, evidence, extension checklist |
| `Status/Roadmap.md` | Ordered next work (post-v0.9) |
| `Status/History/v0.7.md` | TUI / explanation polish |
| `Status/History/v0.8.md` | Machine Overview, provenance, Firewall |
| `Status/History/v0.9.md` | Value semantics, educational resolution |
| `Status/Project_Documentation.md` | Long-form philosophy |
| `README.md` | Public overview and run commands |

---

## 1. Purpose of this handoff

Development must continue without prior conversation. This file over-specifies product identity, safety, architecture constraints, and where to insert the next change.

---

## 2. Project vision (do not dilute)

**Mission:** Help humans **understand** Windows privacy and security configuration through transparent, trustworthy, **read-only** explanations and evidence.

Answers: what is this; why Windows has it; who controls it; what overrides it; where the value came from; what the raw value means; how confident we are; what we do not know.

Does **not** exist to change Windows.

**Understand first. Change later.**

---

## 3. Mandatory rules

1. Solution must compile (0 errors).  
2. Prefer runtime check on Windows after meaningful builds.  
3. Distinguish Implemented vs Planned.  
4. Never redesign the seven-project architecture without explicit approval.  
5. Update Status after verification; History append-only.  
6. **No write paths, no elevation, no remediation** until separately authorized.  
7. Prefer small, reviewable changes. Collectors fail-soft.  
8. **Models** free of registry logic and side effects.  
9. **PolicyPrecedenceResolver** is the only place for layer precedence.  
10. **ValueSemantics** live in catalog; resolvers/CLI/UI must not invent raw-code meanings.  
11. **CLI / TUI** presentation-only.  
12. Never replace Unknown with invented Enabled/Disabled.  
13. Never add a privacy/security score.  
14. Catalog-first for new domains (include ValueSemantics when values are coded).  
15. Do not bulk-import ADMX.  
16. Provenance must be honest.  
17. Machine Overview stays separate from configuration domain trees.  

---

## 4. Safety rules (absolute for current phase)

No registry/service/task/package/policy/firewall **changes**; no elevation; no remediation; no product network telemetry; no executing discovered content.

---

## 5. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

Mental model:

```
Observation (collectors + evidence)
  → Knowledge (catalog + ValueSemantics + explanations)
    → Understanding (resolution + navigation + Machine Overview + TUI)
```

---

## 6. Milestone pointer

| Version | Role |
|---------|------|
| v0.6 FINAL | Binders, precedence, explanations, query/navigation |
| v0.7 | TUI, explanation polish |
| v0.8 | Machine Overview, provenance, Firewall |
| **v0.9** | ValueSemantics, educational resolution, evidence maturity — see `History/v0.9.md` |
| Next | Surface knowledge in UI; relationship queries; careful domain depth |

---

## 7. Onboarding sequence

1. This handoff  
2. `Current_Status.md`  
3. `Architecture.md`  
4. `Roadmap.md`  
5. `History/v0.9.md` (and prior)  
6. Skim `ManagedObjectCatalog.cs`, `ValueSemantics.cs`, `PolicyPrecedenceResolver.cs`  
7. Build and run default CLI + `--tui` on Windows  

---

## 8. How to extend a domain

1. Catalog entry + description + rationale + **ValueSemantics** for coded values.  
2. WhenIgnored / CommonMisconception where known.  
3. Collector if needed (read-only, provenance).  
4. Bind with correct layer + evidence.  
5. Relationships if overlaps.  
6. Precedence only in PolicyPrecedenceResolver (consume maps; do not hardcode digits).  
7. Verify navigation / detail / safety.  
8. Update Current_Status; append History when milestone closes.  

---

## 9. Architectural mistakes to avoid

- Hardcoding `"0"`/`"1"`/`"2"` meanings in resolvers or UI  
- Registry reads in Models or TUI  
- Duplicating precedence outside PolicyPrecedenceResolver  
- Parallel catalog for UI  
- Treating impact tags as a machine score  
- Silent Unknown → Disabled  
- Writes “just for testing”  
- Invented provenance  

---

## 10. Build and run

```
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
dotnet run -c Release -- --tui
```

---

## END OF HANDOFF

If a feature does not improve understanding, trust, evidence quality, or maintainability, it probably does not belong in this phase.
