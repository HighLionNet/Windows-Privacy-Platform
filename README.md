# Windows Privacy Platform

**Current milestone:** Version **1.5**  
**Previous:** v1.4 → v1.3 → …

Local, **read-only by default** privacy and security **knowledge explorer** for Windows.

> **Understand first. Change later.**

Modify mode is an elevation scaffold only (WindowsPrincipal). No write paths are implemented in this version.

---

## Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF)
```

---

## What v1.5 delivers

- Deep Windows Update catalog (deferrals, WUServer, TargetRelease, ManagePreviewBuilds, DualScan, ElevateNonAdmins, …)
- Defender Exploit Guard (Network Protection, Controlled Folder Access, Cloud Block Level, Block at First Sight, Script Scanning, Catch-up scans)
- SmartScreen and Clipboard history / cross-device policies
- Matching PolicyCollector probes and ValueSemantics
- ElevationService for Modify mode (elevated token + confirmation; auth.log / changes.log)
- Setting detail without expanders — minimal always-visible knowledge blocks
- App Version 1.5.0

---

## Building

```powershell
cd Source
dotnet build -c Release
```

## Running

```powershell
cd Source\WindowsPrivacyPlatform.App
dotnet run -c Release
```

---

## Safety

This build does **not** write to the registry, change services/tasks/packages/policies/firewall rules, or remediate. Modify mode only authorizes an elevated session for future controlled-change design.

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
