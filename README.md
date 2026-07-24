# Windows Privacy Platform

**Current milestone:** Prototype v0.3 — Functional identity skeleton  
**Previous:** Prototype v0.2 (architecture complete) → v0.1 (archived)

Local, declarative privacy intelligence platform for Windows.  
Philosophy: **Understand first. Change later.**

## What works today
- Correctly identifies Windows 10 and Windows 11 (including 22H2 / 23H2 / 24H2 / 25H2)
- Reports product name, marketing edition, and build number
- Full collector-based Scanner pipeline
- In-memory Knowledge Base + structural validation
- Thread-safe console audit logging
- Strictly read-only (no elevation, no writes, no remediation)

## Repository layout
```
Archive/          # Immutable historical snapshots (v0.1)
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
| Scanner        | InventoryScanner + collectors             |
| Validator      | SchemaValidator + RequiredFieldRule       |
| CLI            | Explicit composition & pipeline entry     |

## Build & run
```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
```
Expected: real Windows version/edition/build, safety confirmation, 0 errors / 0 warnings.

## Safety model (absolute)
- No registry writes
- No service / task / package / capability / policy changes
- No elevation or UAC
- No remediation, rollback, or recovery
- No network or telemetry

## Current collector status
| Collector                  | Status                          |
|----------------------------|---------------------------------|
| WindowsIdentityCollector   | **Real** (read-only, Win10/11)  |
| CapabilityCollector        | Placeholder                     |
| PackageCollector           | Placeholder                     |
| ServiceCollector           | Placeholder                     |
| ScheduledTaskCollector     | Placeholder                     |
| PrivacyCollector           | Placeholder                     |

## Next steps
1. Implement remaining real collectors one at a time (still read-only).
2. Lightweight CLI argument parsing.
3. Persistent KnowledgeBase (preserve existing interfaces).
4. Reporting / relationship modelling.

Only after the discovery layer is mature should any remediation architecture be considered under a separately authorised phase.

See `Status/` for full architecture, handoff, and status documents.
