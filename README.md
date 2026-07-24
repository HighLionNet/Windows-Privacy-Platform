# Windows Privacy Platform

**Current milestone:** Prototype v0.6 — Bind + validate + risk summary (**runtime verified** on Windows 11 Pro 25H2)  
**Previous:** v0.5 (archived) → v0.4 → v0.3 → v0.2 → v0.1

Local, declarative privacy intelligence platform for Windows.  
Philosophy: **Understand first. Change later.**

## What works today
- Discovers identity, packages, services, tasks, privacy consent settings, and curated GPO/policy registry surfaces
- Explains settings via ManagedObject catalog (name, description, risk tag, rationale)
- Binds live values to the catalog; batch-validates catalog structure
- Prints an observation/risk **summary** and a high-risk **watch list** (not a security score)
- Optional full dump: `--full`
- Strictly read-only, non-interactive (no elevation, no writes, no prompts)

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
| (default) | Summary + high-risk configured items |
| `--full` | Complete categorized dump |
| `--help` | Help |

## Safety (current phase)
No registry/service/task/package/policy changes · no elevation · no remediation · no interactive UI · no product telemetry

## Solution projects
| Project | Role |
|---------|------|
| Models | Data, ManagedObjectCatalog, ObservationSummary |
| Core | Shared primitives |
| Logging | AuditLogger |
| KnowledgeBase | In-memory store |
| Scanner | Collectors + InventoryStateBinder |
| Validator | SchemaValidator (batch) |
| CLI | Pipeline + report |

Note: files under `bin/` are **build outputs** (compiled DLLs from these projects), not separate hand-written sources.

## Future focus (see `Status/AI_Handoff.md` for full detail)
1. **Product domain taxonomy** (Firewall, Defender, Update, App privacy, Telemetry, …) for navigation  
2. **Effective layer resolution** (GPO vs Settings/ConsentStore vs alternate policy paths; show conflicts)  
3. Expand discovery **domain by domain** (Firewall collector next among gaps) with human-readable policy names — not full gpedit import  
4. Optional compare-only baselines and a **transparent** risk-assessment feature  
5. Relationships presentation  
6. Controlled change **design only** until authorised; terminal UI only after report is clear  

**Balance:** broad privacy/security coverage without a self-strangling all-ADMX model.

## Authoritative docs
- `Status/AI_Handoff.md` — continuity + detailed future steps and insertion points  
- `Status/Current_Status.md` — verified state  
- `Status/Prototype_v0.1_Implementation_Map.md` — file/pipeline map  
