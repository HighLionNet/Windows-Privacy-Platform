# Windows Privacy Platform
## Implementation Map — Prototype v0.6 (verified)

**Last Updated:** 2026-07-24  
**Purpose:** Map of what exists on disk and where future pieces plug in.

---

# Version history

| Version | Summary |
|---------|---------|
| v0.4 | Live discovery skeleton |
| v0.5 | PolicyCollector, catalog, full categorized report (archived) |
| v0.6 | Binder, ValidateAll, ObservationSummary, concise default report — **runtime verified** |

---

# Source layout (active)

```
Source/
  WindowsPrivacyPlatform.Models/     # ManagedObject, InventorySnapshot, Catalog, ObservationSummary
  WindowsPrivacyPlatform.Core/       # OperationResult, PathConstants, PlatformException
  WindowsPrivacyPlatform.Logging/    # IAuditLogger, AuditLogger
  WindowsPrivacyPlatform.KnowledgeBase/
  WindowsPrivacyPlatform.Validator/  # SchemaValidator, RequiredFieldRule
  WindowsPrivacyPlatform.Scanner/    # Collectors, InventoryScanner, InventoryStateBinder
  WindowsPrivacyPlatform.CLI/        # Program.cs pipeline + report
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
| *(none)* | Firewall | **Future** |

---

# Pipeline insertion map (for next features)

```
CLI flags
  → Scanner.Collect*            ← add new IInventoryCollector here (e.g. FirewallCollector)
  → InventorySnapshot           ← add lists/fields for new surfaces here
  → ManagedObjectCatalog        ← domain taxonomy + new explained objects HERE FIRST for each surface
  → InventoryStateBinder        ← effective layer resolution + relationship-aware bind HERE
  → KnowledgeBase.Add
  → SchemaValidator.ValidateAll ← keep structural; separate risk/baseline logic from schema
  → ObservationSummary          ← extend aggregates carefully; do not invent fake scores
  → CLI report writers          ← domain-grouped output; --domain filter later; no interactivity
  → Safety confirmation
```

---

# Future work (compressed)

1. ProductDomain on catalog entries; report by domain.  
2. Effective layer (User vs MachinePolicy vs alternate path) + ConflictsWith/RelatedFeature.  
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
