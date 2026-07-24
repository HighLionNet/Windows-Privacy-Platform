# Windows Privacy Platform

**Current milestone:** Prototype v0.6 + Step A (ProductDomain taxonomy) — **runtime verified** on Windows 11 Pro 25H2 (build 26200)  
**Previous:** v0.5 (archived) → v0.4 → v0.3 → v0.2 → v0.1

Local, declarative privacy intelligence platform for Windows.  
Philosophy: **Understand first. Change later.**

## Version history

| Version | Summary |
|---------|---------|
| v0.1 | Seven-project skeleton, basic models |
| v0.2 | Early identity/package discovery |
| v0.3 | Services and scheduled-task inventory |
| v0.4 | Live multi-collector discovery skeleton |
| v0.5 | PolicyCollector, ManagedObjectCatalog, full categorized report (archived) |
| v0.6 | Binder, ValidateAll, ObservationSummary, concise report + high-risk watch list |
| Step A | ProductDomain on every catalog entry; reports grouped by domain then SubCategory |

## What works today
- Discovers identity, packages, services, tasks, privacy consent settings, and curated GPO/policy registry surfaces
- Explains settings via ManagedObject catalog (name, description, risk tag, rationale, **product domain**)
- Binds live values to the catalog; batch-validates catalog structure
- Prints an observation/risk **summary** and a high-risk **watch list** grouped by domain (not a security score)
- Optional full dump by domain: `--full`
- Strictly read-only, non-interactive (no elevation, no writes, no prompts)

## Product domains
ConsentStore · AppPrivacy · Telemetry · WindowsUpdate · Defender · Search · Edge · ActivityHistory · CloudContent · Advertising · Location · Biometrics · Device · Speech · Firewall (reserved) · Other

## Run
```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
```

| Flag | Behavior |
|------|----------|
| (default) | Summary + high-risk configured items (by domain) |
| `--full` | Complete catalog dump under `## Domain:` headers |
| `--help` | Help |

## Safety (current phase)
No registry/service/task/package/policy changes · no elevation · no remediation · no interactive UI · no product telemetry

## Solution projects
| Project | Role |
|---------|------|
| Models | Data, ManagedObjectCatalog, ProductDomain, ObservationSummary |
| Core | Shared primitives |
| Logging | AuditLogger |
| KnowledgeBase | In-memory store |
| Scanner | Collectors + InventoryStateBinder |
| Validator | SchemaValidator (batch) |
| CLI | Pipeline + domain-grouped report |

Note: files under `bin/` are **build outputs** (compiled DLLs from these projects), not separate hand-written sources.

## Future focus (see `Status/AI_Handoff.md` for full detail)
1. ~~**Product domain taxonomy**~~ — **done (Step A)**  
2. **Effective layer resolution** (GPO vs Settings/ConsentStore vs alternate policy paths; show conflicts) — **next (Step B)**  
3. CapabilityCollector reliability pass  
4. Expand discovery **domain by domain** (Firewall collector next among gaps) with human-readable policy names — not full gpedit import  
5. Optional compare-only baselines and a **transparent** risk-assessment feature  
6. Relationships presentation  
7. Controlled change **design only** until authorised; terminal UI only after report is clear  

**Balance:** broad privacy/security coverage without a self-strangling all-ADMX model.

## Authoritative docs
- `Status/AI_Handoff.md` — continuity + detailed future steps and insertion points  
- `Status/Current_Status.md` — verified state  
- `Status/Prototype_v0.1_Implementation_Map.md` — file/pipeline map  
- `Status/Project_Documentation.md` — technical overview  
