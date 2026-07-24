# Windows Privacy Platform

**Current milestone:** Prototype v0.5 — Model + policy discovery + categorized report  
**Previous:** v0.4 (live discovery) → v0.3 (identity) → v0.2 (architecture) → v0.1 (archived)

Local, declarative privacy intelligence platform for Windows.  
Philosophy: **Understand first. Change later.**

## What works today
- Identifies Windows 10 / 11 (version, edition, build)
- Enumerates services, AppX packages, scheduled tasks, privacy consent settings
- Probes high-value GPO/policy/preference registry surfaces (telemetry, Windows Update, Defender, Search, Activity History, Cloud Content, App Privacy, Edge, and more)
- ManagedObject catalog with Name, Description, Category, RiskLevel, and Rationale
- Categorized console report explaining discovered settings
- In-memory Knowledge Base + structural validation
- Thread-safe console audit logging
- Strictly read-only (no elevation, no writes, no remediation)

## v0.4 verified runtime sample (Windows 11 Pro 25H2)
```
Identity : Windows 11 Pro | 25H2 | Build 26200
Capabilities : 0 | Packages : 165 | Services : 303 | Tasks : 247 | Privacy settings : 17
```

## v0.5 additions
- **PolicyCollector** — read-only table-driven probes; missing values → `Not configured`
- **ManagedObjectCatalog** — privacy + policy batches (`All` combined)
- **Categorized report** — groups by SubCategory with description, risk, rationale, current value
- Inventory summary includes policy probe count and configured count

## Repository layout
```
Archive/          # Immutable historical snapshots
KnowledgeBase/    # Intentional mirror of Source KnowledgeBase
Source/           # Active seven-project solution
Status/           # Continuity & architecture documents (authoritative)
```

## Solution projects
| Project        | Role                                      |
|----------------|-------------------------------------------|
| Models         | Pure data structures + ManagedObjectCatalog |
| Core           | OperationResult, PlatformException, paths |
| Logging        | IAuditLogger / AuditLogger (console)      |
| KnowledgeBase  | InMemoryKnowledgeBaseRepository           |
| Scanner        | InventoryScanner + live collectors        |
| Validator      | SchemaValidator + RequiredFieldRule       |
| CLI            | Explicit composition, report, pipeline    |

## Build & run
```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
```
Expected: inventory counts, policy probes, catalog load, categorized report, safety confirmation, 0 errors / 0 warnings.

## Safety model (absolute for current phase)
- No registry writes
- No service / task / package / capability / policy changes
- No elevation or UAC
- No remediation, rollback, or recovery
- No network or telemetry

## Collector status
| Collector                  | Status                                      |
|----------------------------|---------------------------------------------|
| WindowsIdentityCollector   | Live (Win10/11)                             |
| CapabilityCollector        | Live (may return 0 on some builds)          |
| PackageCollector           | Live (AppX, current user)                   |
| ServiceCollector           | Live (ServiceController)                    |
| ScheduledTaskCollector     | Live (schtasks)                             |
| PrivacyCollector           | Live (HKCU ConsentStore + related)          |
| PolicyCollector            | Live (GPO/policy/preference probes, v0.5)   |

## Next focus
1. Hardware-verify v0.5.
2. CapabilityCollector follow-up if still 0.
3. Expand catalog/probes from runtime gaps.
4. Only later: controlled change model with elevation-on-demand.
5. Terminal UI (preferred) or GUI after the model is solid.

See `Status/` for full architecture, handoff, and status documents.
