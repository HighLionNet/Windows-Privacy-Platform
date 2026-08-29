# Safety Model

**Line:** v2.5.1 source (`b06efc9`) plus the four-section hub contract.
**Phase:** this document authorizes information-architecture and presentation work. It does not authorize new write backends.

## Product job

WPP is a local Windows 10/11 privacy, security, and network **policy console** with a read-only inventory and diagnostics dock. It is not WinUtil, not Malwarebytes, and not an EDR.

- Privacy must beat a tuner by listing every effecting path and only mutating researched typed targets.
- Security must beat a consumer AV *console* at Defender / ASR / BitLocker / UAC / Hello / Update evidence. It must not ship a signature engine, web shield, or ransomware rollback.
- The app detects installed AV / EDR / XDR and must not fight them.

## Session authority

View-only and Administrator persist across every section. View-only cannot mutate. Administrator is an explicit session choice. Elevation is an OS privilege, not a catalog permission. Leaving Administrator relaunches View-only unelevated.

## Four sections

The product surface is four exclusive sections plus persistent utilities.

| Section | Job | Mutation in this phase |
|---|---|---|
| Privacy | Curated privacy policy and Windows-app privacy | Existing curated Settings writes only |
| Security | Defender, ASR, BitLocker policy, UAC, Hello, Update, local security | Existing curated Settings writes only |
| Network | Adapters/LAN, DNS, firewall profiles and rule *inventory*, remote access, network protocols | Existing curated Settings writes only (today: firewall **profiles**, network registry policy) |
| Troubleshoot/Explore | Full live inventory, native handoffs, fixed live probes | None |

Persistent across sections: Dashboard, Conflicts, Knowledge Explorer, App Settings, About, scan, search, View-only / Administrator.

A setting belongs in exactly one section rail. Firewall rules and profiles live in Network. Security may show a posture chip that navigates to Network → Firewall. Do not duplicate the same editable list in two rails.

## Category completeness

If a category exists, every path that can control the outcome is **listed** on that page. Listing is not permission to write.

Each path is exactly one class:

| Class | Meaning |
|---|---|
| Writable | Curated Settings entry, applicable, complete typed `WritableTarget`, compiled allowlist, user confirms |
| Observe | Shown with documented function + observed evidence |
| NativeHandoff | WPP will not mutate; open a compiled native tool or Settings URI |
| ExternalApp | Another app owns it (browser DoH, VPN client). Named, not faked |
| Unknown | Distinct evidence. Never drawn as off or safe |

Presentation is three layers and must stay visually separate:

1. **Documented function** — Microsoft / API / policy documentation.
2. **Observed state** — collector + binder + distinct evidence enum.
3. **WPP recommendation** — labeled as a recommendation, never as a Microsoft fact.

## What may be written (this phase)

Unchanged from v2.5.1 code:

1. Curated Settings entry, not Explorer / Troubleshoot inventory / internal reference.
2. Current edition/build and selected value applicable.
3. Exact ObjectId in the source-controlled authorization table.
4. Complete typed `WritableTarget` and supported raw value.
5. User confirms the bounded batch once.

Discovery, observation, search text, process output, and UI copy never authorize a write.

Verified sequence: allowlist compare → pre-read exact target (abort on ambiguous/failed read) → one confirmation of current / intended / side effects / recovery → typed operation → independent read-back of value **and** registry kind → success only on match → local audit. Textual match with the wrong `RegistryValueKind` is failure. At most 32 unique changes. Cross-account HKCU is refused. Failed or canceled scans do not replace last-good `ScanService` state.

Firewall writes remain the twelve profile properties (enabled / inbound / outbound / notifications × domain / private / public). Arbitrary firewall rules are inventory in Network and Troubleshoot/Explore.

Service start/stop/restart remains the existing typed `ServiceController` path: verified non-Microsoft optional services only, after Administrator confirmation and `ServiceMutationPolicy`. Startup type is never changed. Critical names, Microsoft or unknown publishers, Boot/System start, shared svchost groups, and incomplete evidence stay diagnosis-only. **Do not surface those verbs in Troubleshoot/Explore in this phase.**

Tasks, packages, features, capabilities, BitLocker *lifecycle* (encrypt/decrypt/protectors), AppX removal, DISM, and firewall-rule create/edit/delete stay unimplemented. `codex/2.6.0` is an archive, not a source to restore.

BitLocker, Device Encryption, and UAC **registry policy** already in Settings stay high-impact: themed warning plus fresh credential step-up, then the normal contract.

## Troubleshoot/Explore

Read-only in this phase. Allowed actions: scan, filter, open a compiled native handoff, run a **fixed** live probe.

Live probes use `SafeProcessRunner` or an in-process Windows API already used by collectors. Fixed executable, fixed arguments, timeout, no shell, no user-built command line.

Allowed probe examples: adapter link, DNS resolve against *observed* effective resolvers, firewall profile effective state, service running check, Reliability Monitor summary, launch `perfmon /report` and keep the report path as evidence.

Forbidden: generic “run this component,” ingesting perfmon HTML as write authority, synthetic health scores, bulk remediation.

## AV / EDR / XDR coexistence

On scan, record what Windows Security Center / `AntivirusProduct` (and equivalent Defender status) already exposes: product name, state, whether Defender is active or passive.

Show that on Dashboard and Security. Do not stop, disable, exclude, unload, or “optimize” another vendor’s product. Do not fight Tamper Protection. Do not present WPP as the real-time engine. If a third-party stack owns protection, WPP observes and hands off to Windows Security.

## Evidence

Unknown, not observed, not configured, unsupported, error, and access denied stay distinct. Unknown is never safe. No synthetic privacy or security score. Dashboard tiles are counts of evidence states and probe findings tagged Privacy / Security / Network / Explore.

## Permanent exclusions (product, not just this phase)

Disk wipe/format, recovery-key deletion, account deletion, generic command/script execution, bulk harden/debloat profiles, telemetry, silent persistence, dynamic authorization (live inventory minting a `WritableTarget`), competing AV engine.

Future write surfaces (DNS adapter writes, firewall-rule CRUD, task enable/disable, BitLocker lifecycle, and so on) need a new Safety_Model revision, researched ObjectIds, recovery, and tests. Human approval first. This file is not that approval.
