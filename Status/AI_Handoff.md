# Windows Privacy Platform
## AI / Engineer Handoff Document

**Document status:** Authoritative continuity document  
**Applies to:** Prototype **v0.7**  
**Last updated:** 2026-07-24  
**Audience:** Next human or AI implementer who has never seen this repository  

**Read this entire file before changing code.** Do not assume undocumented behavior from chat history.

Companion documents:

| Document | Use when |
|----------|----------|
| `Status/Current_Status.md` | Need the live verified snapshot |
| `Status/Project_Documentation.md` | Need architecture, philosophy, trust model |
| `Status/Prototype_v0.1_Implementation_Map.md` | Need file paths and insertion points |
| `README.md` | Need public overview and run instructions |

---

# 1. Purpose of this handoff

This file exists so development can continue without the prior conversation.

It deliberately over-specifies:

- product identity and non-goals  
- permanent rules  
- architecture after v0.7  
- what shipped in v0.6 vs v0.7  
- where to insert the next change  
- mistakes that look attractive but destroy trust  

---

# 2. Project vision (do not dilute)

**Mission:** Help humans **understand** Windows privacy and security configuration through transparent, trustworthy, **read-only** explanations.

The product answers questions such as:

- What is this setting?  
- Why does Windows have it?  
- Who controls it?  
- What overrides it?  
- Where did this value come from?  
- How confident are we?  
- What don’t we know?  

The product does **not** exist to change Windows.

Long-term feel: an interactive map / technical handbook of Windows privacy and security — not a registry browser and not a one-click hardener.

Development order remains:

1. Discover  
2. Model  
3. Validate  
4. Explain  
5. Relationships and effective configuration  
6. Navigate  
7. Only much later: controlled, reversible change (separate design, explicit authorization)  

Philosophy in one line: **Understand first. Change later.**

---

# 3. Mandatory rules (permanent unless human overrides in writing)

1. Every change must leave the solution compiling (0 errors; avoid new warnings).  
2. Prefer runtime check on Windows after meaningful builds.  
3. Distinguish **Implemented** vs **Planned** in docs and comments.  
4. Never redesign the seven-project architecture without explicit approval.  
5. Update Status documents after verification — not before.  
6. **No write paths, no elevation, no remediation** until separately authorized.  
7. Prefer small, reviewable changes. Collectors fail-soft (never crash the pipeline).  
8. **Models** stay free of registry logic and side effects (data + pure composition).  
9. **PolicyPrecedenceResolver** is the only place for layer precedence rules.  
10. **CLI / TUI** are presentation-only — they must not invent meaning.  
11. Never replace **Unknown** with Enabled/Disabled/False/secure/insecure.  
12. Never add a privacy score or security score as a product feature.  
13. Catalog-first for new domains: human name, explanation, domain, risk/impact tag, discovery path, relationships.  
14. Do not bulk-import ADMX.  

---

# 4. Safety rules (absolute for current phase)

Prohibited:

- Registry writes  
- Service / task / package / capability / policy / firewall changes  
- Elevation / UAC  
- Remediation, rollback, recovery UI  
- Network calls for product telemetry  
- “Fix all” / auto-hardening  
- Executing discovered content or evaluating scripts from inventory  

Future controlled change (only if authorized) must: elevate only when required; warn on sensitive settings; prefer reversible actions; never silently modify the system. That is a **new design**, not a flag on the current pipeline.

---

# 5. Architecture (do not break)

Seven projects, explicit composition, no DI container:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

- Scanner + CLI target `net8.0-windows`  
- Others target `net8.0`  
- `bin/` / `obj/` are build outputs, not source of truth  

### Layer responsibilities

| Project | Responsibility |
|---------|----------------|
| **Models** | ManagedObject, catalog, InventorySnapshot, ConfigurationObservation/Resolution, SettingExplanation, SettingsQuery, NavigationBuilder / SettingDetailView |
| **Core** | OperationResult, PathConstants, PlatformException |
| **Logging** | IAuditLogger / AuditLogger |
| **KnowledgeBase** | In-memory store of catalog entries |
| **Validator** | Structural SchemaValidator only |
| **Scanner** | Collectors; domain binders; RelationshipBinder; **PolicyPrecedenceResolver** |
| **CLI** | Flags, pipeline orchestration, report writers, **TuiHost** (presentation) |

### Runtime pipeline (actual order)

1. CLI parses flags (`--full`, `--tui`, `--help`).  
2. InventoryScanner runs collectors into InventorySnapshot.  
3. ManagedObjectCatalog loaded.  
4. InventoryStateBinder → PrivacyBinder / PolicyBinder → RelationshipBinder + PolicyPrecedenceResolver.  
5. KnowledgeBase populated.  
6. SchemaValidator.ValidateAll.  
7. SettingsQuery + NavigationBuilder available.  
8. Present: default report, `--full`, or `--tui`.  
9. Safety confirmation (CLI modes).  

---

# 6. Milestone history (how we got here)

| Version | What it was |
|---------|-------------|
| **v0.1** | Seven-project skeleton, basic models |
| **v0.2** | Early identity/package probes |
| **v0.3** | Services and scheduled tasks |
| **v0.4** | Live multi-collector discovery → InventorySnapshot |
| **v0.5** | PolicyCollector, ManagedObjectCatalog, categorized report (archived) |
| **v0.6 core** | Binder, ValidateAll, ObservationSummary, concise report + high-impact watch list |
| **v0.6 Step A** | ProductDomain on all catalog entries; domain-grouped reports |
| **v0.6 FINAL** | Domain snapshot sections; binder split; ConfigurationResolution; PolicyPrecedenceResolver; SettingExplanation; SettingsQuery; NavigationBuilder; CLI conflict cards. Archived as understanding foundation. |
| **v0.7** | Read-only TUI; explanation polish; human relationship presentation; neutral impact language; relationship expansion; CapabilityCollector transparency; documentation handbook |

**Evolution narrative:** The project began as a careful inventory scanner. By v0.6 it could bind observations, resolve known layer conflicts, and emit decision cards. v0.7 treats that intelligence as a **navigable knowledge product**: the scanner serves the knowledge layer; the knowledge layer serves understanding.

There is no separate shipping version called v0.6.5; that label was an internal foundation pass folded into final v0.6.

---

# 7. What v0.7 implemented (code-level)

### TUI

- File: `Source/WindowsPrivacyPlatform.CLI/TuiHost.cs`  
- Flag: `--tui`  
- Keyboard: ↑↓, Enter, Esc/Back, `/` search, Q quit; detail card scroll  
- Alternate screen buffer where the console supports it  
- Consumes **only** NavigationBuilder, SettingsQuery, SettingDetailView  
- **No business logic in UI**  

### Explanation system

- `SettingExplanation` + `SettingExplanationFactory`  
- Fields include WhatIsIt, WhyItMatters, impact texts, side effects, exceptions, misconceptions, unknowns, ImpactLabel  
- Writing goal: experienced engineer explaining Windows to a curious technical reader  
- Presentation separates **Observed** facts from **Interpretation**  

### Relationships

Still curated in `RelationshipBinder`:

- ConsentStore ↔ AppPrivacy (location, camera, microphone, filesystem)  
- Dual telemetry policy paths  
- Advertising ID user ↔ GPO  
- Location ConsentStore ↔ DisableLocation; Find My Device related  
- Tailored Experiences ↔ Telemetry  
- Activity History group  
- Search group  

### CapabilityCollector

- Tries powershell and pwsh; installed-only then full; DISM fallbacks  
- Fail-soft  
- CLI explicitly explains zero results as collection uncertainty  

---

# 8. Effective configuration model

### ConfigurationLayer (conceptual strength for documentation)

SecurityBaseline > MDMPolicy > MachinePolicy > AlternatePolicyStore > ApplicationPreference > UserPreference > Unknown

### PolicyPrecedenceResolver methods

- `ResolveConsentVsAppPrivacy` — AppPrivacy codes 0/1/2  
- `ResolveAlternateMachinePolicyPaths` — dual machine stores  
- `ResolveByLayerRank` — generic rank comparison; ties → Unknown + conflict  

Never silently guess. Always emit a human-readable `ResolutionReason`.

---

# 9. Onboarding: how a new engineer should think

### Read first

1. This handoff  
2. `Current_Status.md`  
3. `Project_Documentation.md`  
4. Skim `ManagedObjectCatalog.cs` and `PolicyPrecedenceResolver.cs`  
5. Run default CLI and `--tui` on a Windows machine  

### Mental model

```
Observation (collectors)
    → Knowledge (catalog + explanations)
        → Understanding (resolution + navigation + TUI)
```

If a change only exposes more data without improving understanding, question it.

### How to extend a domain

1. Add catalog entries with ProductDomain, human name, description, rationale, impact tag, discovery path.  
2. Add collector probes if needed (read-only, fail-soft).  
3. Bind observations with correct ConfigurationLayer.  
4. Wire relationships if overlaps exist.  
5. Add precedence rules only inside PolicyPrecedenceResolver.  
6. Ensure explanations still read as documentation.  
7. Verify navigation tree and TUI detail card.  

### How to write explanations

- Explain what Windows is doing and why the feature exists.  
- Separate facts from interpretation.  
- State misconceptions and unknowns explicitly.  
- Never alarmist; never “you should disable this.”  
- Prefer teaching one non-obvious fact per card.  

### Architectural mistakes to avoid

- Putting registry reads in Models or TUI  
- Duplicating precedence logic outside PolicyPrecedenceResolver  
- Inventing a second catalog for the UI  
- Treating RiskLevel / High-impact counts as a machine score  
- Bulk ADMX import  
- Silent conversion of Unknown → Disabled  
- Adding writes “just for testing”  

---

# 10. Rejected or deferred ideas (and why)

| Idea | Status | Why |
|------|--------|-----|
| Full ADMX import | Rejected as near-term path | Quantity without explanation destroys the product |
| Privacy/security score | Rejected | Pretends certainty; invites marketing misuse |
| Auto-hardening | Deferred indefinitely | Violates understand-first; safety risk |
| Terminal.Gui / heavy UI framework | Deferred | v0.7 uses pure Console TUI; models stay UI-independent |
| AI relationship inference | Rejected for now | Curated knowledge is the trust surface |
| Remediation in same binary path | Deferred | Requires separate elevation and UX design |
| Parallel data model for TUI | Rejected | NavigationBuilder/SettingsQuery are the API |

---

# 11. Future steps (ordered priorities, not dates)

### Near term (v0.8 candidates)

1. Local runtime verification of v0.7 on Windows 11 25H2  
2. Firewall: catalog first, then read-only collector, then relationships  
3. More curated relationships and deeper editorial explanations for top settings  
4. Optional `--domain=` CLI filter via SettingsQuery  

### Medium term (v0.9 candidates)

1. Defender / Update / Telemetry / Edge curated expansion  
2. Compare-only baselines (no enforcement)  
3. Stronger provenance display consistency  
4. Capability collection diagnosis if still empty after elevation experiments (docs only unless a safe read path is found)  

### v1.0 vision

A polished, trustworthy **read-only** Windows privacy knowledge product: stable TUI (or thin GUI), high-quality catalog, honest effective-layer reasoning, clear unknowns, no scores, no silent writes.

### Far future (design-doc only until authorized)

Controlled reversible change with elevation-on-demand, per-setting warnings, and auditability — never mixed into the current read-only pipeline without a deliberate architecture pass.

---

# 12. Build and run

```
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
dotnet run -c Release -- --tui
dotnet run -c Release -- --help
```

Confirm: inventory lines, validator pass count, conflict cards or clean conflict section, TUI navigation, safety confirmation on non-TUI modes.

---

# 13. Coding standards (project-specific)

- .NET 8, idiomatic C#, nullable enabled  
- Small focused classes; composition over cleverness  
- No unnecessary external dependencies  
- UI has zero domain decisions  
- Catalog and explanations are the long-term intellectual property — treat with care  
- Prefer readable prose in user-visible strings  
- Match existing style; no drive-by renames or folder reshuffles  

---

# 14. Development model

- Architecture / safety / continuity review may involve external design chat  
- Implementation on GitHub: `HighLionNet/Windows-Privacy-Platform` (main)  
- Human: local build, runtime verification, archives, direction approval  

---

# END OF HANDOFF

If you are about to add a feature, re-read sections 2, 3, 4, and 9. If the feature does not improve understanding, trust, or maintainability, it probably does not belong in this phase.
