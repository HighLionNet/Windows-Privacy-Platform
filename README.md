# Windows Privacy Platform

**Current milestone:** Version **1.3**  
**Previous:** v1.2 → v1.1 → v1.0 (WPF host) → v0.9.5 → …

Local, **read-only** privacy and security **knowledge explorer** for Windows.

> **Understand first. Change later.**

Windows Privacy Platform helps people see *what* is configured, *which layer* appears to control it, *where the observation came from*, and *why that matters* — without modifying the system.

It is not a registry cleaner, optimizer, tweaker, score engine, compliance suite, or one-click hardener.

Presentation target: a serious Windows / enterprise management console (Event Viewer / Services / MMC / XDR console family) — property sheets, dense lists, progressive disclosure — with a mid-cyber visual density for legibility.

---

## Why this project exists

Windows configuration is layered. A camera permission in Settings may be overridden by machine AppPrivacy policy. Telemetry may exist in two policy stores that disagree. Advertising ID is not the same thing as diagnostic data level.

Most tools either dump raw registry values or apply “privacy tweaks” without explaining effective precedence. This project makes configuration **legible**: observations, explanations, relationships, provenance, and honest unknowns.

---

## Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF)
```

| Project | Role |
|---------|------|
| **Models** | Catalog, ValueSemantics, MachineOverview, resolution, explanations, SettingsQuery, NavigationBuilder |
| **Scanner** | Collectors, binders, **PolicyPrecedenceResolver** |
| **CLI** | Console + TUI |
| **App** | WPF desktop host (presentation only) |

---

## What v1.3 changes

GUI-only refinement for human navigation:

- **Domain pages** show each setting as a card: current setting, effective state, options (from ValueSemantics), left accent colored by conflict / unknown / normal  
- **Setting detail** primary surface matches the sketch layout: current + effective on the left, options table on the right; secondary knowledge stays in expanders  
- **Home** decluttered: removed domain tiles and expanders; Identity / Security / Scan property sheets + conflict attention only  
- Mid-cyber palette and monospace values for denser, cooler legibility without game aesthetics  
- Backend frozen  

---

## Building

Requirements: .NET 8 SDK, Windows.

```powershell
cd Source
dotnet build -c Release
```

---

## Running

```powershell
# Desktop (primary)
cd Source\WindowsPrivacyPlatform.App
dotnet run -c Release

# CLI / TUI
cd Source\WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --tui
```

| Host | Behavior |
|------|----------|
| **App** | Management-console UI |
| CLI default | Machine overview + summary + conflicts |
| `--full` | Catalog dump |
| `--tui` | Interactive console explorer |

---

## Safety

This build does **not** write to the registry, change services/tasks/packages/policies/firewall rules, request elevation, remediate, score, or send product telemetry.

---

## Roadmap

| Horizon | Focus |
|---------|--------|
| **v1.5** | Dark theme, export, comparison design, domain depth |
| **Far future** | Controlled reversible change — separate design only |

Details: [`Status/AI_Handoff.md`](Status/AI_Handoff.md)

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
