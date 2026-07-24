# Windows Privacy Platform

**Current milestone:** Prototype v0.6 — Bind + validate + risk summary  
**Previous:** v0.5 (model + policy report, archived) → v0.4 → v0.3 → v0.2 → v0.1

Local, declarative privacy intelligence platform for Windows.  
Philosophy: **Understand first. Change later.**

## What works today
- Discovers identity, packages, services, tasks, privacy consent, and high-value GPO/policy surfaces
- ManagedObject catalog with Name, Description, Category, RiskLevel, Rationale
- Binds live inventory onto catalog `CurrentState`
- Batch structural validation of the catalog
- Observation & risk summary (default concise output)
- Optional full categorized report via `--full`
- Strictly read-only (no elevation, no writes, no interactive prompts)

## Run
```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
```

## CLI flags (non-interactive)
| Flag | Behavior |
|------|----------|
| (default) | Risk summary + high-risk configured items |
| `--full` | Complete categorized catalog dump |
| `--help` | Help text |

## Safety model (absolute for current phase)
- No registry writes
- No service / task / package / capability / policy changes
- No elevation or UAC
- No remediation, rollback, or recovery
- No network or telemetry
- No interactive prompts

## Solution projects
| Project | Role |
|---------|------|
| Models | Data + ManagedObjectCatalog + ObservationSummary |
| Core | OperationResult, paths |
| Logging | AuditLogger |
| KnowledgeBase | In-memory repository |
| Scanner | Collectors + InventoryStateBinder |
| Validator | SchemaValidator (batch) |
| CLI | Pipeline entry + report |

## Next focus
1. Verify v0.6 on hardware.
2. Optional relationship metadata.
3. CapabilityCollector follow-up if still 0.
4. Only later: controlled change design, then terminal UI.

See `Status/` for handoff and architecture documents.
