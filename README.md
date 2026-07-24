# Windows Privacy Platform

**Current milestone:** Prototype v0.4 — Live discovery skeleton  
**Previous:** v0.3 (identity) → v0.2 (architecture) → v0.1 (archived)

Local, declarative privacy intelligence platform for Windows.  
Philosophy: **Understand first. Change later.**

## What works today
- Identifies Windows 10 / 11 (version, edition, build)
- Enumerates services, AppX packages, scheduled tasks, privacy consent settings
- Attempts Windows capabilities via DISM (may return 0 on some builds)
- In-memory Knowledge Base + structural validation
- Thread-safe console audit logging
- Strictly read-only (no elevation, no writes, no remediation)

## Verified runtime sample (Windows 11 Pro 25H2)
```
Identity : Windows 11 Pro | 25H2 | Build 26200
Capabilities : 0 | Packages : 165 | Services : 303 | Tasks : 247 | Privacy settings : 17
```

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
| Models         | Pure data structures                      |
| Core           | OperationResult, PlatformException, paths |
| Logging        | IAuditLogger / AuditLogger (console)      |
| KnowledgeBase  | InMemoryKnowledgeBaseRepository           |
| Scanner        | InventoryScanner + live collectors        |
| Validator      | SchemaValidator + RequiredFieldRule       |
| CLI            | Explicit composition & pipeline entry     |

## Build & run
```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
```
Expected: real inventory counts, safety confirmation, 0 errors / 0 warnings.

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
| CapabilityCollector        | Live (DISM query; 0 results on some builds) |
| PackageCollector           | Live (AppX, current user)                   |
| ServiceCollector           | Live (ServiceController)                    |
| ScheduledTaskCollector     | Live (schtasks)                             |
| PrivacyCollector           | Live (HKCU ConsentStore, limited set)       |

## Next focus
1. Improve discovery quality (especially Capabilities).
2. Begin mapping discovered items into explained ManagedObjects.
3. Reporting / categorization layer.
4. Only later: controlled change model with elevation-on-demand.
5. Terminal UI (preferred) or GUI after the model is solid.

See `Status/` for full architecture, handoff, and status documents.
