# Windows Privacy Platform

**Current milestone:** Prototype **v0.9.5**  
**Previous:** v0.9 (ValueSemantics + educational resolution) → v0.8 → v0.7 → v0.6 FINAL → … → v0.1  

Local, **read-only** privacy and security **knowledge explorer** for Windows.

> **Understand first. Change later.**

Windows Privacy Platform helps people see *what* is configured, *what the raw value means*, *which layer* appears to control it, *where the observation came from*, *why that layer wins*, and *how confident we are* — without modifying the system.

It is not a registry cleaner, optimizer, tweaker, score engine, compliance suite, or one-click hardener.

---

## Why this project exists

Windows configuration is layered. A camera permission in Settings may be overridden by machine AppPrivacy policy. Telemetry may exist in two policy stores that disagree. Advertising ID is not the same thing as diagnostic data level. Raw registry values such as `0` or `2` are meaningless without knowledge.

Most tools either dump raw values or apply “privacy tweaks” without explaining effective precedence.

This project exists to make Windows privacy and security configuration **legible**: observations, **value semantics**, explanations, relationships, provenance, and honest unknowns.

---

## Core philosophy

| Principle | Meaning |
|-----------|---------|
| Explain before change | Understanding precedes any future remediation design |
| Knowledge owns meaning | Catalog ValueSemantics; resolvers never invent raw-code maps |
| Never pretend certainty | Ambiguity is shown, not papered over |
| Unknown is acceptable | Missing evidence or missing maps stay Unknown |
| Relationships matter | Overrides and related features are first-class |
| Layer precedence is core | Effective value is reasoned, not guessed |
| Read-only first | Exploration must be safe on real machines |
| Transparency over automation | Reasons and provenance over magic |
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
| **Models** | Catalog, **ValueSemantics**, MachineOverview, resolution, explanations, SettingsQuery |
| **Core** | Shared primitives |
| **Logging** | Audit logging |
| **KnowledgeBase** | In-memory knowledge store |
| **Validator** | Structural schema checks + unique ObjectId guard |
| **Scanner** | Collectors, binders, **PolicyPrecedenceResolver** (precedence; meaning from catalog) |
| **CLI** | Pipeline host, reports, **TuiHost** |

```
Collectors → InventorySnapshot → Binders → PrecedenceResolver (+ ValueSemanticsInterpreter)
    → SettingsQuery / NavigationBuilder / SettingExplanation / MachineOverview
    → CLI report or TUI
```

---

## What v0.9.5 adds

- **Catalog maturity** — every PolicyCollector probe now has a catalog entry; ConsentStore coverage expanded  
- **ValueSemantics** for AUOptions, Delivery Optimization, MAPS/Spynet, sample submission, Edge tracking prevention, and the full binary polarity set  
- **Full AppPrivacy–ConsentStore relationship graph** (14 capability pairs) plus Defender / Update / Search / Location edges  
- **SchemaValidator** unique-ObjectId batch detection  
- SchemaVersion **0.9.5**  

(v0.9 retained: ValueSemanticsInterpreter, educational resolution reasons, provenance, WhenIgnored / CommonMisconception scaffolding.)

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
| **Next** | Surface semantics/evidence in UI; relationship query shapes; careful domain depth |
| **v1.0 vision** | Stable read-only knowledge product |
| **Far future** | Controlled reversible change — separate design, explicit authorization only |

Details: [`Status/AI_Handoff.md`](Status/AI_Handoff.md) · [`Status/Current_Status.md`](Status/Current_Status.md)

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
