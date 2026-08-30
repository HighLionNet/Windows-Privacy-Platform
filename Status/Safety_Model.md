# Safety Model

## Authority and evidence

Windows Privacy Platform has two session modes: View-only and Administrator. Administrator mode is backed by a Windows-elevated process token and an in-memory authorization lifetime; UI state is not authority. Cross-account HKCU writes are refused.

Unknown, not observed, not configured, unsupported, stale, access denied, and error remain distinct. Unknown is never rendered as off, safe, or absent. A canceled or failed scan does not replace the last completed snapshot, and inventory actions require a completed snapshot no older than 30 minutes.

## Two mutation classes

| Class | Surface | Service | Authorized operation |
| --- | --- | --- | --- |
| Settings write | Privacy, Security, Network Settings | `PolicyChangeService` | Compiled registry `WritableTarget` only |
| Inventory action | Troubleshoot/Explore | `InventoryChangeService` | Optional non-Microsoft service runtime action; non-Microsoft task enable/disable |

These classes never cross. Dynamic inventory rows never receive a `WritableTarget`, discovery text never authorizes a write, and inventory actions never pass through `PolicyChangeService`.

### Settings writes

Registry writes require a complete compiled allowlist row, supported raw value, exact hive/view/path/value kind, Administrator authority where required, pre-read, one human confirmation, typed write, independent value-and-kind read-back, and local audit. `ManagedObjectCatalog.HasValidAuthorizationHash` remains an interlock. Non-registry `WritableTarget.Kind` values are denied by `PolicyChangeService`.

Firewall writes remain exactly the twelve profile properties: enabled, default inbound, default outbound, and inbound notifications for Domain, Private, and Public. Firewall rules remain inventory.

BitLocker and UAC policy Settings remain high-impact and require a warning plus a fresh administrator credential. The credential token must represent an administrator. Credential buffers are pinned only while used and securely zeroed; credentials never enter disk or audit logs.

### Inventory actions

The identifier must exactly match a row in the current scan snapshot. Administrator authority and one confirmation must show current state, intended state, side effects, and recovery. Success requires independent live read-back and a local audit event.

- Services: start, stop, or restart only. `ServiceMutationPolicy` denies critical names, Boot/System start, shared `svchost -k` groups, Microsoft or unknown publishers, missing/incomplete evidence, and access-denied rows. Startup type is never changed.
- Scheduled tasks: enable or disable only. `TaskMutationPolicy` denies Microsoft paths, Defender, BitLocker, Update, Task Scheduler maintenance, malformed paths, and paths absent from the snapshot. The implementation uses only fully qualified `System32\schtasks.exe` with fixed verbs and the exact observed task path. It never creates, deletes, or changes task commands.

## DNS and external applications

DNS evidence is layered: selected general interface, VPN/tunnel participation, per-adapter resolver addresses and source evidence, NRPT namespaces, Windows DoH policy, fixed-name probes against observed resolvers, and explicit ExternalApp boundaries for browser/VPN DNS. An empty resolver list is Unknown, not disabled. A failed probe is Error. Browser Secure DNS is never inferred from the Windows DNS client.

Adapter DNS writes are not authorized. The DNS page exposes only the existing DoH, LLMNR, and NetBIOS registry Settings plus a Windows Settings handoff.

## Binary integrity

The executable SHA-256 is always computed and displayed. For unsigned community builds, a changed hash is status information and does not by itself block high-impact Settings. For Authenticode-signed builds, high-impact Settings require a valid certificate chain whose publisher matches HighLionNet. The catalog authorization hash remains separate and mandatory.

## Permanent exclusions

No disk wipe, recovery-key deletion, account deletion, generic script execution, bulk hardening/debloat, AppX or provisioned-package removal, optional-feature or capability mutation, firewall-rule CRUD, BitLocker lifecycle operation, UAC master switch, Edge/WebView2 uninstall, service start-type change, adapter DNS write, or task delete exists in this product line. AV/EDR/XDR products are observed and never fought.
