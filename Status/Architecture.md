# Architecture — Windows Privacy Platform v2.1

**Applies to:** Version 2.1

## Product shape

- Primary product: **WPF GUI** (`WindowsPrivacyPlatform.App`)
- Libraries: Models, Core, Logging, KnowledgeBase, Scanner, Validator
- CLI project folder may remain on disk for history but is **not** in the solution and is not a product surface.

## Layers

1. **Models** — 257-entry `ManagedObject` catalog, required `SettingNarrative`, `WritableTarget`, `ValueSemantics`, scan diagnostics, and inventory shapes. No OS I/O.
2. **Scanner** — registry/WMI/process collectors, domain binders, `InventoryAnchorBinder`, and `PolicyPrecedenceResolver`. Collectors are read-only and fail soft with structured diagnostics.
3. **Validator** — schema, required narrative, fallback rejection, and catalog integrity.
4. **App** — WPF shell, startup mode chooser, `ScanService`, `ElevationService`, and `PolicyChangeService`.

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
- Local security policy is exported read-only with fixed `secedit` arguments and parsed through a field whitelist.
- Curated services, tasks, packages, and capabilities are bound as inventory evidence only.
- Live BitLocker protection status is queried only when elevated; an ordinary scan preserves the explicit insufficient-access state.

## Explanation contract

- Catalog finalization requires a complete, setting-specific `SettingNarrative`.
- The presentation factory projects catalog narrative fields; it does not generate normal explanations from risk/domain switches.
- Emergency fallback text is visibly flagged and rejected by catalog validation/tests.

## Version source

Single authoritative version: `Directory.Build.props` (currently 2.1.0).
