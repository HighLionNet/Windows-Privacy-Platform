# Windows Privacy Platform
## Current Status

**Project Name:** Windows Privacy Platform  
**Current Development Version:** Prototype v0.6 + Step A (ProductDomain taxonomy)  
**Previous Versions:** v0.5 (archived) → v0.4 → v0.3 → v0.2 → v0.1  
**Document Status:** Authoritative Current State  
**Last Updated:** 2026-07-24  
**Runtime:** Verified on Windows 11 Pro 25H2 (build 26200) — build 0 warnings / 0 errors; domain-grouped report confirmed

---

# Purpose

Exact current state of the active repository for future AI sessions.  
For full roadmap logic (domains, effective layers, insertion points), also read `Status/AI_Handoff.md`.

---

# Version history (verbose)

| Version | Summary |
|---------|---------|
| v0.1 | Initial seven-project skeleton and basic models |
| v0.2 | Early identity/package collector experiments |
| v0.3 | Services/tasks inventory paths expanded |
| v0.4 | Live multi-collector discovery skeleton → InventorySnapshot |
| v0.5 | PolicyCollector, ManagedObjectCatalog (human names + rationales), full categorized report — **archived by human** |
| v0.6 | InventoryStateBinder, SchemaValidator.ValidateAll, ObservationSummary, concise default report + high-risk watch list, safety confirmation — **hardware verified** |
| Step A (on v0.6) | `ProductDomain` enum + property; all 65 catalog entries assigned; high-risk and `--full` reports group by domain then SubCategory — **build + runtime verified 2026-07-24** |

---

# What the app does today (medium-level)

Non-interactive CLI pipeline:

1. CLI starts collectors via InventoryScanner.  
2. Collectors **read** (never write): registry (identity, privacy ConsentStore, curated policy keys), PowerShell (packages; capabilities attempt), ServiceController (services), schtasks (tasks).  
3. Results go into InventorySnapshot.  
4. ManagedObjectCatalog (static explained settings, each with a **ProductDomain**) is bound to live values via InventoryStateBinder (`CurrentState`).  
5. KnowledgeBase stores entries; SchemaValidator batch-checks catalog structure.  
6. CLI prints observation/risk **summary** and high-risk **watch list** grouped by domain (or `--full` dump under `## Domain:` headers).  
7. Safety confirmation: no modifications, no elevation.

`bin/` DLLs are **compiled build outputs** from our C# projects + NuGet dependencies — not separately authored binary sources.

**Not present:** firewall collector, interactive UI, writes, overall security score, full GPO library, effective GPO-vs-UI resolution.

---

# Verified runtime sample (v0.6 + Step A)

```
Identity : Windows 11 Pro | 25H2 | Build 26200
Capabilities : 0 | Packages : 165 | Services : 303 | Tasks : 247
Privacy settings : 30 | Policy probes : 79 (configured: 45)
KnowledgeBase: 65 | Validator: 65 passed / 0 failed
Observed 63 / not 2 | High-risk configured 25 | Medium-risk configured 22
Privacy Allow/Deny/Prompt: 18 / 2 / 0
```

High-risk lines now include domain, e.g. `[ConsentStore/ConsentStore] Location`, `[AppPrivacy/AppPrivacy] Let Apps Access Camera (GPO)`, `[Telemetry/Telemetry] Allow Telemetry (GPO)`.

### Meaning of risk lines

- **Not a security score.**  
- Catalog H/M/L tags are static.  
- “High-risk configured” = high-impact topics that have a configured value worth reviewing.  
- Deny on Location/Microphone can still appear under high-risk *category* because the capability is sensitive.

---

# Implementation status

| Area | Status |
|------|--------|
| Seven-project architecture | Complete |
| Discover (7 collectors) | Live; Capabilities often 0 |
| Model catalog (~65 explained objects) | Live |
| Product domain taxonomy (`ProductDomain`) | **Complete (Step A)** |
| Report grouped by domain | **Complete (Step A)** |
| Bind CurrentState | Live |
| Structural ValidateAll | Live |
| Concise + full report | Live |
| Effective layer / GPO vs UI | **Planned (Step B — next)** |
| Firewall discovery | **Planned (Step D)** |
| CapabilityCollector reliability | **Planned (Step C)** |
| Baselines / recommended sets | **Planned (compare-only first)** |
| Formal risk assessment feature | **Optional planned** |
| Relationships graph | **Planned** |
| Writes / elevation / interactive UI | **Deferred** |

---

# Product domains in catalog

ConsentStore, AppPrivacy, Telemetry, WindowsUpdate, Defender, Search, Edge, ActivityHistory, CloudContent, Advertising, Location, Biometrics, Device, Speech, Firewall (reserved empty), Other.

---

# Future steps (ordered — summary)

Full detail and pipeline insertion points: **AI_Handoff.md § FUTURE STEPS**.

1. ~~**Domain taxonomy** on catalog (Models)~~ — **DONE**  
2. **Effective layer + relationships** (Models + InventoryStateBinder + report) — resolve GPO vs ConsentStore vs alternate policy paths; show conflicts. **← next**  
3. **CapabilityCollector** reliability pass.  
4. **Expand discovery by domain** (Firewall first among missing surfaces), each with catalog explanations — **not** full gpedit import.  
5. Report polish (e.g. optional `--domain=` filter); non-interactive flags only.  
6. **Optional baselines** (desired vs observed, compare-only).  
7. **Optional transparent risk assessment** (separate from today’s watch list).  
8. Relationships presentation polish.  
9. Controlled-change **design doc only** until authorised.  
10. Terminal UI only after report is navigable and layers are clear.

### Balance rule (owner)

Maximize relevant privacy/security coverage **domain by domain** without a self-strangling “every ADMX setting” model. Human-readable GPO area is a primary value prop; overlap must be explained, not doubled blindly.

---

# Safety

Strictly read-only. No registry/service/task/package/policy writes. No elevation. No remediation. No interactive prompts.

---

# Overall

v0.6 + Step A verified. Next work is **Step B effective layers**, then CapabilityCollector fix and domain expansion (Firewall) — still understand-first.
