# Windows Privacy Platform
## Implementation Map — Prototype v0.6 FINAL

**Last Updated:** 2026-07-24  
**Purpose:** Map of what exists on disk and where future pieces plug in.  
**Note:** Intermediate foundation work briefly labeled “v0.6.5” is included here as **final v0.6**.

---

# Version history (verbose)

| Version | Summary |
|---------|---------|
| v0.1 | Seven-project skeleton, basic models |
| v0.2 | Early identity/package collectors |
| v0.3 | Services/tasks inventory expansion |
| v0.4 | Live discovery skeleton (multi-collector → InventorySnapshot) |
| v0.5 | PolicyCollector, ManagedObjectCatalog, full categorized report (archived) |
| v0.6 core | Binder, ValidateAll, ObservationSummary, concise default report + high-risk watch list — runtime verified |
| Step A | ProductDomain on all 65 catalog entries; domain-grouped reports |
| Foundation → **final v0.6** | Domain snapshot sections; IStateBinder split; PolicyPrecedenceResolver; ConfigurationResolution; SettingExplanation; SettingsQuery; NavigationBuilder; CLI decision cards |

---

# Source layout (active)

```
Source/
  WindowsPrivacyPlatform.Models/
    Enums.cs
    ManagedObject.cs
    ManagedObjectCatalog.cs
    InventorySnapshot.cs
    InventorySections.cs
    ConfigurationModels.cs      # Observation, Resolution, EffectiveState, Relationship
    SettingExplanation.cs       # Decision-card model + factory
    SettingsQuery.cs            # Application API
    NavigationModels.cs         # NavigationNode, SettingDetailView, NavigationBuilder
    ObservationSummary.cs
    ...
  WindowsPrivacyPlatform.Core/
  WindowsPrivacyPlatform.Logging/
  WindowsPrivacyPlatform.KnowledgeBase/
  WindowsPrivacyPlatform.Validator/
  WindowsPrivacyPlatform.Scanner/
    Collectors/                 # Identity, Capability, Package, Service, Task, Privacy, Policy
    Binding/
      IStateBinder.cs
      BinderHelpers.cs
      PrivacyBinder.cs
      PolicyBinder.cs
      RelationshipBinder.cs
      PolicyPrecedenceResolver.cs   # ONLY place for precedence rules
    InventoryScanner.cs
    InventoryStateBinder.cs     # Orchestrator
  WindowsPrivacyPlatform.CLI/
    Program.cs                  # Pipeline + summary + high-risk + conflict cards + --full
```

Build outputs under each project’s `bin/` / `obj/` are compiled assemblies — not hand-written DLL sources.

---

# Collectors (v0.6)

| Collector | Mechanism | Notes |
|-----------|-----------|-------|
| WindowsIdentityCollector | HKLM registry | Verified |
| CapabilityCollector | PowerShell / DISM | Often 0 results |
| PackageCollector | PowerShell Get-AppxPackage | Verified |
| ServiceCollector | ServiceController | Verified |
| ScheduledTaskCollector | schtasks CSV | Verified |
| PrivacyCollector | HKCU ConsentStore + prefs | Verified (~30 values) |
| PolicyCollector | Curated HKLM/HKCU probes | Verified (~79 probes, ~45 configured on test box) |
| *(none)* | Firewall | **Future** (`ProductDomain.Firewall` reserved) |

---

# Binding + intelligence (v0.6 final)

| Component | Role |
|-----------|------|
| PrivacyBinder | ConsentStore / privacy prefs → ManagedObject + UserPreference layer |
| PolicyBinder | Policy probes → ManagedObject + MachinePolicy / AlternatePolicyStore / UserPreference layer |
| RelationshipBinder | Wires known pairs; applies PolicyPrecedenceResolver results |
| PolicyPrecedenceResolver | Consent vs AppPrivacy codes; dual machine policy paths; generic rank comparison |
| InventoryStateBinder | Orchestrates binders + ObservationSummary |

Known relationship pairs:

- privacy.consentstore.location ↔ policy.appprivacy.location  
- privacy.consentstore.webcam ↔ policy.appprivacy.camera  
- privacy.consentstore.microphone ↔ policy.appprivacy.microphone  
- privacy.consentstore.broadFileSystemAccess ↔ policy.appprivacy.filesystem  
- policy.telemetry.allowtelemetry ↔ policy.telemetry.allowtelemetry.currentversion  

---

# Application API (UI-independent)

| Type | Role |
|------|------|
| SettingsQuery | GetByDomain/Id, GetRelatedSettings, GetConflicts, GetMachineControlledSettings, GetSettingsNeedingReview, Search, GetExplanation |
| NavigationBuilder | Domain → feature → setting tree; SettingDetailView cards |
| SettingExplanationFactory | Human decision-card text from catalog definitions |

Future TUI/GUI **must** consume these — do not fork a second model.

---

# Pipeline insertion map (for next features)

```
CLI flags
  → Scanner.Collect*                 ← add new IInventoryCollector (e.g. FirewallCollector)
  → InventorySnapshot sections       ← add lists/fields for new surfaces
  → ManagedObjectCatalog             ← new explained objects HERE FIRST; always set ProductDomain
  → PrivacyBinder / PolicyBinder     ← bind observations + layers
  → RelationshipBinder               ← add relationship pairs here
  → PolicyPrecedenceResolver         ← add precedence rules here ONLY
  → KnowledgeBase.Add
  → SchemaValidator.ValidateAll      ← structural only; keep risk/baseline logic separate
  → SettingsQuery / NavigationBuilder ← TUI consumes here
  → CLI report writers               ← presentation only
  → Safety confirmation
```

---

# Future work (compressed)

1. ~~ProductDomain + domain reports~~ **DONE**  
2. ~~Effective layer foundation + explanations + query/nav~~ **DONE (final v0.6)**  
3. **Read-only TUI** over NavigationBuilder + SettingsQuery — recommended v0.7 start  
4. Expand relationship pairs + richer SettingExplanation overrides  
5. CapabilityCollector fix  
6. FirewallCollector + catalog (read-only; document elevation limits)  
7. Expand Defender/Update/Telemetry/Edge curated sets with human GPO names  
8. Optional baseline compare-only profiles  
9. Optional explicit RiskAssessment feature (documented rules)  
10. No writes/UI frameworks until human authorises  

See **AI_Handoff.md** for full rationale and owner constraints.

---

# Deferred

Remediation, elevation UI, interactive TUI/GUI host (until models adopted by a host), full ADMX import, network telemetry, auto-hardening, security scores.
