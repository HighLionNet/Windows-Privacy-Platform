# Windows Privacy Platform
## Current Status

**Project Name:** Windows Privacy Platform  
**Current Development Version:** Prototype **v0.6 FINAL**  
**Previous Versions:** v0.5 (archived) → v0.4 → v0.3 → v0.2 → v0.1  
**Document Status:** Authoritative Current State  
**Last Updated:** 2026-07-24  
**Runtime:** Verified on Windows 11 Pro 25H2 (build 26200) — build 0 warnings / 0 errors  
**Note:** Intermediate “v0.6.5” foundation work is **folded into final v0.6**. Human is archiving this state as the official v0.6 backup.

---

# Purpose

Exact current state of the active repository for future AI sessions.  
For full roadmap logic and insertion points, also read `Status/AI_Handoff.md` (authoritative continuity).

---

# Version history (verbose)

| Version | Summary |
|---------|---------|
| v0.1 | Initial seven-project skeleton and basic models |
| v0.2 | Early identity/package collector experiments |
| v0.3 | Services/tasks inventory paths expanded |
| v0.4 | Live multi-collector discovery skeleton → InventorySnapshot |
| v0.5 | PolicyCollector, ManagedObjectCatalog (human names + rationales), full categorized report — **archived by human** |
| v0.6 core | InventoryStateBinder, SchemaValidator.ValidateAll, ObservationSummary, concise default report + high-risk watch list, safety confirmation — **hardware verified** |
| Step A (on v0.6) | `ProductDomain` enum + property; all 65 catalog entries assigned; reports group by domain then SubCategory |
| Foundation pass (internal “v0.6.5”, now **final v0.6**) | Domain-organized snapshot; split binders; ConfigurationResolution + PolicyPrecedenceResolver; SettingExplanation; SettingsQuery; NavigationBuilder; CLI conflict **decision cards** |

---

# What the app does today (medium-level)

Non-interactive CLI pipeline:

1. CLI starts collectors via InventoryScanner.  
2. Collectors **read** (never write): registry (identity, privacy ConsentStore, curated policy keys), PowerShell (packages; capabilities attempt), ServiceController (services), schtasks (tasks).  
3. Results go into a **domain-organized InventorySnapshot**.  
4. ManagedObjectCatalog (static explained settings, each with a **ProductDomain**) is bound via orchestrated binders:
   - PrivacyBinder / PolicyBinder attach `CurrentState` + per-layer `ConfigurationObservation`  
   - RelationshipBinder wires known pairs and runs **PolicyPrecedenceResolver** → `ConfigurationResolution` (effective value, source, reason, conflict)  
5. KnowledgeBase stores entries; SchemaValidator batch-checks **structure** (not security score).  
6. **SettingsQuery** + **NavigationBuilder** expose a UI-independent application API.  
7. CLI prints:
   - Observation/risk **summary** (including layer conflict count, needs-review count, nav domain count)  
   - High-risk **watch list** grouped by domain  
   - **Decision cards** for effective-layer conflicts (what / why / effective / source / layers / related)  
   - Or `--full` dump under domain headers with What/Why lines  
8. Safety confirmation: no modifications, no elevation.

`bin/` DLLs are **compiled build outputs** from our C# projects + NuGet dependencies — not separately authored binary sources.

**Not present:** firewall collector, interactive TUI/GUI host, writes, overall security score, full GPO library, remediation.

---

# Verified runtime sample (v0.6 final)

```
Identity : Windows 11 Pro | 25H2 | Build 26200
Capabilities : 0 | Packages : ~165 | Services : ~303 | Tasks : ~247
Privacy settings : ~30 | Policy probes : ~79 (configured: ~45)
KnowledgeBase: 65 | Validator: 65 passed / 0 failed
Observed ~63 / not ~2 | High-risk configured ~25 | Medium-risk configured ~22
Layer conflicts : shown when dual policy paths or force-policy pairs disagree
Nav domains : enumerated from catalog
```

When conflicts exist, default mode prints decision cards (title, domain path, risk, what/why, effective value + source + reason, layers, related settings, guidance).

### Meaning of risk lines

- **Not a security score.**  
- Catalog H/M/L tags are static.  
- “High-risk configured” = high-impact topics that have a configured value worth reviewing.  
- Deny on Location/Microphone can still appear under high-risk *category* because the capability is sensitive.  
- Effective resolution tells **who wins and why**; it does not auto-label “secure/insecure.”

---

# Implementation status

| Area | Status |
|------|--------|
| Seven-project architecture | Complete |
| Discover (7 collectors) | Live; Capabilities often 0 |
| Model catalog (~65 explained objects) | Live |
| Product domain taxonomy (`ProductDomain`) | **Complete** |
| Report grouped by domain | **Complete** |
| Domain-organized InventorySnapshot | **Complete** |
| Split binders (IStateBinder + Privacy/Policy/Relationship) | **Complete** |
| ConfigurationLayer + ConfigurationObservation | **Complete** |
| ConfigurationResolution + PolicyPrecedenceResolver | **Complete** (known pairs) |
| SettingExplanation + factory | **Complete** |
| SettingsQuery application API | **Complete** |
| NavigationBuilder / SettingDetailView | **Complete** (data only; no TUI host) |
| CLI conflict decision cards | **Complete** |
| Bind CurrentState | Live |
| Structural ValidateAll | Live |
| Firewall discovery | **Planned** |
| CapabilityCollector reliability | **Planned** |
| More relationship pairs / editorial explanations | **Planned** |
| TUI host over existing models | **Planned (v0.7 candidate)** |
| Baselines / recommended sets | **Planned (compare-only first)** |
| Formal risk assessment feature | **Optional planned** |
| Writes / elevation / interactive hardening | **Deferred** |

---

# Product domains in catalog

ConsentStore, AppPrivacy, Telemetry, WindowsUpdate, Defender, Search, Edge, ActivityHistory, CloudContent, Advertising, Location, Biometrics, Device, Speech, Firewall (reserved empty), Other.

---

# Key source files (v0.6 final map)

### Models
- `Enums.cs` — ProductDomain, ConfigurationLayer, RelationshipKind, EffectiveConfidence, …  
- `ManagedObject.cs` — definition + Observation / StructuredRelationships  
- `ManagedObjectCatalog.cs` — 65 explained entries  
- `InventorySnapshot.cs` + `InventorySections.cs` — domain sections + compat accessors  
- `ConfigurationModels.cs` — ConfigurationObservation, ConfigurationResolution, EffectiveState, SettingRelationship  
- `SettingExplanation.cs` — decision-card model + factory  
- `SettingsQuery.cs` — read-only application API  
- `NavigationModels.cs` — NavigationNode, SettingDetailView, NavigationBuilder  

### Scanner
- Collectors under `Collectors/`  
- `Binding/IStateBinder.cs`, `PrivacyBinder.cs`, `PolicyBinder.cs`, `RelationshipBinder.cs`, `BinderHelpers.cs`  
- `Binding/PolicyPrecedenceResolver.cs` — **only** place for precedence rules  
- `InventoryStateBinder.cs` — orchestrator  

### CLI
- `Program.cs` — pipeline + summary + high-risk + conflict cards + `--full`  

---

# Future steps (ordered — summary)

Full detail: **AI_Handoff.md § FUTURE STEPS**.

1. ~~Domain taxonomy~~ **DONE**  
2. ~~Effective layer foundation + explanations + query/nav models~~ **DONE (final v0.6)**  
3. **Thin read-only TUI** consuming NavigationBuilder + SettingsQuery — **recommended v0.7 start**  
4. Expand relationship pairs + richer explanation overrides for top settings  
5. **CapabilityCollector** reliability pass  
6. **Expand discovery by domain** (Firewall first), each with catalog explanations — not full gpedit import  
7. Optional `--domain=` filter; compare-only baselines; transparent risk assessment  
8. Controlled-change **design doc only** until authorised  

### Balance rule (owner)

Maximize relevant privacy/security coverage **domain by domain** without a self-strangling “every ADMX setting” model. Human-readable explanations and effective-layer honesty are the product differentiators.

---

# Safety

Strictly read-only. No registry/service/task/package/policy writes. No elevation. No remediation. No interactive prompts. No product telemetry network.

---

# Overall

**Prototype v0.6 FINAL** is the archived understanding foundation: discover → explain → resolve effective configuration for known overlaps → query/navigate in a UI-independent way → present decision cards.  
Next product step is a **read-only TUI** (or continued curated domain expansion) — still understand-first, still no writes.
