# Security Policy

## Supported versions

The current minor release line receives security fixes. Older minor releases should be upgraded before reporting product behavior because catalog and write-authorization rules evolve together.

## Reporting a vulnerability

Use a private GitHub security advisory for issues that could permit unintended registry, service, scheduled-task, package, optional-feature, process, or firewall modification. Do not disclose an exploitable write-path issue in a public issue before maintainers have had an opportunity to respond.

Include the app version, build identifier, detected Windows edition/build shown on the About page, reproduction steps, and whether the session was in Inspect or Modify mode. Do not attach audit logs until you have reviewed them for device-specific paths or names.

## Security boundaries

- Inspect is the initial and read-only operating mode.
- Modify requires an explicit session decision; privileged changes require Windows elevation.
- Only complete targets from curated source-controlled allowlists are writable.
- Dynamic system inventory and arbitrary firewall rules are never writable.
- BitLocker and User Account Control remain observation-only because a generic mutation surface could cause lockout or broad security regression.
- Registry type, service/task state, package presence, feature state, and firewall-profile values are independently read back before success is reported.
- External tools use fixed executable and argument shapes without a shell or user-supplied commands.
- Audit data stays local and the application has no telemetry or cloud backend.

The detailed threat and change model is documented in [Status/Safety_Model.md](Status/Safety_Model.md).
