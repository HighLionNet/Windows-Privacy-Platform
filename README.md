# Windows Privacy Platform

**Current milestone:** Prototype **v0.6 FINAL** — runtime verified on Windows 11 Pro 25H2 (build 26200)  
**Previous:** v0.5 (archived) → v0.4 → v0.3 → v0.2 → v0.1  
**Note:** Work briefly tracked as internal “v0.6.5” is included in this final v0.6 archive.

Local, declarative **privacy intelligence** platform for Windows.  
Philosophy: **Understand first. Change later.**

This is a **read-only decision-support** foundation — not a tweaking tool, not a security score, not a full gpedit clone.

## Version history

| Version | Summary |
|---------|---------|
| v0.1 | Seven-project skeleton, basic models |
| v0.2 | Early identity/package discovery |
| v0.3 | Services and scheduled-task inventory |
| v0.4 | Live multi-collector discovery skeleton |
| v0.5 | PolicyCollector, ManagedObjectCatalog, full categorized report (archived) |
| v0.6 core | Binder, ValidateAll, ObservationSummary, concise report + high-risk watch list |
| Step A | ProductDomain on every catalog entry; reports grouped by domain |
| **v0.6 FINAL** | Effective configuration foundation, SettingExplanation cards, SettingsQuery + Navigation models, split binders, CLI **decision cards** for layer conflicts |

## What works today

- Discovers identity, packages, services, tasks, privacy consent settings, and curated GPO/policy registry surfaces  
- Explains settings via ManagedObject catalog (name, description, risk tag, rationale, **product domain**)  
- Binds live values with **per-layer observations** (UserPreference / MachinePolicy / AlternatePolicyStore / …)  
- Resolves **effective configuration** for known overlaps (ConsentStore vs AppPrivacy; dual telemetry policy paths) with explicit **reason + conflict**  
- Builds **SettingExplanation** decision-card content (what it is, why it matters, user impact, guidance)  
- Exposes **SettingsQuery** and **NavigationBuilder** as a UI-independent application API for future TUI/GUI  
- Prints observation/risk **summary**, high-risk **watch list** by domain, and **conflict decision cards**  
- Optional full dump by domain: `--full`  
- Strictly read-only, non-interactive (no elevation, no writes, no prompts)  

## Product domains

ConsentStore · AppPrivacy · Telemetry · WindowsUpdate · Defender · Search · Edge · ActivityHistory · CloudContent · Advertising · Location · Biometrics · Device · Speech · Firewall (reserved) · Other

## Run

```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
```

| Flag | Behavior |
|------|----------|
| (default) | Summary + high-risk configured items (by domain) + conflict decision cards |
| `--full` | Complete catalog dump under `## Domain:` headers with What/Why lines |
| `--help` | Help |

## Safety (current phase)

No registry/service/task/package/policy changes · no elevation · no remediation · no interactive UI · no product telemetry · no auto-hardening

## Solution projects

| Project | Role |
|---------|------|
| Models | Data, catalog, domains, ConfigurationResolution, SettingExplanation, SettingsQuery, NavigationBuilder |
| Core | Shared primitives |
| Logging | AuditLogger |
| KnowledgeBase | In-memory store |
| Scanner | Collectors + domain binders + **PolicyPrecedenceResolver** |
| Validator | SchemaValidator (structural batch only) |
| CLI | Pipeline + presentation (summary, watch list, decision cards) |

Note: files under `bin/` are **build outputs** (compiled DLLs from these projects), not separate hand-written sources.

## Architecture snapshot

```
Collectors → InventorySnapshot → Binders → PrecedenceResolver
    → SettingsQuery / NavigationBuilder / SettingExplanation
    → CLI report (or future TUI)
```

Models hold data. Scanner discovers and reasons. CLI only renders.

## Risk output meaning

- Catalog H/M/L tags are **static definition labels**, not a live security grade  
- “High-risk configured” is a **watch list** of high-impact topics that have a real observed value  
- It is **not** an overall score and does **not** mean “misconfigured”  

## Future focus (see `Status/AI_Handoff.md` for full detail)

1. ~~Product domain taxonomy~~ **done**  
2. ~~Effective layer foundation + explanations + query/nav models~~ **done (final v0.6)**  
3. **Read-only TUI** over existing NavigationBuilder + SettingsQuery — recommended next product step  
4. Expand curated relationship pairs and richer explanation text  
5. CapabilityCollector reliability pass  
6. Expand discovery **domain by domain** (Firewall collector next among gaps) with human-readable policy names — not full gpedit import  
7. Optional compare-only baselines and a **transparent** risk-assessment feature  
8. Controlled change **design only** until authorised  

**Balance:** broad privacy/security coverage without a self-strangling all-ADMX model.

## Authoritative docs

- `Status/AI_Handoff.md` — continuity + detailed future steps and insertion points  
- `Status/Current_Status.md` — verified state  
- `Status/Prototype_v0.1_Implementation_Map.md` — file/pipeline map  
- `Status/Project_Documentation.md` — technical overview  
