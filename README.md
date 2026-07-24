# Windows Privacy Platform

**Current milestone:** Prototype **v0.7**  
**Previous archive:** v0.6 FINAL (understanding foundation) → v0.5 → v0.4 → v0.3 → v0.2 → v0.1  

Local, **read-only** privacy and security **knowledge explorer** for Windows.

> **Understand first. Change later.**

Windows Privacy Platform helps people see *what* is configured, *which layer* appears to control it, and *why that matters* — without modifying the system.

It is not a registry cleaner, optimizer, tweaker, score engine, compliance suite, or one-click hardener.

---

## Why this project exists

Windows configuration is layered. A camera permission in Settings may be overridden by machine AppPrivacy policy. Telemetry may exist in two policy stores that disagree. Advertising ID is not the same thing as diagnostic data level.

Most tools either:

- dump raw registry values, or  
- apply “privacy tweaks” without explaining effective precedence  

This project exists to make Windows privacy and security configuration **legible**: observations, explanations, relationships, and honest unknowns — presented as an interactive map rather than a scary score.

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

## Design principles (engineering)

- **No business logic in UI** — CLI and TUI only present model decisions  
- **Catalog-first** — human-readable entries before new collectors  
- **Explanation-first** — every setting should teach something  
- **Collectors remain read-only and fail-soft**  
- **Composition over hardcoding sprawl** — precedence rules centralized  
- **Maintainability before cleverness**  

---

## Architecture overview

Seven C# / .NET 8 projects with one-way dependencies:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

| Project | Role |
|---------|------|
| **Models** | Catalog, snapshot, resolution types, explanations, SettingsQuery, NavigationBuilder |
| **Core** | Shared primitives |
| **Logging** | Audit logging |
| **KnowledgeBase** | In-memory knowledge store |
| **Validator** | Structural schema checks only |
| **Scanner** | Collectors, binders, **PolicyPrecedenceResolver** |
| **CLI** | Pipeline host, reports, **TuiHost** |

```
Collectors → InventorySnapshot → Binders → PrecedenceResolver
    → SettingsQuery / NavigationBuilder / SettingExplanation
    → CLI report or TUI
```

Models hold data. Scanner discovers and reasons. Presentation only renders.

Deep handbook: [`Status/Project_Documentation.md`](Status/Project_Documentation.md)

---

## Pipeline

1. **Discover** — identity, packages, services, tasks, ConsentStore, curated policy probes  
2. **Model** — static catalog with domains, descriptions, rationales  
3. **Validate** — structural completeness  
4. **Bind** — attach live values and configuration layers  
5. **Resolve** — effective value for known relationship pairs  
6. **Explain** — documentation-style cards  
7. **Navigate** — domain tree and detail views  
8. **Present** — CLI report or interactive TUI  

---

## Current capabilities (v0.7)

- Read-only inventory of key privacy and policy surfaces  
- ~65 curated, explained catalog settings across product domains  
- Per-layer observations (user preference, machine policy, alternate stores, …)  
- Effective-layer resolution for known overlaps with explicit reasons and conflicts  
- SettingsQuery and NavigationBuilder as a UI-independent application API  
- **Interactive TUI** (`--tui`): domains → categories → settings → explanation cards  
- CLI observation summary, high-impact **watch list** (not a score), conflict cards  
- Full domain report via `--full`  
- Neutral impact language; Observed vs Interpretation separation in cards  

### Product domains

ConsentStore · AppPrivacy · Telemetry · Windows Update · Defender · Search · Edge · Activity History · Cloud Content · Advertising · Location · Biometrics · Device · Speech · Firewall (reserved) · Other

---

## Current limitations

- Windows capability enumeration often returns empty without elevation; reported as **Unknown**, not “zero installed”  
- Catalog is curated, not a full Settings/ADMX mirror  
- Relationship graph is curated, not exhaustive  
- MDM / security baseline layers are ranked in the model but not fully collected  
- Firewall domain is reserved (no entries yet)  
- No GUI, baselines, historical snapshots, or comparison mode  
- No remediation by design in this phase  

---

## Read-only guarantee and safety philosophy

This build does **not**:

- write to the registry  
- change services, tasks, packages, capabilities, or policies  
- request elevation  
- remediate, optimize, or “fix” anything  
- emit a privacy or security score  
- send product telemetry  

Discovered values are treated as untrusted display text. Process arguments are fixed strings. That safety boundary is intentional: trust requires a tool people can run without fear.

---

## Screenshots

> **Placeholder:** Add terminal screenshots of (1) default observation summary, (2) conflict explanation card, (3) TUI domain list, (4) TUI setting detail card.

---

## Example workflow

1. Build and run the default report on a workstation.  
2. Note identity, inventory counts, and any layer conflicts.  
3. Open `--tui` and browse **Privacy — App permissions** → Camera.  
4. Read **Observed** values, then **Interpretation** (effective layer and reason).  
5. Follow **Related configuration** into AppPrivacy policy if present.  
6. Leave knowing *what Windows is doing* — without having changed it.  

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
| *(default)* | Observation summary + high-impact watch list + conflict cards |
| `--full` | Complete catalog dump by domain with What/Why |
| `--tui` | Interactive read-only explorer |
| `--help` | Help |

### TUI usage

| Key | Action |
|-----|--------|
| ↑ ↓ | Move selection / scroll detail card |
| Enter | Open |
| Esc / Backspace | Back |
| `/` | Search |
| Q | Quit |

---

## Impact labels (not a score)

Catalog entries carry static High / Medium / Low **impact** tags describing significance of the *topic*.  
“High-impact configured” means: a high-impact topic has an observed value worth reviewing.  
It does **not** mean the machine failed a test, and it does **not** mean “misconfigured.”

---

## Roadmap (priorities, not dates)

| Horizon | Focus |
|---------|--------|
| **v0.8** | Runtime verification of v0.7; Firewall catalog + collector; more relationships/explanations; optional domain filter |
| **v0.9** | Curated domain expansion; compare-only baselines; stronger provenance consistency |
| **v1.0 vision** | Stable, trustworthy read-only knowledge product for learning Windows configuration |
| **Far future** | Controlled reversible change — separate design, explicit authorization only |

Details: [`Status/AI_Handoff.md`](Status/AI_Handoff.md)

---

## Non-goals

- Bulk ADMX / gpedit clone  
- Automatic relationship inference  
- Privacy or security scoring  
- Silent system modification  
- Feature count for its own sake  

---

## Contribution philosophy

Contributions should improve **understanding**, **trust**, **safety**, **explainability**, or **maintainability**.

Before adding a setting:

1. Catalog entry with human name, domain, rationale, impact tag  
2. Discovery path  
3. Explanation quality  
4. Relationships where overlaps exist  
5. Read-only collector behavior  

Read the Status engineering journal before large changes:

- [`Status/AI_Handoff.md`](Status/AI_Handoff.md) — continuity and rules  
- [`Status/Current_Status.md`](Status/Current_Status.md) — live snapshot  
- [`Status/Project_Documentation.md`](Status/Project_Documentation.md) — architecture handbook  
- [`Status/Prototype_v0.1_Implementation_Map.md`](Status/Prototype_v0.1_Implementation_Map.md) — file map  

---

## Future direction

The long-term identity is a **publicly useful, safe, transparent Windows privacy intelligence tool** — an interactive handbook of how Windows configuration actually layers — not an XDR product and not a tweak utility.

---

## License / repository

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)

Prototype software. Run at your own discretion; the authors intend the current phase to be strictly non-mutating.
