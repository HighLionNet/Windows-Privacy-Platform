# Safety Model — Windows Privacy Platform v2.1

## Core rule

**Understand first. Change deliberately. Never invent certainty.**

## Read-only default

- The application starts and operates in Inspect mode.
- No registry writes, no service changes, no firewall rule mutation, no elevation until the user explicitly enters Modify mode.

## Explicit Modify authorization

1. User chooses Modify.
2. If the process is not elevated, UAC relaunch is offered. The old process exits.
3. After elevation, the user must again explicitly authorize Modify for the session.
4. Elevation alone never grants Modify authorization.

## WritableTarget (deny-by-default)

- Only settings with an **explicit, complete** `WritableTarget` in the catalog may be modified.
- `DiscoveryMethod` is observation metadata only. It **never** creates write permission.
- Authorization is ObjectId-whitelisted in the catalog.
- Firewall domain is hard-blocked from writes.

Each WritableTarget specifies:

- Hive, View, SubKey, ValueName
- Exact `RegistryValueKindExpected`
- Supported raw values (when applicable)
- SupportsDeletion
- RequiresElevation
- Notes

## Write path

1. Resolve explicit WritableTarget (not UI observation).
2. Pre-read exact target (kind + value). Read failure aborts the change.
3. User confirmation shows system current state vs intended.
4. Write using the catalog kind only.
5. Independent read-back (up to 3 attempts).
6. Success only if value **and** kind match. Textual match with wrong kind is failure.
7. Audit log entry (local only).

## Firewall boundary

Firewall rules and generic firewall command execution are observation-only. No firewall rule writing through WPP.

## Evidence integrity

- Errors and access-denied are not reported as "Not configured".
- Canceled or failed scans do not replace the last successful scan in the UI.
- Collector failures appear in scan diagnostics; a scan is not marked fully successful when important collectors fail.

## Process execution

- Collectors that need external tools use `SafeProcessRunner` (fixed executable + arguments, concurrent stream drain, timeout kill, no shell).
- No arbitrary user-controlled command execution surface.

## Logging & privacy

- Logs remain local under the user’s profile / LocalAppData.
- No telemetry, no cloud backend, no network phone-home.

## Unknown is preserved

Unknown, not observed, error, unsupported, and not configured are distinct concepts. The product does not invent certainty.
