# Work: four-section hub shell

## Intent

Present v2.5.1 as four sections with a native-leaning WPF-UI shell. Keep every write path identical to v2.5.1.

## Non-goals

- New `WritableTargetKind` or 2.6 backends
- Firewall rule CRUD, BitLocker lifecycle, task/package/feature mutation
- Version bump to 2.6.0
- Fluent modal dialogs
- Privacy/security score
- Fighting or configuring third-party AV/EDR

## BASE / branch

```text
BASE:   v2.5.1@b06efc9
BRANCH: v2.6.1
TARGET: v2.6.1
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

App:

- Pin `WPF-UI` **4.3.0**.
- Main window: `FluentWindow` + `ui:TitleBar` (ShowClose/ShowMaximize/ShowMinimize true) + native Menu File/Edit/View/Settings/Help + context bar + section rail.
- ThemeManager bridges WPF-UI appearance to existing WPP brushes.
- Dialogs remain `Window`.
- Hide Apply when `!IsWritable`.

## Verify

```powershell
dotnet restore .\Source\WindowsPrivacyPlatform.sln
dotnet build .\Source\WindowsPrivacyPlatform.sln -c Release --no-restore
dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release --no-build
```

GUI smoke: caption Close works; File/Edit/View/Settings/Help exist; section toggles swap the rail; View-only cannot Apply; Explore has no Apply; dialogs are Window.
