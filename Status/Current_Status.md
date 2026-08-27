# Current Status

**Release state:** v2.5.0 Release-build candidate
**Updated:** 2026-08-27

## Product surface

The supported product is a .NET 8 WPF application for Windows 10 and Windows 11. The obsolete console prototype and duplicate top-level source copies have been removed.

The application separates two kinds of data:

- **Settings:** relevant privacy and security policies with an approved registry write contract. Non-editable definitions never appear here.
- **System Explorer:** grouped live scheduled tasks, packages, optional features, capabilities, and firewall rules. It is always read-only.
- **Services:** compact, top-level read-only service evidence with state, startup, account, path, publisher/signature evidence, dependency, and issue filters.

## Coverage delivered

- Privacy permissions and the full AppPrivacy policy families.
- Defender, SmartScreen, controlled-folder access, and network-protection policy.
- Search, cloud content, activity, advertising, speech, location, biometrics, device access, Edge, Copilot, Recall, Widgets, OneDrive, networking, remote access, and local-security policy.
- Windows Update, Storage Sense, BitLocker, UAC, exploit-protection configuration, and other unapproved definitions remain internal references rather than UI clutter.
- Firewall-profile observation and bounded profile writes; arbitrary firewall rules remain inventory-only.
- Services, tasks, packages, and Windows features are never writable in this application.

## Write safety delivered

- Deny-by-default typed targets with explicit exclusion reasons for every other setting.
- CredUI password authorization for Admin, UAC elevation when required, category-level pending batches, pre-read, one themed confirmation, typed operation, independent verification, local audit, and stay-open Apply behavior.
- Exact runtime-target comparison against the compiled registry allowlist and cross-account HKCU refusal.
- Startup authorization-table hashing, bounded command-line markers, a single-instance boundary, LocalAppData ACL hardening, previous-hash audit chaining, and best-effort process mitigations.
- Value- and edition-aware UI state; inapplicable controls cannot be changed.
- Registry-only backend enforcement plus automated authorization/round-trip coverage.
- Automated prose validation prevents technical identifiers from leaking back into user-facing explanation fields.

## Release engineering delivered

- Assembly-derived product/version/build/company/repository identity.
- One-time Desktop and Start Menu shortcut offer.
- Reproducible win-x64 release archive, PDB/source/secret-file rejection, content-hash manifest, static ZIP audit, optional Authenticode hook, CI artifact, and tag-based GitHub Release publishing.
- Repository-relative, fast-forward-only sync/build helper that never discards local changes.

The remaining backlog includes an isolated authenticated elevation broker, signed publisher reputation, localization, Windows accessibility certification, maintained release screenshots, and optional report export. See `Review-v2.5.0.md` for the current limitations.
