# Security Policy

## Supported versions

The current minor release line receives security fixes. Older minor releases should be upgraded before reporting product behavior because catalog and write-authorization rules evolve together.

## Reporting a vulnerability

Use a private GitHub security advisory for issues that could permit unintended registry or firewall-profile modification, or any service, task, package, feature, process, or firewall-rule modification. Do not disclose an exploitable write-path issue in a public issue before maintainers have had an opportunity to respond.

Include the app version, build identifier, detected Windows edition/build shown on the About page, reproduction steps, and whether the session was View-only or Admin. Do not attach audit logs until you have reviewed them for device-specific paths or names.

## Security boundaries

- View-only is the initial and read-only operating mode.
- Admin requires explicit session authorization; privileged changes require Windows elevation. Elevation does not add catalog permissions.
- Only complete targets from curated source-controlled allowlists are writable, and the runtime target must still match the compiled catalog.
- System Explorer and arbitrary firewall rules are never writable.
- Service mutation is start/stop/restart only for verified non-Microsoft optional services after confirmation. Startup configuration, critical/Microsoft/unknown/shared-host/boot/system services, and incomplete evidence stay diagnosis-only.
- BitLocker and User Account Control are typed high-impact registry Settings and require a fresh credential step-up before the verified-change contract.
- Registry type and firewall-profile values are independently read back before success is reported.
- Over-the-shoulder elevation cannot redirect an HKCU request into the administrator account; that per-user operation is refused.
- External tools use fixed executable and argument shapes without a shell or user-supplied commands.
- Audit data stays local and the application has no telemetry or cloud backend.

The detailed threat and change model is documented in [Status/Safety_Model.md](Status/Safety_Model.md).
