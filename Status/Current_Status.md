# Current Status — Windows Privacy Platform v2.1

**Date:** 2026-08-25
**Authoritative application version:** **2.1.0** (`Directory.Build.props`)

## Release state

v2.1 is implemented on `release/v2.1` and ready for pull-request review. The WPF GUI remains the only product surface.

## Completed in v2.1

- Fixed semantic conflict false positives by comparing canonical values before raw registry encodings; regression tests cover alternate policy stores and layer-rank resolution.
- Added the startup Inspect/Modify chooser and `--resume-modify` elevation handoff, leaving one deliberate session-authorization confirmation after UAC.
- Added global dispatcher/domain exception logging and graceful error presentation.
- Added normalized smooth scrolling to the main scroll surfaces and generated domain navigation from the catalog.
- Corrected probe/catalog mismatches and added UAC, BitLocker policy, Recall, Copilot, ASR, Windows Hello, Storage Sense, Widgets, Search, accessibility, Edge, network, and diagnostic-data observations.
- Added elevated read-only live BitLocker WMI status; non-elevated scans report `Requires Modify mode to observe`.
- Added read-only local security-policy export through `secedit`, plus curated service/task/package/capability inventory binding.
- Routed package inventory through `SafeProcessRunner` and removed the stale duplicate root `KnowledgeBase/` source tree.
- Expanded the catalog from 135 to **257** entries across 26 represented product domains.
- Made complete per-setting narratives mandatory, removed explanation-factory category defaults from the normal path, and added tests for completeness, no fallback, and paragraph reuse.

## Safety state

The deny-by-default boundary is unchanged. Only complete, ObjectId-whitelisted `WritableTarget` entries can write. UAC master switches, BitLocker, firewall, services, scheduled tasks, packages, capabilities, ASR rules, and local security policy are observation-only.

The write sequence remains pre-read → confirm → typed write → independent value-and-kind read-back → local audit log.

## Validation

The release validation target is:

```powershell
dotnet restore Source\WindowsPrivacyPlatform.sln
dotnet build Source\WindowsPrivacyPlatform.sln -c Release
dotnet test Source\WindowsPrivacyPlatform.sln -c Release
dotnet publish Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj -c Release -r win-x64 --self-contained false
```

See `Status/AI_Handoff.md` for implementation details and the final observed validation results.

Final local result: locked restore passed; Release build passed with 0 warnings and 0 errors; 19/19 tests passed; win-x64 publish passed; hidden WPF startup smoke remained responsive.
