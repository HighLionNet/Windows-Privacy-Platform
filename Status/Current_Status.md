# Current Status — Windows Privacy Platform **v2.1**

**Date:** 2026-08-09

## Product

WPF GUI only (CLI not in solution). Application version **2.1.0**.

## v2.1 hardening completed this session

- **WritableTarget** is deny-by-default and ObjectId-whitelisted only. DiscoveryMethod never authorizes writes.
- **ValueKind** is explicit per ObjectId (no heuristic from discovery path for authorization).
- **PolicyChangeService**: kind-aware verification; read errors (AccessDenied/Error) are not treated as "Not configured"; RequiresElevation is enforced from WritableTarget.
- **SafeProcessRunner**: concurrent stdout/stderr, timeout kill, cancellation, no shell.
- **CapabilityCollector** rewritten on SafeProcessRunner; no `-ExecutionPolicy Bypass`; shorter timeouts; empty ≠ proven absence.
- **ScanResult** + **InventoryScanner**: structured per-collector diagnostics; ScanStatus; cancellation support; no false full success when collectors fail.
- **ScanService**: passes CancellationToken; preserves last successful scan on cancel/fail; clones catalog definitions per scan to avoid stale observation mutation; version 2.1.
- Authoritative version source: `Directory.Build.props` → **2.1.0**.

## Remaining (next feature prompt)

- Full automated test project expansion (precedence, ValueSemantics, WritableTarget validation).
- GitHub Actions CI polish.
- Stronger SchemaValidator rules (WritableTarget completeness, relationship resolution).
- Architecture.md / README / SAFETY model docs completion.
- Preference file robustness and remaining collector error/absence distinctions.
- Optional further policy catalog audit entries.

## Update local

```powershell
cd "C:\Windows Privacy Platform"
git pull origin main
cd Source
dotnet build WindowsPrivacyPlatform.sln -c Release
```
