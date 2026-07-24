# Windows Privacy Platform

**Current milestone:** Prototype **v0.8**  
**Previous:** v0.7 (TUI + explanation polish) → v0.6 FINAL → v0.5 → v0.4 → v0.3 → v0.2 → v0.1  

Local, **read-only** privacy and security **knowledge explorer** for Windows.

> **Understand first. Change later.**

Windows Privacy Platform helps people see *what* is configured, *which layer* appears to control it, *where the observation came from*, and *why that matters* — without modifying the system.

It is not a registry cleaner, optimizer, tweaker, score engine, compliance suite, or one-click hardener.

---

## Why this project exists

Windows configuration is layered. A camera permission in Settings may be overridden by machine AppPrivacy policy. Telemetry may exist in two policy stores that disagree. Advertising ID is not the same thing as diagnostic data level.

Most tools either:

- dump raw registry values, or  
- apply “privacy tweaks” without explaining effective precedence  

This project exists to make Windows privacy and security configuration **legible**: observations, explanations, relationships, provenance, and honest unknowns — presented as an interactive map rather than a scary score.

---

## Core philosophy

| Principle | Meaning |
|-----------|---------|
| Explain before change | Understanding precedes any future remediation design |
| Never pretend certainty | Ambiguity is shown, not papered over |
| Unknown is acceptable | Missing evidence stays Unknown |
| Relationships matter | Overrides and related features are first-class |
| Layer precedence is core | Effective value is reasoned, not guessed |
| Read-only first | Exploration must be safe on real machines |
| Transparency over automation | Reasons and provenance over magic |
| Knowledge before remediation | Catalog quality is the long-term asset |
| Quality over quantity | Excellent explanations beat bulk ADMX imports |
| Trust is the product | Honesty beats marketing language |

---

## Architecture overview

Seven C# / .NET 8 projects with one-way dependencies:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

| Project | Role |
|---------|------|
| **Models** | Catalog, snapshot, MachineOverview, resolution types, explanations, SettingsQuery, NavigationBuilder |
| **Core** | Shared primitives |
| **Logging** | Audit logging |
| **KnowledgeBase** | In-memory knowledge store |
| **Validator** | Structural schema checks only |
| **Scanner** | Collectors (incl. multi-source identity, Firewall), binders, **PolicyPrecedenceResolver** |
| **CLI** | Pipeline host, reports, **TuiHost** (Home → Machine Overview → Domains) |

```
Collectors → InventorySnapshot → Binders → PrecedenceResolver
    → SettingsQuery / NavigationBuilder / SettingExplanation / MachineOverview
    → CLI report or TUI
```

---

## What v0.8 adds

- **Machine Overview** — separate device-context landing surface (OS, hardware, Secure Boot/TPM/BitLocker placeholders, domain, firewall/Defender service summary, catalog version). Not a score.
- **Evidence / provenance** on `ConfigurationObservation` (CollectorName, EvidenceSource, AlternativeSources, CollectionNotes, EffectiveConfidence).
- **Resilient WindowsIdentityCollector** — registry primary + RuntimeInformation + optional WMI/CIM cross-checks; confidence and notes when sources agree or fail.
- **Firewall domain** — curated catalog entries + read-only `FirewallCollector` (Domain/Private/Public profiles, defaults, logging summary, MpsSvc state).
- **TUI Home** — Machine Overview vs Explore Domains separation.

---

## Building

Requirements: .NET 8 SDK, Windows (for Scanner/CLI `net8.0-windows`).

```powershell
cd Source
dotnet build -c Release
```

---

## Running

```powershell
cd Source\WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
dotnet run -c Release -- --tui
dotnet run -c Release -- --help
```

| Flag | Behavior |
|------|----------|
| *(default)* | Machine overview + observation summary + high-impact watch list + conflict cards |
| `--full` | Complete catalog dump by domain with What/Why |
| `--tui` | Interactive read-only explorer (Home → Machine Overview / Domains) |
| `--help` | Help |

---

## Safety

This build does **not** write to the registry, change services/tasks/packages/policies/firewall rules, request elevation, remediate, score, or send product telemetry.

---

## Roadmap

| Horizon | Focus |
|---------|--------|
| **v0.9** | Deeper provenance consistency; Defender/Update/Telemetry expansion; compare-only baselines |
| **v1.0 vision** | Stable read-only knowledge product |
| **Far future** | Controlled reversible change — separate design, explicit authorization only |

Details: [`Status/AI_Handoff.md`](Status/AI_Handoff.md)

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
