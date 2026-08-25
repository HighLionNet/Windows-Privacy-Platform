# AI Handoff — v2.1 (2026-08-25)

## Source of truth

- Version: `2.1.0` in `Directory.Build.props`
- Product: .NET 8 WPF application; no supported CLI
- Release branch: `release/v2.1`
- Catalog: 257 unique entries, schema `2.1`, across 26 represented `ProductDomain` values

## What changed

### Safety, startup, and stability

- `StartupModeDialog` is shown at ordinary launch. Inspect proceeds unelevated; Modify relaunches with `--resume-modify`, then presents the existing session authorization once.
- `App` logs dispatcher and AppDomain unhandled exceptions and presents a graceful error dialog.
- `SmoothScrollBehavior` normalizes high-frequency wheel/touchpad input.
- `PackageCollector` now uses `SafeProcessRunner`; all process-backed collection stays timeout-bound and shell-free.
- Writable authorization remains solely `WritableTarget` + explicit ObjectId whitelist. No v2.1 expansion adds write permission to sensitive or inventory surfaces.

### Evidence correctness

- `PolicyPrecedenceResolver` compares canonical meanings before raw encodings in both alternate-store and layer-rank resolution.
- UAC and BitLocker policy probes now bind to their catalog entries.
- `WindowsIdentityCollector` queries `Win32_EncryptableVolume` only when elevated and otherwise emits the exact deliberate state `Requires Modify mode to observe`.
- Probe IDs were reconciled with the values actually read; inaccurate duplicate SmartScreen representation was removed.

### Catalog and explanations

- `CatalogV21Expansion.cs` adds source-referenced Recall, Copilot, ASR, Windows Hello, Storage Sense, Widgets, Search, accessibility-sync, diagnostic-data, Edge, network, local-security, service, task, package, and capability entries.
- `LocalSecurityPolicyCollector` uses read-only `secedit /export` and parses only a fixed field whitelist.
- `InventoryAnchorBinder` maps curated service/task/package/capability definitions onto collected inventories without changing Windows state.
- `SettingNarrative` is required and projected directly by `SettingExplanationFactory`. Schema/tests reject incomplete or emergency-fallback narratives and reused decision-support paragraphs.

### Repository

- Root `KnowledgeBase/` duplicates were removed; the solution-owned project remains `Source/WindowsPrivacyPlatform.KnowledgeBase`.
- Dynamic sidebar navigation is derived from catalog domains, so new domains no longer require XAML buttons.

## Important files

- `Source/WindowsPrivacyPlatform.Models/CatalogV21Expansion.cs`
- `Source/WindowsPrivacyPlatform.Models/SettingNarrative.cs`
- `Source/WindowsPrivacyPlatform.Scanner/PolicyCollector.cs`
- `Source/WindowsPrivacyPlatform.Scanner/LocalSecurityPolicyCollector.cs`
- `Source/WindowsPrivacyPlatform.Scanner/Binding/InventoryAnchorBinder.cs`
- `Source/WindowsPrivacyPlatform.Scanner/Binding/PolicyPrecedenceResolver.cs`
- `Source/WindowsPrivacyPlatform.App/App.xaml.cs`
- `Source/WindowsPrivacyPlatform.App/StartupModeDialog.xaml`
- `Source/WindowsPrivacyPlatform.App/Behaviors/SmoothScrollBehavior.cs`

## Tests and release checks

The v2.1 suite covers semantic precedence, write boundaries, unique catalog IDs, schema version, narrative completeness/uniqueness, collector-to-catalog alignment, local-security whitelisting, and non-mutating inventory binding.

Final release command results are recorded here after validation:

- Release restore: passed (`--locked-mode`)
- Release build: passed, 0 warnings / 0 errors
- Tests: passed, 19 / 19
- win-x64 framework-dependent publish: passed
- WPF startup smoke: passed; process remained responsive and was stopped after the check

## Next work

Follow `Status/Roadmap.md`. Do not reintroduce raw-value conflict comparisons, explanation-factory category prose, inferred write targets, a second post-UAC Modify selection, or conversion of failed reads into `Not configured`.
