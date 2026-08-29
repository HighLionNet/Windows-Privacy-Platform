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
- Main window keeps `ui:TitleBar` min/max/close and a native File / Edit / View / Settings / Help menu. Human 2026-08-29: do not drop them.

## Current release delta

`v2.6.1` is based on the four-section contract commit `5cb1673`; its product parent remains v2.5.1 @ `b06efc9`.

Delta: four exclusive sections, WPF-UI 4.3.0 main-window chrome, square inset presentation tokens, fail-soft Security Center antivirus observation, native File/Edit/View/Settings/Help menu, and WPF-UI TitleBar caption buttons (min/max/close). The v2.5.1 write contract and authorization table are unchanged; Troubleshoot/Explore remains read-only.
