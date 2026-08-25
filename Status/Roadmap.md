# Windows Privacy Platform Roadmap

**Last updated:** 2026-08-25
**Current completed milestone:** **Version 2.1.0**

## Permanent constraints

- Inspect is the default and is fully read-only.
- Modify is explicit, session-authorized, one setting at a time, and limited to catalog-whitelisted `WritableTarget` entries.
- No bulk modify, profiles, rollback/snapshot system, privacy score, supported CLI, generic registry editor, firewall-rule writing, or service/task control.
- Catalog semantics and authored explanations are model data; presentation code contains no Windows policy logic.
- Unknown, not configured, not observed, unsupported, and collection failure remain distinct evidence states.

## Completed milestones

- v1.4 — Domain → category → setting information architecture
- v2.0 — Deny-by-default controlled modification, evidence hardening, and WPF-only product boundary
- **v2.1 — startup/elevation UX, stability hardening, canonical conflict resolution, mandatory setting narratives, dynamic navigation, and 257-entry Windows coverage**

## Next milestone: v2.2

1. Accessibility review: keyboard-only navigation, screen-reader labels, high-contrast behavior, and scalable typography.
2. Read-only export of scan evidence and collector diagnostics in documented JSON and HTML formats.
3. Richer relationship and precedence visualization without introducing scores or compliance verdicts.
4. Continue source-verified catalog depth for Defender platform state, Windows servicing, identity protection, and firewall observation.
5. Add versioned catalog-change notes, data migration checks, and reproducible release packaging.
6. Expand runtime tests around elevation argument handling, scan cancellation, exception presentation, and live collector degradation.

## Future work remains bounded

Any future write expansion requires an exact documented target, supported values, pre-read, confirmation, independent verification, and an explicit whitelist entry. New domains remain observation-only until that complete safety case exists.
