# Architecture — Windows Privacy Platform v2.1

**Applies to:** Version 2.1

## Product shape

- Primary product: **WPF GUI** (`WindowsPrivacyPlatform.App`)
- Libraries: Models, Core, Logging, KnowledgeBase, Scanner, Validator
- CLI project folder may remain on disk for history but is **not** in the solution and is not a product surface.

## Layers

1. **Models** — ManagedObject catalog, WritableTarget, ValueSemantics, ScanResult diagnostics, inventory snapshot shapes. No OS I/O.
2. **Scanner** — Collectors + binders + PolicyPrecedenceResolver. Fail-soft collectors with structured diagnostics.
3. **Validator** — Schema / catalog integrity.
4. **App** — WPF shell, ScanService, ElevationService, PolicyChangeService.

## Write safety contract

- Default: read-only Inspect mode.
- Modify requires explicit session authorization **and** (when Required) elevation.
- Only catalog entries with an explicit complete `WritableTarget` are writable.
- `DiscoveryMethod` never authorizes a write.
- Write path: pre-read → confirm → write with catalog kind → independent read-back verifying value **and** kind.
- Firewall domain is observation-only.

See `Status/Safety_Model.md`.

## Scan contract

- Collectors report through `ScanResult` / `CollectorDiagnostic`.
- Cancellation is supported; canceled or failed scans do not replace the last successful UI scan state.
- Process-backed collectors use `SafeProcessRunner`.

## Version source

Single authoritative version: `Directory.Build.props` (currently 2.1.0).
