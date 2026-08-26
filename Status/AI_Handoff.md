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

## Release verification

Run restore, Release build, tests without rebuilding, release packaging, repository hygiene scans, and an extracted-archive launch smoke test. The GitHub tag workflow produces the same stable archive name used by the documented download commands.

## Safe future finishing work

Publisher signing/reputation, accessibility audit, localization, documentation visuals, optional export, and additional verified catalog coverage can be added without changing the authority model. New write surfaces require the full authorization, recovery, and per-target verification standard—not a generic adapter.
