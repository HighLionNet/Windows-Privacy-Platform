# Current Status

**Release state:** v2.2.0 desktop release
**Updated:** 2026-08-26

## Product surface

The supported product is a .NET 8 WPF application for Windows 10 and Windows 11. The obsolete console prototype and duplicate top-level source copies have been removed.

The application separates two kinds of data:

- **Settings:** curated concepts with authored narratives, applicability, semantics, and an explicit writable or excluded decision.
- **System Inventory:** live bulk services, scheduled tasks, packages, optional features, capabilities, and firewall rules. This workspace is diagnostic and always read-only.

## Coverage delivered

- Privacy permissions and the full AppPrivacy policy families.
- Defender policy including attack-surface reduction, exploit protection, controlled-folder access, and network protection.
- Windows Update, Delivery Optimization, Search, cloud content, activity, advertising, speech, location, biometrics, device access, Edge, Copilot, Recall, Widgets, OneDrive, Storage Sense, networking, remote access, and local-security policy.
- BitLocker/Device Encryption and User Account Control observation with native-tool handoff.
- Firewall-profile observation and bounded profile writes; arbitrary firewall rules remain inventory-only.
- Curated reversible controls for selected diagnostic services/tasks, reinstallable inbox apps, and optional Windows features.

## Write safety delivered

- Deny-by-default typed targets with explicit exclusion reasons for every other setting.
- Per-target pre-read, confirmation, operation, independent verification, and local audit.
- Value- and edition-aware UI state; inapplicable controls cannot be changed.
- One automated authorization/round-trip contract case per native allowlist entry.
- Automated prose validation prevents technical identifiers from leaking back into user-facing explanation fields.

## Release engineering delivered

- Assembly-derived product/version/build/company/repository identity.
- One-time Desktop and Start Menu shortcut offer.
- Reproducible win-x64 release archive, optional Authenticode hook, CI artifact, and tag-based GitHub Release publishing.
- Repository-relative, fast-forward-only sync/build helper that never discards local changes.

The remaining backlog is limited to finishing work such as signed publisher reputation, localization, accessibility certification, maintained release screenshots, and optional report export. It does not include unfinished safety boundaries or partially implemented write paths.
