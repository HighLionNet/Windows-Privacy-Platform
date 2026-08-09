# Contributing

## Principles

- Prefer accuracy over coverage.
- Prefer structural safety over documentation warnings.
- Do not invent certainty about Windows behavior.
- Do not turn discovery paths into write authorization.

## Adding a catalog setting

1. Give it a stable `ObjectId`, name, domain, category, and description.
2. Provide an exact discovery path when registry-backed.
3. Add `ValueSemantics` when values are known.
4. Mark applicability (Windows versions/editions) when known.
5. **Writable only if intentional:** add the ObjectId to the explicit write whitelist in `ManagedObjectCatalog` and ensure `WritableTarget` fields are complete.
6. Never assume a discovery probe is writable.

## Building and testing

```powershell
cd Source
dotnet restore WindowsPrivacyPlatform.sln
dotnet build WindowsPrivacyPlatform.sln -c Release
dotnet test WindowsPrivacyPlatform.sln -c Release
```

Primary target: Windows 11. Windows 10 where the same implementation is naturally correct.

## Pull requests

- Keep changes focused.
- Include regression tests for safety-sensitive logic when practical.
- Do not add bulk modify, profiles, privacy scores, telemetry, or generic editors.
