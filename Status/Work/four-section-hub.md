# Work: four-section hub shell

Delete this file when the shell slice merges.

## Intent

Present v2.5.1 as four sections with a native-leaning WPF-UI shell. Keep every write path identical to v2.5.1.

## Non-goals

- New `WritableTargetKind` or 2.6 backends
- Firewall rule CRUD, BitLocker lifecycle, task/package/feature mutation
- Version bump to 2.6.0
- Fluent modal dialogs
- Privacy/security score
- Deep-scan product, perfmon HTML parser
- Fighting or configuring third-party AV/EDR

## BASE / branch

```text
BASE:   v2.5.1@b06efc9  (include wpp/docs/four-section-hub Status/ first)
BRANCH: wpp/feat/four-section-shell
TARGET: v2.5.1
RISK:   normal (UI + taxonomy). Becomes architecture if any write path moves.
```

## Invariants that must not move

- `PolicyChangeService` sequence and registry-kind read-back
- `CuratedWriteAuthorizations` / `IsAuthorizedWriteTarget`
- Dynamic inventory has no `WritableTarget`
- Distinct evidence states and last-good scan
- `SettingsListTarget`
- Service mutation policy unchanged and not shown as Explore actions
- No `cmd.exe` / user-built process lines

## Surfaces to touch

Models:

- Add `HubSection { Privacy, Security, Network, Explore }` (Explore display name **Troubleshoot/Explore**).
- Map `ProductDomain` → `HubSection` exactly as `Status/Architecture.md`.
- Navigation models: section-scoped category lists; persist items are not section categories.

App:

- Pin `WPF-UI` **4.3.0** in `WindowsPrivacyPlatform.App.csproj`.
- Main window: `FluentWindow` + context bar + section-colored rail header + section category items + persist footer items.
- Remove unused native `Menu` (`File View Settings Tools Help`) if present.
- ThemeManager bridges WPF-UI appearance to existing WPP brushes. One token system.
- Dialogs remain `Window`.
- Setting bars: square inset cards; hide Apply when `!IsWritable`.
- Dashboard: keep identity + evidence tiles. Add a protection-product line when Security Center data exists (“Defender active” / “Vendor X reported” / “not observed”). No score.

Scanner (minimal, fail-soft):

- Observe installed AV/EDR via the existing identity/WMI patterns already in `WindowsIdentityCollector` if present; otherwise a small fail-soft read of Security Center / `AntivirusProduct`. Access denied → not observed. Do not query vendor APIs. Do not change protection state.

Tests:

- Every `ProductDomain` maps to exactly one `HubSection`.
- Firewall `ProductDomain` is Network, not Security.
- WindowsUpdate is Security.
- Explore destinations are non-writable.
- Existing catalog integrity and authorization tests still pass.
- Product version still reads from `Directory.Build.props` (still 2.5.1 unless human says bump).

## Visual spec (do not improvise)

- Context bar taller than the current breadcrumb-only strip.
- Four toggle buttons: Privacy, Security, Network, Troubleshoot/Explore. Text + Fluent icon. Exclusive. Selected = drop shadow / elevation, not a filled pill.
- Left rail header: section title + section color token (Privacy / Security / Network / Explore each have one accent).
- Left rail body: only that section’s categories. Accordion/collapse groups if the list overflows the way 2.5.1 already struggles.
- Content cards inset from the window edge. Corner radius 2–4px. No stadium pills on setting actions.
- Icons via WPF-UI `SymbolIcon`, not ad-hoc PNG sets.

## Windows facts vs UNCERTAIN

- Security Center `AntivirusProduct` is the supported observation channel for “what protection product is registered.” UNCERTAIN: completeness for every EDR vendor. Fail soft. Never treat absence as “no AV.”
- `perfmon /report` is a native diagnostics handoff. Do not parse the HTML in this slice. A launch tile in Explore is enough if `NativeToolLauncher` already allowlists `perfmon.exe`; if not, add a compiled allowlist row or skip launch and leave a later slice.
- Effective DNS is multi-path (adapter, DHCP, DoH policy, NRPT, VPN, browser). This slice only *places* a Network → DNS category. Do not invent DNS writes.

## Human-escalation triggers

Stop and challenge if the plan appears to require: new write kind, disabling Defender or third-party AV, Fluent dialogs, copying 2.6 `PolicyChangeService`, or bumping version to 2.6.0.

## Verify

```powershell
dotnet restore .\Source\WindowsPrivacyPlatform.sln
dotnet build .\Source\WindowsPrivacyPlatform.sln -c Release --no-restore
dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release --no-build
```

GUI smoke (human): each section toggle swaps the rail; persist pages remain reachable; Apply still works on one existing Settings row in Administrator; View-only still cannot apply; Explore shows no Apply; dialogs open without crash.

## Codex report

End with the WPP-HANDOFF block. Include `git diff v2.5.1...HEAD --stat` and any plan deviation.
