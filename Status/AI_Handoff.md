# Engineering Handoff

## Current contracts

- The WPF application is the sole product; do not restore the removed prototype CLI or duplicate source tree.
- `Directory.Build.props` is the only literal app-version source. UI identity must continue to use `ProductInfoReader`.
- Settings and System Explorer are separate data flows. Dynamic inventory must never gain a `WritableTarget`.
- Every static catalog entry requires a complete plain-language narrative and either a complete target or non-default `ExclusionReason`.
- Registry and native targets are deny-by-default. Keep one authorization/round-trip theory row per curated allowlist entry.
- BitLocker, User Account Control, and arbitrary firewall-rule writes are permanent exclusions with native-tool handoff.
- Preserve unknown/error/access-denied semantics and last-good scan state.
- Keep the change contract: pre-read, confirm, typed operation, independent read-back, local audit.
- Preserve `SettingsListTarget`: Overview/search must land in a category list and never auto-open a Settings detail route.
- Preserve `ManagedObjectCatalog.IsAuthorizedWriteTarget` revalidation and `ElevationService.CanModifyHive`; a complete runtime object alone is never authority.
- Keep Services read-only and use neutral evidence terminology rather than malware verdicts.

## Release verification

Run restore, Release build, tests without rebuilding, release packaging, `scripts/verify-release.ps1`, repository hygiene scans, and a later extracted-archive launch smoke test. The source-only v2.4.0 implementation pass compiled the solution but deliberately did not execute tests or launch the app under its task constraint.

## Safe future finishing work

Publisher signing/reputation, accessibility audit, localization, documentation visuals, optional export, and additional verified catalog coverage can be added without changing the authority model. New write surfaces require the full authorization, recovery, and per-target verification standard—not a generic adapter.
