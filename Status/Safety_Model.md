# Safety Model

## Authority boundaries

Inspect mode has no mutation authority. Modify mode is an explicit session choice; elevation satisfies an operating-system privilege requirement but does not create new catalog permissions.

A target is writable only when all of the following are true:

1. It is a curated Settings entry, not live System Inventory.
2. Its current Windows edition/build and selected value are applicable.
3. Its exact object ID appears in the appropriate source-controlled authorization table.
4. Its typed target is complete and the requested value is supported.
5. The user confirms the one requested operation.

Discovery paths, observed source paths, search results, external process output, and UI text never authorize a write.

## Verified change contract

Each backend implements the same sequence:

1. Pre-read the exact target and abort on an ambiguous or failed read.
2. Display current state, intended state, side effects, and recovery information.
3. Execute one typed operation through a fixed API/tool shape.
4. Read the target again through an independent path.
5. Report success only when the typed state matches the request.
6. Write a local audit record that does not leave the device.

Registry verification includes value kind. Service verification includes startup type and service-controller state. Task verification parses the registered task state. Package removal verifies per-user absence. Optional-feature verification checks the DISM state. Firewall-profile verification checks the exact profile values.

## Permanent exclusions

- **BitLocker and Device Encryption:** observation-only because a generic state change can be lengthy and can lock a user out without a verified recovery key. The detail page opens Windows' native encryption management.
- **User Account Control:** observation-only because master-level changes alter a broad security boundary and may require coordinated values/restart behavior. The detail page opens the native management surface.
- **Arbitrary firewall rules:** inventory-only because broad rule mutation is unsafe and semantically complex. The app changes only the twelve fixed enabled/inbound/outbound/notification profile properties and can open Windows Defender Firewall with Advanced Security.
- **Non-curated services, tasks, packages, features, and capabilities:** inventory-only. Enumeration can never expand authority.

## Evidence integrity

Unknown, not observed, not configured, unsupported, error, and access denied are distinct. Failed or canceled scans do not replace the last good view. Edition/build limitations are shown rather than silently filtered or presented as disabled configuration.

## Process and data handling

External collection and operation commands use fixed executables and fixed arguments without a shell or generic runner exposed to the UI. Discovered text is treated as display-only input. Local preferences and audit logs stay beneath the user's LocalAppData directory. The application has no telemetry or cloud backend.
