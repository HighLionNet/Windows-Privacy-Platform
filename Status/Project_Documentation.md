# Windows Privacy Platform
## Complete Technical Documentation

**Document Status:** Living  
**Current Applies To:** Prototype v0.6 (verified)  
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

# 3. Current capabilities (v0.6 verified)

- Identity, AppX packages, services, scheduled tasks  
- Privacy ConsentStore + related HKCU preferences  
- Curated policy/GPO-style registry probes (missing → Not configured)  
- ManagedObject catalog with description, rationale, risk tags  
- Bind live values to catalog; batch structural validation  
- Observation summary + high-risk watch list; `--full` dump  
- Strictly read-only, non-interactive CLI  

Not implemented: firewall collector, effective GPO-vs-UI resolution, full ADMX set, security score, baselines, writes, GUI.

---

# 4. Repository

`Archive/` · `KnowledgeBase/` · `Source/` (seven projects) · `Status/`

---

# 5. Architecture

Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI  
Explicit composition. No DI. Models have no business logic.

`bin/` DLLs = compile outputs of our projects + NuGet references.

---

# 6. Pipeline

CLI → collectors → InventorySnapshot → catalog + InventoryStateBinder → KnowledgeBase → SchemaValidator.ValidateAll → ObservationSummary → report → safety confirmation

---

# 7. Future architecture (planned)

## Domain taxonomy

Group settings by product domain (Firewall, Defender, Windows Update, Telemetry, App privacy, Edge, Search, …) with human names. Primary navigation model for reports and later UI.

## Effective layers

Model User preference vs Machine policy vs alternate policy stores. Use ManagedObject relationship fields. Binder computes effective/conflict. Prevents silent double-counting when GPO overrides Settings UI.

## Coverage policy

Expand **domain by domain** with curated high-value settings. Do not import entire gpedit. Human-readable GPO explanations are a core product differentiator.

## Optional later

Compare-only baselines; transparent risk assessment feature (not today’s H/M/L counts); still no remediation until authorised.

Insertion order and detail: **Status/AI_Handoff.md § FUTURE STEPS**.

---

# 8. Safety

No writes, no elevation, no interactive blocking prompts, no product telemetry network.

---

# 9. Build & run

```
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
```
