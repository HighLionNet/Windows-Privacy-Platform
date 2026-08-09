# Current Status — Windows Privacy Platform **v2.1**

**Date:** 2026-08-09

## Product

WPF GUI only. Application version **2.1.0**.

## Hardening (completed)

- Explicit WritableTarget ObjectId whitelist (DiscoveryMethod never authorizes)
- Kind-aware write verification; read errors ≠ Not configured
- SafeProcessRunner; CapabilityCollector + ScheduledTaskCollector on it
- Scan diagnostics + last-good scan preservation + catalog clone per scan
- Tests project + CI
- Safety_Model.md, LICENSE, SECURITY.md, CONTRIBUTING.md

## Coverage expansion (completed)

- CatalogExpansion: full AppPrivacy LetApps* set, Defender depth, Windows Update policies, Search/Activity/Cloud, Location/Device/Biometrics, UAC observation, BitLocker **policy** observation, Edge privacy policies, key service anchors (observation-only)
- AppPrivacy writable via explicit whitelist
- BitLocker / UAC master switches / services remain non-writable by design
- Conflicts page: concise Effective + writable status (no UI clutter)

## Explicit non-goals retained

No bulk modify, profiles, rollback, privacy score, CLI product, firewall rule writing, generic service/task editors.

## Local verify

```powershell
cd Source
dotnet build WindowsPrivacyPlatform.sln -c Release
dotnet test WindowsPrivacyPlatform.sln -c Release
```
