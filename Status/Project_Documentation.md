# Windows Privacy Platform
## Complete Technical Documentation

**Document Status:** Living  
**Current Applies To:** Prototype v0.6 + Step A (ProductDomain) — verified  
**Last Updated:** 2026-07-24

For the exhaustive next-work roadmap, pipeline insertion points, and owner constraints, **AI_Handoff.md is authoritative**.

---

# 1. Overview

Local, declarative privacy intelligence platform for Windows.  
Goal: discover and **explain** privacy/security-related configuration (including human-readable policy/GPO surfaces) before any change capability exists.

---

# 2. Philosophy

Discover → Model → Validate → Report → Relationships (incl. effective layers) → Controlled remediation (future, authorised separately).

**Understand first. Change later.**

---

# 3. Version history (verbose)

| Version | Summary |
|---------|---------|
| v0.1 | Seven-project solution skeleton, basic models |
| v0.2 | Early identity and package discovery |
| v0.3 | Services and scheduled-task inventory |
| v0.4 | Live multi-collector discovery skeleton → InventorySnapshot |
| v0.5 | PolicyCollector, ManagedObjectCatalog with descriptions/rationales, full categorized report (archived) |
| v0.6 | InventoryStateBinder, SchemaValidator batch, ObservationSummary, concise default report, high-risk watch list, safety confirmation — hardware verified on Win11 Pro 25H2 |
| Step A | ProductDomain taxonomy on every catalog entry; domain-grouped CLI reports — build and runtime verified |

---

# 4. Current capabilities (v0.6 + Step A verified)

- Identity, AppX packages, services, scheduled tasks  
- Privacy ConsentStore + related HKCU preferences  
- Curated policy/GPO-style registry probes (missing → Not configured)  
- ManagedObject catalog with description, rationale, risk tags, **and ProductDomain**  
- Bind live values to catalog; batch structural validation  
- Observation summary + high-risk watch list **grouped by domain**; `--full` dump under domain headers  
- Strictly read-only, non-interactive CLI  

Not implemented: firewall collector, effective GPO-vs-UI resolution, full ADMX set, security score, baselines, writes, GUI.

---

# 5. Repository

`Archive/` · `KnowledgeBase/` · `Source/` (seven projects) · `Status/`

---

# 6. Architecture

Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI  
Explicit composition. No DI. Models have no business logic.

`bin/` DLLs = compile outputs of our projects + NuGet references.

---

# 7. Pipeline

CLI → collectors → InventorySnapshot → catalog (with ProductDomain) + InventoryStateBinder → KnowledgeBase → SchemaValidator.ValidateAll → ObservationSummary → domain-grouped report → safety confirmation

---

# 8. Product domains (implemented)

ConsentStore, AppPrivacy, Telemetry, WindowsUpdate, Defender, Search, Edge, ActivityHistory, CloudContent, Advertising, Location, Biometrics, Device, Speech, Firewall (reserved), Other.

Every catalog ObjectId has exactly one primary domain. SubCategory remains secondary classification.

---

# 9. Future architecture (planned)

## Effective layers (Step B — next)

Model User preference vs Machine policy vs alternate policy stores. Use ManagedObject relationship fields. Binder computes effective/conflict. Prevents silent double-counting when GPO overrides Settings UI. Verified dual telemetry paths on the test machine motivate this work.

## Coverage policy

Expand **domain by domain** with curated high-value settings. Do not import entire gpedit. Human-readable GPO explanations are a core product differentiator. New entries must set ProductDomain.

## Optional later

Compare-only baselines; transparent risk assessment feature (not today’s H/M/L counts); still no remediation until authorised.

Insertion order and detail: **Status/AI_Handoff.md § FUTURE STEPS**.

---

# 10. Safety

No writes, no elevation, no interactive blocking prompts, no product telemetry network.

---

# 11. Build & run

```
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
```
