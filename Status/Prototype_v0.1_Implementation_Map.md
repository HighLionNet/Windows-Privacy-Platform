# Windows Privacy Platform
## Implementation Map — Prototype v0.7

**Last updated:** 2026-07-24  
**Purpose:** Map of what exists on disk, where logic lives, and where future pieces plug in.  
**Filename note:** Historical name retained for continuity; content tracks **v0.7**.

---

# 1. Version timeline (compressed)

| Version | Summary |
|---------|---------|
| v0.1–v0.3 | Skeleton → identity/packages → services/tasks |
| v0.4 | Live multi-collector InventorySnapshot |
| v0.5 | PolicyCollector + ManagedObjectCatalog (archived) |
| v0.6 FINAL | Binders, precedence, explanations, SettingsQuery, NavigationBuilder, CLI decision cards |
| **v0.7** | TUI, explanation polish, human relationships, neutral impact language, capability transparency |

---

# 2. Source layout (active)

```
Source/
  WindowsPrivacyPlatform.sln
  WindowsPrivacyPlatform.Models/
    Enums.cs
    ManagedObject.cs
    ManagedObjectCatalog.cs          # ~65 curated entries
    InventorySnapshot.cs
    InventorySections.cs
    ConfigurationModels.cs           # Observation, Resolution, Relationship
    SettingExplanation.cs            # Card model + factory
    SettingsQuery.cs                 # Application query API
    NavigationModels.cs              # NavigationNode, SettingDetailView, NavigationBuilder
    ObservationSummary.cs
    ...
  WindowsPrivacyPlatform.Core/
  WindowsPrivacyPlatform.Logging/
  WindowsPrivacyPlatform.KnowledgeBase/
  WindowsPrivacyPlatform.Validator/
  WindowsPrivacyPlatform.Scanner/
    WindowsIdentityCollector.cs
    CapabilityCollector.cs
    PackageCollector.cs
    ServiceCollector.cs
    ScheduledTaskCollector.cs
    PrivacyCollector.cs
    PolicyCollector.cs
    InventoryScanner.cs
    InventoryStateBinder.cs          # Orchestrator
    Binding/
      IStateBinder.cs
      BinderHelpers.cs
      PrivacyBinder.cs
      PolicyBinder.cs
      RelationshipBinder.cs
      PolicyPrecedenceResolver.cs    # ONLY precedence rules
  WindowsPrivacyPlatform.CLI/
    Program.cs                       # Pipeline + reports + flags
    TuiHost.cs                       # Read-only keyboard explorer
```

Build outputs under `bin/` / `obj/` are compiled assemblies — not hand-authored binary sources.

Root also contains:

```
README.md
Status/           # Engineering journal (this folder)
KnowledgeBase/    # Legacy/top-level duplicates may exist; Source/ is authoritative for build
```

---

# 3. Collectors

| Collector | Mechanism | Notes |
|-----------|-----------|-------|
| WindowsIdentityCollector | HKLM registry | Version / edition / build |
| CapabilityCollector | powershell, pwsh, DISM | Often empty non-elevated; treat as Unknown |
| PackageCollector | Get-AppxPackage | Current user |
| ServiceCollector | ServiceController | Read-only |
| ScheduledTaskCollector | schtasks CSV | Read-only |
| PrivacyCollector | HKCU ConsentStore + prefs | ~30 values |
| PolicyCollector | Curated HKLM/HKCU probes | ~79 probes |
| *(none)* | Firewall | Future; domain reserved |

---

# 4. Binding and intelligence

| Component | Role |
|-----------|------|
| PrivacyBinder | Privacy settings → CurrentState + UserPreference layer |
| PolicyBinder | Policy probes → layers by hive/path |
| RelationshipBinder | Curated pairs; applies resolver results |
| PolicyPrecedenceResolver | Consent vs AppPrivacy; dual telemetry paths; layer rank |
| InventoryStateBinder | Orchestrates bind + ObservationSummary |

### Known relationship groups (v0.7)

**Consent ↔ AppPrivacy**

- location, webcam/camera, microphone, broadFileSystemAccess/filesystem  

**Alternate paths**

- policy.telemetry.allowtelemetry ↔ …currentversion  

**User vs GPO**

- privacy.advertisingid.enabled ↔ policy.advertising.disabledbygpo  

**Related documentation edges**

- Location kill-switch, Find My Device  
- Tailored Experiences ↔ telemetry  
- Activity History trio  
- Search web/location/Cortana related edges  

---

# 5. Application API (UI-independent)

| Type | Role |
|------|------|
| SettingsQuery | Domain/id/related/conflicts/search/explanations |
| NavigationBuilder | Domain tree + SettingDetailView |
| SettingExplanationFactory | Documentation-style card text |

**Rule:** Future GUI must consume these. Do not invent a parallel model in the UI project.

---

# 6. Presentation

| Entry | File | Behavior |
|-------|------|----------|
| Default report | Program.cs | Summary + high-impact watch list + conflict cards |
| `--full` | Program.cs | Domain-grouped dump |
| `--tui` | TuiHost.cs | Interactive explorer |
| `--help` | Program.cs | Usage |

TUI is presentation-only. Detail cards separate Observed vs Interpretation and show provenance.

---

# 7. Pipeline insertion map

```
CLI flags
  → new IInventoryCollector              ← discovery surface
  → InventorySnapshot section fields     ← storage
  → ManagedObjectCatalog                 ← ALWAYS first for product meaning
  → PrivacyBinder / PolicyBinder         ← observations + layers
  → RelationshipBinder                   ← edges
  → PolicyPrecedenceResolver             ← precedence ONLY here
  → KnowledgeBase.Add
  → SchemaValidator                      ← structural only
  → SettingsQuery / NavigationBuilder    ← UI consumes here
  → Program / TuiHost                    ← presentation only
```

---

# 8. Design constraints checklist (before PR)

- [ ] Read-only?  
- [ ] No elevation?  
- [ ] Models free of registry I/O?  
- [ ] Precedence only in PolicyPrecedenceResolver?  
- [ ] UI free of business decisions?  
- [ ] Unknown preserved where evidence is weak?  
- [ ] Catalog explanation quality acceptable?  
- [ ] Solution builds Release?  

---

# 9. Future plug-ins (not started)

| Feature | Insert at |
|---------|-----------|
| FirewallCollector | New collector + catalog ProductDomain.Firewall |
| `--domain=` filter | CLI + SettingsQuery |
| Baseline compare-only | DesiredState fields already exist; report section only |
| MDM collector | New observations with ConfigurationLayer.MDMPolicy |
| GUI host | New project consuming NavigationBuilder only |

---

# 10. Deferred

Remediation, elevation UX, bulk ADMX import, scoring, network product telemetry, auto-hardening, inferred relationship graphs.

See `Status/AI_Handoff.md` for rationale and owner constraints.
