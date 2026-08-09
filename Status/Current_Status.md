# Current Status — Windows Privacy Platform **v2.0**

**Date:** 2026-08-09

## Product

WPF GUI only (CLI removed). Application version **2.0.0**.

## Fixed this session

- Unknown never shown as Not configured
- Option **buttons** = raw value only (`0`, `1`, `—`, …) — no index numbering
- Option **notes** = clear DisplayLabel (Off / On / Force deny / Zero tolerance), not "Policy value 0."
- No invented 0/1 when ValueSemantics missing
- WritableTarget deny-by-default; PolicyChangeService type-safe verified writes
- Firewall observation-only
- UI + README + About all say **v2.0**

## Remaining

Tests/CI, scan lifecycle, ScanDiagnostics, SafeProcessRunner, CapabilityCollector fix, full policy catalog expansion, OSS docs.

## Update local

```powershell
cd "C:\Windows Privacy Platform"
git pull origin main
cd Source
dotnet build WindowsPrivacyPlatform.sln -c Release
```
