# Windows Privacy Platform
## Implementation Map — Prototype v0.6 + Step A (verified)

**Last Updated:** 2026-07-24  
**Purpose:** Map of what exists on disk and where future pieces plug in.

---

# Version history (verbose)

| Version | Summary |
|---------|---------|
| v0.1 | Seven-project skeleton, basic models |
| v0.2 | Early identity/package collectors |
| v0.3 | Services/tasks inventory expansion |
| v0.4 | Live discovery skeleton (multi-collector → InventorySnapshot) |
| v0.5 | PolicyCollector, ManagedObjectCatalog, full categorized report (archived) |
| v0.6 | Binder, ValidateAll, ObservationSummary, concise default report + high-risk watch list — **runtime verified** |
| Step A | `ProductDomain` enum + ManagedObject field; all 65 catalog entries assigned; CLI report groups by domain then SubCategory — **build + runtime verified** |

---

# Source layout (active)

```
Source/
  WindowsPrivacyPlatform.Models/     # ManagedObject (+ ProductDomain), InventorySnapshot, Catalog, ObservationSummary, Enums
  WindowsPrivacyPlatform.Core/       # OperationResult, PathConstants, PlatformException
  WindowsPrivacyPlatform.Logging/    # IAuditLogger, AuditLogger
  WindowsPrivacyPlatform.KnowledgeBase/
  WindowsPrivacyPlatform.Validator/  # SchemaValidator, RequiredFieldRule
  WindowsPrivacyPlatform.Scanner/    # Collectors, InventoryScanner, InventoryStateBinder
  WindowsPrivacyPlatform.CLI/        # Program.cs pipeline + domain-grouped report
```

Build outputs under each project’s `bin/` / `obj/` are compiled assemblies from the above — not hand-written DLL sources.

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
| PolicyCollector | Curated HKLM/HKCU probes | Verified (79 probes, 45 configured on test box) |
| *(none)* | Firewall | **Future** (`ProductDomain.Firewall` reserved) |

---

# Catalog / domain (Step A)

- `ProductDomain` enum in `Enums.cs`  
- Property on `ManagedObject`  
- Assigned in `ManagedObjectCatalog` for every privacy + policy entry  
- `ObservedItem.ProductDomain` filled by `InventoryStateBinder.ToItem`  
- CLI high-risk and `--full` reports order/group by domain then SubCategory  

Domains in use: ConsentStore, AppPrivacy, Telemetry, WindowsUpdate, Defender, Search, Edge, ActivityHistory, CloudContent, Advertising, Location, Biometrics, Device, Speech (+ Firewall reserved, Other).

---

# Pipeline insertion map (for next features)

```
CLI flags
  → Scanner.Collect*            ← add new IInventoryCollector here (e.g. FirewallCollector)
  → InventorySnapshot           ← add lists/fields for new surfaces here
  → ManagedObjectCatalog        ← new explained objects HERE FIRST; always set ProductDomain
  → InventoryStateBinder        ← effective layer resolution + relationship-aware bind HERE (Step B)
  → KnowledgeBase.Add
  → SchemaValidator.ValidateAll ← keep structural; separate risk/baseline logic from schema
  → ObservationSummary          ← extend aggregates carefully; do not invent fake scores
  → CLI report writers          ← domain grouping live; --domain filter later; no interactivity
  → Safety confirmation
```

---

# Future work (compressed)

1. ~~ProductDomain on catalog entries; report by domain.~~ **DONE (Step A)**  
2. Effective layer (User vs MachinePolicy vs alternate path) + ConflictsWith/RelatedFeature. **← next (Step B)**  
3. CapabilityCollector fix.  
4. FirewallCollector + catalog (read-only; document elevation limits).  
5. Expand Defender/Update/Telemetry/Edge curated sets with human GPO names.  
6. Optional baseline compare-only profiles.  
7. Optional explicit RiskAssessment feature (documented rules).  
8. No writes/UI until human authorises.

See **AI_Handoff.md** for full rationale and owner constraints.

---

# Deferred

Remediation, elevation UI, interactive TUI/GUI, full ADMX import, network telemetry.
