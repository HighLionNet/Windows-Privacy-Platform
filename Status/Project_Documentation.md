# Windows Privacy Platform
## Complete Technical Documentation

**Document Status:** Living  
**Current Applies To:** Prototype **v0.6 FINAL**  
**Last Updated:** 2026-07-24

For the exhaustive next-work roadmap, pipeline insertion points, and owner constraints, **AI_Handoff.md is authoritative**.

---

# 1. Overview

Local, declarative **privacy intelligence** platform for Windows.  
Goal: discover, **explain**, and reason about **effective configuration** of privacy/security-related settings so a user can understand the system before any change is offered.

Not a tweaking tool. Not a security score. Not a full gpedit clone.

---

# 2. Philosophy

Discover → Model → Validate → Explain → Relationships / effective layers → (future) Controlled remediation when authorised.

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
| v0.6 core | InventoryStateBinder, SchemaValidator batch, ObservationSummary, concise default report, high-risk watch list, safety confirmation — hardware verified on Win11 Pro 25H2 |
| Step A | ProductDomain taxonomy on every catalog entry; domain-grouped CLI reports |
| Foundation pass (internal “v0.6.5” → **final v0.6**) | Domain snapshot sections; split binders; ConfigurationResolution + PolicyPrecedenceResolver; SettingExplanation; SettingsQuery; NavigationBuilder; CLI decision cards |

---

# 4. Current capabilities (v0.6 final)

- Identity, AppX packages, services, scheduled tasks  
- Privacy ConsentStore + related HKCU preferences  
- Curated policy/GPO-style registry probes (missing → Not configured)  
- ManagedObject catalog with description, rationale, risk tags, **ProductDomain**  
- Bind live values with **per-layer observations**  
- **Effective configuration resolution** for known relationship pairs (Consent↔AppPrivacy, dual telemetry paths) with explicit reasons and conflicts  
- **SettingExplanation** decision-card content from catalog definitions  
- **SettingsQuery** read-only application API (domains, conflicts, related, search, explanations)  
- **NavigationBuilder** domain trees + SettingDetailView (UI-independent)  
- Batch structural validation  
- Observation summary + high-risk watch list by domain + **conflict decision cards**  
- Strictly read-only, non-interactive CLI  

Not implemented: firewall collector, TUI/GUI host, full ADMX set, security score, baselines, writes, elevation.

---

# 5. Repository

`Archive/` · `KnowledgeBase/` · `Source/` (seven projects) · `Status/`

---

# 6. Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

Explicit composition. No DI.

| Layer | Contains |
|-------|----------|
| Models | Data + pure composition (catalog, explanations, query, navigation) — **no registry** |
| Scanner | Discovery + binding + **PolicyPrecedenceResolver** |
| CLI | Presentation only |

`bin/` DLLs = compile outputs of our projects + NuGet references.

---

# 7. Pipeline

```
CLI
 → collectors
 → InventorySnapshot (domain sections)
 → PrivacyBinder / PolicyBinder (observations + layers)
 → RelationshipBinder + PolicyPrecedenceResolver (relationships + effective resolution)
 → KnowledgeBase
 → SchemaValidator.ValidateAll
 → SettingsQuery / NavigationBuilder
 → report (summary | decision cards | --full)
 → safety confirmation
```

---

# 8. Product domains (implemented)

ConsentStore, AppPrivacy, Telemetry, WindowsUpdate, Defender, Search, Edge, ActivityHistory, CloudContent, Advertising, Location, Biometrics, Device, Speech, Firewall (reserved), Other.

Every catalog ObjectId has exactly one primary domain.

---

# 9. Effective configuration model (implemented foundation)

**ConfigurationLayer** ranks (highest wins when both configured):

SecurityBaseline > MDMPolicy > MachinePolicy > AlternatePolicyStore > ApplicationPreference > UserPreference > Unknown

**ConfigurationResolution** fields:

- RawObservations  
- EffectiveValue  
- EffectiveSource  
- Confidence (Unknown/Low/Medium/High)  
- ResolutionReason (always present; never silent)  
- HasConflict  

**PolicyPrecedenceResolver** owns all precedence logic. RelationshipBinder only wires pairs and applies results.

Known pairs: 4× ConsentStore↔AppPrivacy (location, camera, microphone, filesystem); AllowTelemetry dual path.

---

# 10. Explanation + navigation (implemented foundation)

- `SettingExplanation` / `SettingExplanationFactory` — WhatIsIt, WhyItMatters, UserImpact, EnterpriseImpact, TypicalUseCases, DecisionGuidance, RelatedApplications  
- `SettingsQuery` — GetByDomain/Id, GetRelatedSettings, GetConflicts, GetMachineControlledSettings, GetSettingsNeedingReview, Search, GetExplanation  
- `NavigationBuilder` — domain tree with conflict/risk counts; SettingDetailView full card  
- Discovered strings sanitized for display; search capped at 200 characters  

---

# 11. Future architecture (planned)

1. Read-only **TUI** host over existing navigation/query models (preferred next product step)  
2. Expand curated relationships and editorial explanations  
3. CapabilityCollector fix  
4. Firewall and other domains — collector + catalog together  
5. Compare-only baselines; optional transparent risk assessment  
6. Controlled-change design doc only until authorised  

Insertion order: **Status/AI_Handoff.md § FUTURE STEPS**.

---

# 12. Safety

No writes, no elevation, no interactive blocking prompts, no product telemetry network, no auto-hardening.

---

# 13. Build & run

```
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
```
