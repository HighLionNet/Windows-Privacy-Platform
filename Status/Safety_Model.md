# Safety Model

## Authority boundaries

View-only mode has no mutation authority. Admin mode is an explicit, password-authorized session choice; elevation satisfies an operating-system privilege requirement but does not create new catalog permissions.

A target is writable only when all of the following are true:

1. It is a curated Settings entry, not System Explorer or an internal reference definition.
2. Its current Windows edition/build and selected value are applicable.
3. Its exact object ID appears in the appropriate source-controlled authorization table.
4. Its typed target is complete and the requested value is supported.
5. The runtime target still exactly matches the compiled catalog target.
6. The user confirms the bounded pending batch once.

Discovery paths, observed source paths, search results, external process output, and UI text never authorize a write.

## Verified change contract

The registry-policy backend implements this sequence:

1. Validate up to 32 unique requests and compare every exact target/value to the compiled allowlist.
2. Pre-read every exact target and abort the batch before confirmation on an ambiguous or failed read.
3. Display each current and intended state, then ask for one batch confirmation.
4. Revalidate and execute each typed registry operation.
5. Read each target again through an independent path.
6. Report each success only when the typed state matches the request; retain structured partial-failure results.
7. Write bounded local audit records that do not leave the device, then rescan while the authorized Admin session stays open.

Registry verification includes value kind. Firewall-profile verification checks the exact profile values. Non-registry targets are rejected before any operation is attempted.

## Permanent exclusions

- **Destructive storage and identity operations:** disk wipe/format, recovery-key deletion, recursive profile deletion, disabling the signed-in user's only administrator account, and generic command execution are not product controls.
- **Arbitrary firewall rules:** inventory-only because broad rule mutation is unsafe and semantically complex. The app changes only the twelve fixed enabled/inbound/outbound/notification profile properties and can open Windows Defender Firewall with Advanced Security.
- **Critical and uncertain services:** permanent critical names, Boot/System starts, shared svchost groups, Microsoft or unknown publishers, and incomplete evidence are diagnosis-only. Only verified non-Microsoft optional services can receive a confirmed runtime start/stop/restart action; startup configuration is never changed.
- **Tasks, packages, features, and capabilities:** System Explorer only. Enumeration can never expand authority.

BitLocker, Device Encryption, and User Account Control are exact typed registry controls. They are catalog-tagged high-impact and require a themed warning plus a fresh Windows credential step-up before the normal verified-change contract.

## Evidence integrity

Unknown, not observed, not configured, unsupported, error, and access denied are distinct. Failed or canceled scans do not replace the last good view. Edition/build limitations are shown rather than silently filtered or presented as disabled configuration.

## Process and data handling

External collection commands use fixed absolute executables, structured fixed arguments, bounded output, timeouts, and cancellation without a generic runner exposed to the UI. Discovered text is display-only. Local preferences and shortcut state are atomically replaced beneath LocalAppData; logs are sanitized, rotated, and previous-hash chained there. The local-data ACL is limited to the current user, SYSTEM, and Administrators. The application has no telemetry or cloud backend. Local audit records are not claimed to be immutable or cryptographically tamper-proof.

The v2.5.1 elevation boundary is not a separate authenticated IPC broker. CredUI verifies an administrator password before Admin mode; UAC supplies an elevated token when required. High-impact changes require a fresh Windows credential step-up. The in-process Admin session remains open after Apply, and leaving Admin relaunches View-only through the Windows shell so the elevated token is dropped. A future broker must preserve the same default-deny checks and add authenticated, bounded IPC rather than accepting arbitrary serialized objects.
