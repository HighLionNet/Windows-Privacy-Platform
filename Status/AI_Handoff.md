# Engineering Handoff

**Line:** v2.5.1 source  
**Contracts:** [Safety_Model.md](Safety_Model.md), [Architecture.md](Architecture.md)  
Release chronology lives in git. Do not recreate History notes.

## Standing constraints

- WPF app only. Do not restore the prototype CLI or a duplicate source tree.
- `Directory.Build.props` is the only literal version source. UI identity uses `ProductInfoReader`.
- Settings, System Explorer, and Services are separate flows. Dynamic inventory cannot gain a `WritableTarget`.
- Writes require a complete typed target, compiled-allowlist match, applicability, and one confirmation. Discovery never authorizes a write.
- Change contract: allowlist compare → pre-read → confirm → typed operation → independent value-and-kind read-back → local audit. Partial batch failure stays structured.
- `ManagedObjectCatalog.IsAuthorizedWriteTarget` and `ElevationService.CanModifyHive` revalidate at apply time. A complete runtime object is not authority.
- Cross-account HKCU writes are refused.
- Preserve `SettingsListTarget`: Overview/search land on a category list and must not auto-open Settings detail.
- BitLocker, Device Encryption, and UAC are typed high-impact registry Settings: themed warning plus fresh credential step-up, then the normal verified-change contract.
- Firewall writes: twelve profile properties only. Arbitrary rules stay inventory-only.
- Service runtime start/stop/restart is allowed only for verified non-Microsoft optional services after Admin authorization and `ServiceMutationPolicy` approval. Startup configuration is never changed. Critical, Microsoft, unknown, shared-host, boot/system, and incomplete-evidence services stay diagnosis-only.
- Tasks, packages, features, and capabilities have no mutation backend.
- Keep distinct unknown/error/access-denied/not-configured states and last-good scan state.
- Neutral evidence language. No malware verdicts. No synthetic score.
- Every static catalog entry needs a complete plain-language narrative and either a complete target or a non-default `ExclusionReason`. One authorization/round-trip test per curated allowlist entry.
- No bulk hardening profiles, generic editors, telemetry, silent persistence, or dynamic authorization.

## Finish work that does not change authority

Isolated authenticated elevation broker, publisher signing/reputation, accessibility certification, localization, release screenshots, optional read-only export, additional verified catalog coverage.

New write surfaces need the full authorization, recovery, and per-target verification standard — not a generic adapter.

## Agent communication

Git is the bus. Do not add status diaries. Use this block in chat or a PR:

```text
WPP-HANDOFF
TASK:
FROM: Grok|Codex|Human
TO: Grok|Codex|Human
TYPE: plan|implement|test|audit|challenge|escalate|complete
BRANCH:
BASE: v2.5.1@<shortsha>
TARGET: v2.5.1
COMMIT: <shortsha>|uncommitted
PUSHED: yes|no
STATUS:
RISK: low|normal|safety|architecture
CHANGED:
- 
VERIFIED:
- <command> → PASS|FAIL|NOT-RUN
KNOWN:
- 
INSPECT:
- 
NEXT:
- 
BLOCKS-COMPLETE: yes|no
```

## Release check

```powershell
dotnet restore .\Source\WindowsPrivacyPlatform.sln
dotnet build .\Source\WindowsPrivacyPlatform.sln -c Release --no-restore
dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release --no-build
```

Packaging and `scripts\build-release.ps1` only when the task is distribution.
