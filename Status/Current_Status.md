# Current Status

**Release state:** v2.3.5 desktop release
**Updated:** 2026-08-26

## Product surface

The supported product is a .NET 8 WPF application for Windows 10 and Windows 11. The obsolete console prototype and duplicate top-level source copies have been removed.

The application separates two kinds of data:

- **Settings:** relevant privacy and security policies with an approved registry write contract. Non-editable definitions never appear here.
- **System Explorer:** grouped live services, scheduled tasks, packages, optional features, capabilities, and firewall rules. It is always read-only.

## Coverage delivered

- Privacy permissions and the full AppPrivacy policy families.
- Defender, SmartScreen, controlled-folder access, and network-protection policy.
- Search, cloud content, activity, advertising, speech, location, biometrics, device access, Edge, Copilot, Recall, Widgets, OneDrive, networking, remote access, and local-security policy.
- Windows Update, Storage Sense, BitLocker, UAC, exploit-protection configuration, and other unapproved definitions remain internal references rather than UI clutter.
- Firewall-profile observation and bounded profile writes; arbitrary firewall rules remain inventory-only.
- Services, tasks, packages, and Windows features are never writable in this application.

## Write safety delivered

- Deny-by-default typed targets with explicit exclusion reasons for every other setting.
- Per-target pre-read, confirmation, operation, independent verification, and local audit.
- Value- and edition-aware UI state; inapplicable controls cannot be changed.
- Registry-only backend enforcement plus automated authorization/round-trip coverage.
- Automated prose validation prevents technical identifiers from leaking back into user-facing explanation fields.

## Release engineering delivered

- Assembly-derived product/version/build/company/repository identity.
- One-time Desktop and Start Menu shortcut offer.
- Reproducible win-x64 release archive, optional Authenticode hook, CI artifact, and tag-based GitHub Release publishing.
- Repository-relative, fast-forward-only sync/build helper that never discards local changes.

The remaining backlog is limited to finishing work such as signed publisher reputation, localization, accessibility certification, maintained release screenshots, and optional report export.
