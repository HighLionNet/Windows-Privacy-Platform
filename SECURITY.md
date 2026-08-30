# Security Policy

## Supported versions

The current product on `main` (today 2.6.1) receives security fixes. Older `vX.Y.Z` branches are snapshots. Upgrade before reporting product behavior, because catalog and write-authorization rules evolve together.

## Reporting a vulnerability

Use a private GitHub security advisory for issues that could permit unintended registry or firewall-profile modification, or any service, task, package, feature, process, or firewall-rule modification. Do not disclose an exploitable write-path issue in a public issue before maintainers have had an opportunity to respond.

Include the app version, build identifier, detected Windows edition/build shown on the About page, reproduction steps, and whether the session was View-only or Administrator. Do not attach audit logs until you have reviewed them for device-specific paths or names.

## Security boundaries

- View-only is the initial and read-only operating mode.
- Administrator mode is an explicit session choice. Privileged changes require Windows elevation.
- Only complete targets from curated source-controlled allowlists are writable.
- System Explorer, live inventory, and arbitrary firewall rules are never writable.
- BitLocker *lifecycle* (encrypt, decrypt, protectors) and User Account Control *master* mutation are not implemented. Existing BitLocker and UAC *registry policy* Settings are high-impact: warning plus a fresh Windows credential, then the normal write contract.
- The modification engine accepts verified registry targets only. Native-component write backends from the retired 2.6.0 experiment are not shipped.
- Every runtime target is compared field-for-field to the compiled catalog. Registry type and firewall-profile values are independently read back before success is reported.
- Over-the-shoulder elevation cannot redirect an HKCU request into the administrator account; that per-user operation is refused.
- External tools use fixed executable and argument shapes without a shell or user-supplied commands.
- Audit data stays local. The application has no telemetry or cloud backend.

The detailed threat and change model is documented in [Status/Safety_Model.md](Status/Safety_Model.md).
