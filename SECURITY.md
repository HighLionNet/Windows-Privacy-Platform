# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| 2.1.x   | Yes       |
| 2.0.x   | Limited   |
| < 2.0   | No        |

## Reporting a vulnerability

Please open a private security advisory on GitHub or contact the maintainers via the repository owner profile.

Do **not** open a public issue for vulnerabilities that could enable unintended registry/process modification.

## Security model (summary)

- Default mode is read-only inspection.
- Modify mode requires explicit session authorization and elevation when required.
- Writes are limited to catalog entries with an explicit complete `WritableTarget`.
- `DiscoveryMethod` never authorizes writes.
- Registry type and value are verified on read-back.
- Firewall rules, services, and scheduled tasks are not generically editable.
- No telemetry and no cloud backend.

See `Status/Safety_Model.md` for details.
