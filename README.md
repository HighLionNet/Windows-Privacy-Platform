# Windows Privacy Platform

**Prototype v0.2** — Local, declarative privacy intelligence platform for Windows.

## Core Principles
- **Understand first. Change later.**
- Strictly read-only. No remediation, no elevation, no Windows writes.
- Architecture-first: seven-project layered solution under `Source/`.

## Repository Layout
```
Archive/          # Immutable historical snapshots (v0.1)
KnowledgeBase/    # Mirror of KnowledgeBase source (intentional)
Source/           # Active solution (7 projects + .sln)
Status/           # Continuity & architecture docs (authoritative)
```

## Solution Projects
| Project | Role |
|---------|------|
| Models | Pure data structures (ManagedObject, Snapshot, Results) |
| Core | OperationResult, PlatformException, PathConstants |
| Logging | IAuditLogger / AuditLogger (console, thread-safe) |
| KnowledgeBase | InMemoryKnowledgeBaseRepository |
| Scanner | InventoryScanner + IInventoryCollector framework (placeholders) |
| Validator | SchemaValidator + RequiredFieldRule (ObjectId/ObjectName) |
| CLI | Explicit composition & pipeline entry point |

## Build
```bash
cd Source
dotnet build -c Release
```

## Runtime
```bash
cd Source/WindowsPrivacyPlatform.CLI
dotnet run -c Release
```
Expected: scan → store test ManagedObject → validate → safety confirmation. No Windows changes.

## Key Constraints
- No circular dependencies. Explicit composition only (no DI container).
- Models contain zero business logic.
- Collectors remain placeholders until architecture is verified.
- Top-level `KnowledgeBase/` mirrors Source implementation — do not create further duplicates.
- Required fields validated: `ObjectId`, `ObjectName`.

## Next Steps
1. Local Release build + CLI runtime verification.
2. First real read-only collector: `WindowsIdentityCollector`.
3. Expand validation / persistence only after verification.

See `Status/` for full architecture, handoff, and status documents.
