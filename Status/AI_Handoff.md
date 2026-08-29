# Engineering Handoff

**Line:** v2.6.1 on `v2.6.1` (base: v2.5.1 @ `b06efc9`)
**Implementation:** four-section shell and observe-only protection-product inventory @ `f113ea9`
**Contract branch:** `wpp/docs/four-section-hub`
**Archive, do not merge:** `codex/2.6.0` @ `1697709`

Contracts: [Safety_Model.md](Safety_Model.md), [Architecture.md](Architecture.md).
Implementation brief: [Work/four-section-hub.md](Work/four-section-hub.md).

## Standing constraints

- WPF app only. No CLI, no duplicate tree.
- `Directory.Build.props` is the only literal version. UI identity uses `ProductInfoReader`.
- Four sections: Privacy, Security, Network, Troubleshoot/Explore. Persistent: Dashboard, Conflicts, Knowledge, App Settings, About, View-only / Administrator.
- Settings vs inventory stay separate. Dynamic inventory cannot gain a `WritableTarget`.
- Writes: curated Settings + existing firewall-profile path + existing optional non-Microsoft service runtime actions only. Discovery never authorizes a write.
- Change contract: allowlist compare → pre-read → confirm → typed operation → independent value-and-kind read-back → local audit.
- `ManagedObjectCatalog.IsAuthorizedWriteTarget` and `ElevationService.CanModifyHive` revalidate at apply. A complete runtime object is not authority.
- Cross-account HKCU refused.
- `SettingsListTarget` stands.
- BitLocker / UAC **policy** Settings remain high-impact (warning + fresh credential) then the normal contract. BitLocker *lifecycle* is not implemented.
- Firewall writes: twelve profile properties. Rules are inventory.
- Troubleshoot/Explore is read-only this phase (scan, handoff, fixed probes only).
- Detect AV / EDR / XDR; do not disable, exclude, or compete with them. No malware verdicts. No synthetic score.
- Distinct unknown / error / access-denied / not-configured / last-good scan.
- Narrative: documented function, observed state, labeled recommendation — recommendation is never a Microsoft fact.
- Every static catalog entry: complete narrative and either a complete target or non-default `ExclusionReason`.
- No bulk profiles, generic editors, telemetry, silent persistence, dynamic authorization.
- Do not restore 2.6 native write backends.
- WPF-UI, when added, is pinned **4.3.0**. Main window only. Dialogs stay `Window` this slice.

## Current release delta

`v2.6.1` is based on the four-section contract commit `5cb1673`; its product parent remains v2.5.1 @ `b06efc9`.

Delta: four exclusive sections, WPF-UI 4.3.0 main-window chrome, square inset presentation tokens, and fail-soft Security Center antivirus observation. The v2.5.1 write contract and authorization table are unchanged; Troubleshoot/Explore remains read-only.

Next: audit `origin/v2.6.1` against [Work/four-section-hub.md](Work/four-section-hub.md) and the v2.5.1 safety invariants.

## Agent communication

```text
WPP-HANDOFF
TASK:
FROM: Grok|Codex|Human
TO: Grok|Codex|Human
TYPE: plan|implement|test|audit|challenge|escalate|complete
BRANCH:
BASE: v2.5.1@b06efc9
TARGET: v2.6.1
COMMIT:
PUSHED:
STATUS:
RISK:
CHANGED:
-
VERIFIED:
- dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release →
KNOWN:
-
INSPECT:
-
NEXT:
-
BLOCKS-COMPLETE:
```

## Release check

```powershell
dotnet restore .\Source\WindowsPrivacyPlatform.sln
dotnet build .\Source\WindowsPrivacyPlatform.sln -c Release --no-restore
dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release --no-build
```
