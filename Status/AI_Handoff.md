# Windows Privacy Platform
## AI / Engineer Handoff Document

**Document status:** Authoritative continuity rules  
**Applies to:** Prototype **v0.8** (live)  
**Last updated:** 2026-07-24  
**Audience:** Next human or AI implementer who has never seen this repository  

**Read this file, then `Status/Current_Status.md`, then `Status/Architecture.md` and `Status/Roadmap.md`, before changing code.**  
Do not assume undocumented behavior from chat history.

---

## Document map

| Document | Use when |
|----------|----------|
| `Status/Current_Status.md` | Live capabilities, collectors, limitations, debt |
| `Status/Architecture.md` | Pipeline, project boundaries, evidence model, extension checklist |
| `Status/Roadmap.md` | Ordered next work (v0.9+) |
| `Status/History/v0.7.md` | Why TUI and explanation polish happened |
| `Status/History/v0.8.md` | Why Machine Overview, provenance, Firewall happened |
| `Status/Project_Documentation.md` | Long-form philosophy (still valid; some version labels lag) |
| `Status/Prototype_v0.1_Implementation_Map.md` | Historical path map (filename legacy) |
| `README.md` | Public overview and run commands |

---

## 1. Purpose of this handoff

Development must continue without the prior conversation. This file over-specifies:

- product identity and non-goals  
- permanent safety rules  
- architecture constraints  
- where to insert the next change  
- mistakes that destroy trust  

History of *what shipped* lives in `Status/History/`. Live *what exists now* lives in `Current_Status.md`.

---

## 2. Project vision (do not dilute)

**Mission:** Help humans **understand** Windows privacy and security configuration through transparent, trustworthy, **read-only** explanations and evidence.

The product answers:

- What is this setting / machine fact?  
- Why does Windows have it?  
- Who controls it?  
- What overrides it?  
- Where did this value come from?  
- How confident are we?  
- What don’t we know?  

The product does **not** exist to change Windows.

Long-term feel: interactive technical handbook / map of Windows configuration — not a registry browser and not a one-click hardener.

Development order remains:

1. Discover  
2. Model  
3. Validate  
4. Explain  
5. Relationships and effective configuration  
6. Navigate  
7. Only much later: controlled, reversible change (separate design, explicit authorization)  

**Understand first. Change later.**

---

## 3. Mandatory rules (permanent unless human overrides in writing)

1. Every change must leave the solution compiling (0 errors; avoid new warnings).  
2. Prefer runtime check on Windows after meaningful builds.  
3. Distinguish **Implemented** vs **Planned** in docs and comments.  
4. Never redesign the seven-project architecture without explicit approval.  
5. Update Status documents after verification — keep History append-only.  
6. **No write paths, no elevation, no remediation** until separately authorized.  
7. Prefer small, reviewable changes. Collectors fail-soft (never crash the pipeline).  
8. **Models** stay free of registry logic and side effects.  
9. **PolicyPrecedenceResolver** is the only place for layer precedence rules.  
10. **CLI / TUI** are presentation-only — they must not invent meaning.  
11. Never replace **Unknown** with Enabled/Disabled/False/secure/insecure.  
12. Never add a privacy score or security score as a product feature.  
13. Catalog-first for new domains: human name, explanation, domain, impact tag, discovery path, relationships.  
14. Do not bulk-import ADMX.  
15. Provenance must be honest — never invent sources.  
16. Machine Overview stays separate from configuration domain trees.  

---

## 4. Safety rules (absolute for current phase)

Prohibited:

- Registry writes  
- Service / task / package / capability / policy / firewall **changes**  
- Elevation / UAC  
- Remediation, rollback, recovery UI  
- Network calls for product telemetry  
- “Fix all” / auto-hardening  
- Executing discovered content or evaluating scripts from inventory  

Future controlled change (only if authorized) must: elevate only when required; warn on sensitive settings; prefer reversible actions; never silently modify the system. That is a **new design**, not a flag on the current pipeline.

---

## 5. Architecture (do not break)

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

See `Status/Architecture.md` for full pipeline, evidence fields, and extension checklist.

Runtime order:

1. CLI parses flags  
2. Collectors → InventorySnapshot  
3. Catalog load  
4. Bind (Privacy / Policy / Firewall mapping)  
5. RelationshipBinder + PolicyPrecedenceResolver  
6. KnowledgeBase  
7. SchemaValidator  
8. SettingsQuery / NavigationBuilder / MachineOverview  
9. Report or TUI  
10. Safety confirmation (non-TUI)  

---

## 6. Milestone pointer

| Version | Role |
|---------|------|
| v0.1–v0.5 | Skeleton → multi-collector inventory → policy catalog |
| v0.6 FINAL | Binders, precedence, explanations, query/navigation, conflict cards |
| v0.7 | TUI, explanation polish, neutral impact language — see `History/v0.7.md` |
| **v0.8** | Machine Overview, provenance fields, identity resilience, Firewall domain — see `History/v0.8.md` |
| v0.9+ | Evidence maturity, value semantics, relationship exploration — see `Roadmap.md` |

---

## 7. Onboarding sequence for a new session

1. This handoff  
2. `Current_Status.md`  
3. `Architecture.md`  
4. `Roadmap.md`  
5. Latest `History/*.md`  
6. Skim `ManagedObjectCatalog.cs`, `PolicyPrecedenceResolver.cs`, `WindowsIdentityCollector.cs`, `FirewallCollector.cs`  
7. Build and run default CLI + `--tui` on Windows  

Mental model:

```
Observation (collectors + evidence)
  → Knowledge (catalog + explanations + future value semantics)
    → Understanding (resolution + navigation + Machine Overview + TUI/GUI)
```

If a change only exposes more data without improving understanding or trust, question it.

---

## 8. How to extend a domain

1. Catalog entries with ProductDomain, human name, description, rationale, impact tag, discovery path.  
2. Explanation quality (What / Why / misconceptions / unknowns).  
3. Collector probes if needed (read-only, fail-soft, provenance filled).  
4. Bind observations with correct ConfigurationLayer + evidence fields.  
5. Wire relationships if overlaps exist.  
6. Add precedence rules only inside PolicyPrecedenceResolver.  
7. Verify navigation tree, Machine Overview (if relevant), and TUI detail card.  
8. Update Current_Status and append History when the milestone closes.  

---

## 9. Architectural mistakes to avoid

- Registry reads in Models or TUI  
- Duplicating precedence outside PolicyPrecedenceResolver  
- Parallel catalog for UI  
- Treating impact tags as a machine score  
- Bulk ADMX import  
- Silent Unknown → Disabled conversion  
- Writes “just for testing”  
- Invented provenance  
- Putting hardware inventory only inside setting relationship graphs  

---

## 10. Coding standards

- .NET 8, idiomatic C#, nullable enabled  
- Small focused classes; composition over cleverness  
- No unnecessary external dependencies  
- UI has zero domain decisions  
- Catalog and explanations are long-term intellectual property  
- Match existing style; no drive-by renames  

---

## 11. Build and run

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

If you are about to add a feature, re-read sections 2–4 and `Status/Roadmap.md`. If the feature does not improve understanding, trust, evidence quality, or maintainability, it probably does not belong in this phase.
