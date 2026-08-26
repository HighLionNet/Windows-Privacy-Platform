# Finishing Roadmap

The core product, safety model, write backends, catalog validation, Windows applicability, inventory split, release packaging, and public documentation are implemented. Remaining work is polish that can land without weakening or redesigning those contracts.

## Publication finish

- Use a protected code-signing certificate in tagged builds and establish publisher reputation.
- Add signed checksum/provenance attestations and a maintained screenshot set to GitHub Releases.
- Complete accessibility testing with keyboard-only, high-contrast, scaling, and screen-reader scenarios.
- Add localization infrastructure after English catalog prose receives editorial review.

## Product finish

- Optional read-only HTML/JSON export with explicit provenance and unknown states.
- Visual comparison of user-captured snapshots without automatic remediation.
- Additional catalog entries only where authoritative behavior, applicability, narrative, and exclusion/write decisions are verifiable.
- Performance profiling on unusually large enterprise task/package/firewall inventories.

## Permanent constraints

No privacy score, bulk apply, generic registry/service/task/firewall editor, application telemetry, silent persistence, dynamic authorization, BitLocker mutation, or User Account Control master mutation.
