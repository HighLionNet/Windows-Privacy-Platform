# Current Status — Windows Privacy Platform v2.0

**Date:** 2026-08-09

## Shipped on main (this session)

### Correctness (P0)
- **Unknown is never converted to Not configured** (NavigationBuilder, SettingsQuery, PolicyPrecedenceResolver, CategoryView)
- **Option buttons show ONLY raw values** (no `1. 0` / `2. 1` numbering)
- **No invented 0/1 options** when ValueSemantics is missing
- **WritableTarget** explicit modification contract (deny-by-default)
- **PolicyChangeService** requires WritableTarget; type-safe writes; exact-target verification; firewall domain refused
- **Firewall** remains observation-only (no WritableTarget attached)
- **Catalog** attaches WritableTarget for concrete non-firewall registry settings
- **CLI project removed** from solution
- **Version 2.0.0** via Directory.Build.props
- **About** screen rewritten for v2.0

### Known remaining (next pass)
- Full test project + CI workflows
- Scan lifecycle generation-ID / cancellation safety
- Structured ScanDiagnostics model
- Central safe process runner
- CapabilityCollector installed-vs-available fix
- PolicyCollector/catalog consistency audit for duplicate probes
- Window off-screen restore hardening
- Knowledge base depth expansion
- OSS docs (LICENSE, SECURITY.md, CONTRIBUTING, Actions)

### How to update local app
```
cd "C:\Windows Privacy Platform"
git fetch origin
git checkout main
git pull origin main
cd Source
dotnet build WindowsPrivacyPlatform.sln -c Release
```
Then run the Release exe (or use your existing shortcut after rebuild).
