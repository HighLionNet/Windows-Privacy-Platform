# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype **v0.6 FINAL** (includes former Step A + v0.6.5 architectural foundation)  
**Last Updated:** 2026-07-24  
**Audience:** Next AI implementer. Read this entire file before any code change. Do not assume undocumented behavior.

---

# PURPOSE

Primary continuity document. Every AI session must read this completely before any code change.

This document deliberately over-specifies **current behavior**, **verified runtime facts**, **product intent**, **architecture after the v0.6 foundation pass**, and **ordered future work** so a fresh chat can continue without the prior conversation.

The human is archiving this repository state as the official **v0.6 backup** immediately after these docs land.

---

# PROJECT VISION

Local, declarative **privacy intelligence** platform for Windows.

This is **not** primarily a CLI dump tool and **not** a tweaking/hardening utility.

Long-term product:

- Secure, local, **read-only decision-support** application
- User navigates privacy/security settings by **domain**
- Each setting presents a **human decision card**: what it is, why it matters, current effective value, which layer wins, related settings, user impact
- Future interface: **keyboard-navigable TUI first**, optional GUI later
- Data model must remain **UI-independent** (CLI / TUI / GUI consume the same query + navigation models)

Development order remains:

1. Discover  
2. Model  
3. Validate  
4. Report / explain  
5. Understand relationships and **effective configuration**  
6. Only then consider controlled, reversible remediation (elevation only when required)

Philosophy: **Understand first. Change later.**

---

# DEVELOPMENT MODEL

- ChatGPT — architecture, safety, continuity, review  
- Grok — direct GitHub implementation on `HighLionNet/Windows-Privacy-Platform` (main)  
- Human — local build + runtime verification, direction approval, archives  
- Local path: `C:\Windows Privacy Platform`

---

# MANDATORY RULES

1. Every change must leave the solution compiling (0 errors; avoid new warnings).  
2. Runtime check on Windows after every meaningful build.  
3. Distinguish **Implemented** vs **Planned** in docs and code comments.  
4. Never redesign the seven-project architecture without explicit approval.  
5. Update Status documents at end of session **after** verification.  
6. **No write paths, no elevation, no interactive UI framework** until authorised.  
7. Prefer small, reviewable changes. Fail-soft collectors (never crash the pipeline).  
8. **Models** project stays free of registry logic and business calculations (data + pure composition only).  
9. **Scanner** discovers and binds; **PolicyPrecedenceResolver** is the only place for layer precedence rules.  
10. **CLI** is presentation-only — it must not invent meaning.

---

# SAFETY RULES (ABSOLUTE FOR CURRENT PHASE)

Strictly read-only. Prohibited:

- Registry writes  
- Service / task / package / capability / policy / firewall changes  
- Elevation / UAC  
- Remediation, rollback, recovery  
- Network calls for product telemetry  
- Interactive prompts that block automation  
- “Fix all” / auto-hardening  

Future controlled changes (only when separately authorised) must: prompt for elevation only when needed; warn on sensitive settings; prefer reversible actions; never silently modify the system.

---

# ARCHITECTURE (IMPLEMENTED — DO NOT BREAK)

Seven projects, explicit composition, no DI container:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

- Scanner + CLI target `net8.0-windows`  
- Others target `net8.0`  
- `bin/` and `obj/` are **build outputs**. Source of truth is `Source/**/*.cs`.

### Layer responsibilities (v0.6 final)

| Project | Responsibility |
|---------|----------------|
| **Models** | Data structures only: ManagedObject, catalog, InventorySnapshot domain sections, ConfigurationObservation, ConfigurationResolution, EffectiveState, SettingExplanation, SettingRelationship, SettingsQuery, NavigationNode / SettingDetailView |
| **Core** | OperationResult, PathConstants, PlatformException |
| **Logging** | IAuditLogger / AuditLogger |
| **KnowledgeBase** | In-memory store of catalog entries |
| **Validator** | Structural SchemaValidator (ObjectId, name, description, type, schema version) — **not** a security score |
| **Scanner** | Collectors (discover); domain binders (attach observations); RelationshipBinder; **PolicyPrecedenceResolver** (effective-config reasoning) |
| **CLI** | Flags, pipeline orchestration, **presentation only** (summary, high-risk list, conflict decision cards, `--full`) |

### Runtime pipeline (v0.6 final — actual order)

1. **CLI** (`Program.cs`) — non-interactive flags only (`--full`, `--help`).  
2. **InventoryScanner** runs collectors sequentially into one **InventorySnapshot** (domain-organized sections with backward-compat accessors).  
3. **Collectors** (all read-only, fail-soft):
   - `WindowsIdentityCollector` — HKLM NT\CurrentVersion  
   - `CapabilityCollector` — PowerShell Get-WindowsCapability; DISM `/English` fallback (**often returns 0**)  
   - `PackageCollector` — PowerShell Get-AppxPackage (current user)  
   - `ServiceCollector` — ServiceController.GetServices()  
   - `ScheduledTaskCollector` — schtasks CSV  
   - `PrivacyCollector` — HKCU ConsentStore + related HKCU privacy prefs  
   - `PolicyCollector` — curated HKLM/HKCU policy/preference probes; missing → `"Not configured"`  
4. **ManagedObjectCatalog** — static explained settings; every entry has exactly one `ProductDomain`.  
5. **InventoryStateBinder** (orchestrator) → domain binders:
   - `PrivacyBinder` / `PolicyBinder` (`IStateBinder`) attach `CurrentState` + `ConfigurationObservation` layers  
   - `RelationshipBinder` wires known pairs and calls **PolicyPrecedenceResolver**  
6. **InMemoryKnowledgeBaseRepository** stores catalog entries.  
7. **SchemaValidator.ValidateAll** — structural rules only.  
8. **SettingsQuery** + **NavigationBuilder** available as the application API for reports (and future TUI).  
9. **CLI report**:
   - Default: Observation & Risk Summary + High-Risk Configured Items + **Effective-Layer Conflict decision cards**  
   - `--full`: complete dump under `## Domain:` with What/Why explanation lines  
10. Safety confirmation lines.

**There is no firewall collector.** `ProductDomain.Firewall` is reserved.

---

# PRODUCT DOMAIN TAXONOMY (IMPLEMENTED)

`ProductDomain` enum on every `ManagedObject`. Primary navigation axis.

| Domain | Role |
|--------|------|
| ConsentStore | Per-user app capability permissions (HKCU ConsentStore) |
| AppPrivacy | Machine AppPrivacy GPO overrides (LetApps*) |
| Telemetry | Diagnostic data level and related |
| WindowsUpdate | AU / WU access / Delivery Optimization |
| Defender | Antivirus / MAPS / PUA policies |
| Search | Cortana / web search / connected search |
| Edge | Edge tracking, metrics, password manager, suggestions |
| ActivityHistory | Timeline / activity feed / upload |
| CloudContent | Consumer features, Spotlight, soft landing, content delivery |
| Advertising | Advertising ID (user + GPO) |
| Location | Machine location kill-switch |
| Biometrics | Windows biometric framework |
| Device | Find My Device and related |
| Speech | Online speech recognition |
| Firewall | **Reserved** — no catalog entries yet |
| Other | Catch-all |

All **65** catalog ObjectIds have exactly one primary domain. `SubCategory` is secondary.

---

# EFFECTIVE CONFIGURATION (IMPLEMENTED — foundation)

### Types (Models)

- `ConfigurationLayer`: Unknown, UserPreference, ApplicationPreference, AlternatePolicyStore, MachinePolicy, MDMPolicy, SecurityBaseline  
- `ConfigurationObservation`: one raw value from one layer  
- `ConfigurationResolution`: EffectiveValue, EffectiveSource, Confidence, **ResolutionReason**, HasConflict, RawObservations  
- `EffectiveState`: compatibility projection used by older consumers  
- `SettingRelationship` + `RelationshipKind`: Related, Overrides, OverriddenBy, ConflictsWith, DependsOn, Requires, Affects, SameFeatureAlternatePath  

### PolicyPrecedenceResolver (Scanner)

**Single place** for precedence logic. Never silently guesses.

- Layer rank order (highest wins when both configured): SecurityBaseline > MDMPolicy > MachinePolicy > AlternatePolicyStore > ApplicationPreference > UserPreference  
- `ResolveConsentVsAppPrivacy`: AppPrivacy codes 0=user, 1=force allow, 2=force deny  
- `ResolveAlternateMachinePolicyPaths`: prefers SOFTWARE\Policies over CurrentVersion\Policies when both differ; marks conflict  
- `ResolveByLayerRank`: generic fallback; ties → Unknown + conflict  

### Known wired pairs (RelationshipBinder)

ConsentStore ↔ AppPrivacy:

- location ↔ policy.appprivacy.location  
- webcam ↔ policy.appprivacy.camera  
- microphone ↔ policy.appprivacy.microphone  
- broadFileSystemAccess ↔ policy.appprivacy.filesystem  

Alternate paths:

- policy.telemetry.allowtelemetry ↔ policy.telemetry.allowtelemetry.currentversion  

Dual telemetry values on the test machine (0 vs 1) remain a **live conflict test case**.

---

# EXPLANATION + NAVIGATION FOUNDATION (IMPLEMENTED)

### SettingExplanation + SettingExplanationFactory (Models)

Human decision-card fields derived from catalog definition (no registry logic):

- WhatIsIt, WhyItMatters, UserImpact, EnterpriseImpact, TypicalUseCases, DecisionGuidance, RelatedApplications  
- DomainPath, RiskSummary  

Presentation layers format this object; they do not invent meaning.

### SettingsQuery (Models) — future API surface

Read-only methods intended for CLI / TUI / GUI / possible API:

- `GetByDomain`, `GetById`, `GetRelatedSettings`, `GetRelationshipEdges`  
- `GetConflicts`, `GetMachineControlledSettings`, `GetSettingsNeedingReview`  
- `Search` (plain-text filter, max 200 chars — never execute search text)  
- `GetExplanation` / `TryGetExplanation`  

### NavigationBuilder + SettingDetailView (Models)

- Domain → feature → setting tree with conflict/risk counts on nodes  
- Detail card combines explanation + resolution + layers + related edges  
- Display sanitization strips control characters from untrusted discovered strings  
- **No Terminal.Gui or other UI dependency yet**

### CLI decision cards (default mode)

For each unique effective-layer conflict, prints a card:

Domain, Risk, What is this, Why it matters, Current raw, Effective value, Source + confidence, Why it wins, Observed layers, Related settings, Related apps, User impact, Guidance.

---

# VERIFIED RUNTIME (v0.6 on Windows 11 Pro 25H2 build 26200)

**Build:** `dotnet build -c Release` — 0 Warning(s), 0 Error(s) after final CLI fix (2026-07-24).

Representative inventory (counts may vary slightly by host state):

```
Identity : Windows 11 Pro | 25H2 | Build 26200
Capabilities : 0 | Packages : ~165 | Services : ~303 | Tasks : ~247
Privacy settings : ~30 | Policy probes : ~79 (configured: ~45)
KnowledgeBase: 65 catalog entries
Validator batch: passed=65, failed=0
Observed / not observed: ~63 / ~2
Risk (H/M/L catalog) : 30 / 26 / 9
Layer conflicts : present when dual telemetry / force policy pairs disagree
Nav domains : enumerated from catalog
```

Pipeline ~2–3 seconds. Safety confirmation present. No elevation, no writes.

### Critical interpretation of “risk” output (do not misread)

- **Not an overall security score.** No 0–100 grade. No machine pass/fail.  
- **Risk (H/M/L catalog)** = static tags on catalog *definitions*.  
- **High-Risk Configured Items** = high-impact topics that have a real observed value — a **watch list** for human review.  
- High-risk does **not** mean “misconfigured.” Location=Deny can still appear under high-risk *topic*.  
- Effective resolution explains **who wins and why**; it still does not auto-judge “good vs bad” policy for the user.

---

# VERSION HISTORY (VERBOSE — FULL)

| Version | What shipped | Status |
|---------|--------------|--------|
| **v0.1** | Initial project skeleton, seven-project solution layout, basic models | Historical |
| **v0.2** | Early collector experiments, identity/package probes | Historical |
| **v0.3** | Expanded inventory surfaces; services/tasks paths | Historical |
| **v0.4** | Live discovery skeleton — multi-collector InventoryScanner → InventorySnapshot | Historical |
| **v0.5** | PolicyCollector (curated probes), ManagedObjectCatalog with human names/rationales, full categorized report | Archived by human |
| **v0.6 core** | InventoryStateBinder (`CurrentState`), SchemaValidator.ValidateAll, ObservationSummary, concise default report + high-risk watch list, safety confirmation | Runtime verified on Win11 Pro 25H2 |
| **Step A** | `ProductDomain` enum + property; all 65 catalog entries assigned; ObservedItem + binder + CLI report group by domain then SubCategory | Verified 2026-07-24 |
| **Foundation pass (documented as interim “v0.6.5”, now folded into final v0.6)** | Domain-organized InventorySnapshot; split binders (`IStateBinder`, PrivacyBinder, PolicyBinder, RelationshipBinder); ConfigurationLayer / ConfigurationObservation / EffectiveState / ConfigurationResolution; PolicyPrecedenceResolver; SettingExplanation; SettingsQuery application API; NavigationBuilder + SettingDetailView; CLI conflict decision cards; display sanitization; search length cap | **Build verified 2026-07-24; this is FINAL v0.6** |

**There is no separate shipping version called v0.6.5.** That label was an internal implementation milestone. The archive the human is taking is **Prototype v0.6 final**.

---

# PRODUCT INTENT (OWNER DIRECTION)

## What the product is becoming

A Windows **privacy understanding** tool:

- Navigate by domain (Privacy, Update, Defender, …)  
- Open a setting → see explanation card, effective state, layers, relationships  
- Never force the user to start from raw registry paths  
- Technical discovery engine exists to **serve human understanding**

## What the product is not

- Not a gpedit clone  
- Not a full ADMX importer  
- Not a security score product  
- Not an auto-hardener  
- Not a remediation tool in this phase  

## Coverage vs complexity (hard constraint)

- **Do not** import all of gpedit/ADMX in one version.  
- **Do** grow **domain by domain** with curated high-value settings + human labels.  
- Every new setting needs: stable ObjectId, human name, description, rationale, risk tag, discovery path, **ProductDomain**, subcategory, and ideally relationship edges when overlap exists.  
- Collectors stay fail-soft.

## UI direction (future)

- Prefer **TUI** (keyboard: arrows, Enter, Back, search/filter, domain browse, detail view) before full GUI  
- Consume existing `NavigationBuilder` + `SettingsQuery` + `SettingDetailView` — do **not** invent a parallel data model  
- No Terminal.Gui / GUI framework until authorised

---

# FUTURE STEPS (ORDERED — WHERE TO INSERT)

Use this sequence. Each step must leave the solution build-green and read-only.

## Completed in final v0.6

- Step A — Product domain taxonomy  
- Effective configuration foundation (layers, resolution, precedence resolver, known pairs)  
- Explanation card model  
- Query / navigation application API  
- Binder split + snapshot domain organization  
- CLI decision cards for conflicts  

## Next recommended work (v0.7 starting points)

### 1. Thin read-only TUI host (highest product leverage)

**Insert at:** new optional project or CLI mode that only **consumes** Models navigation/query types.  
**Work:** keyboard navigation over `NavigationBuilder.BuildDomainTree`; detail pane from `SettingDetailView`.  
**Do not** add writes, elevation, or a second catalog model.

### 2. Expand known relationship pairs + editorial explanation overrides

**Insert at:** RelationshipBinder pair tables; optional richer text in SettingExplanationFactory for top settings.  
**Work:** more Consent↔AppPrivacy pairs; advertising ID user vs GPO; location machine kill-switch vs ConsentStore.  
Still curated — not ADMX dump.

### 3. CapabilityCollector reliability (Step C)

**Insert at:** `CapabilityCollector` only.  
**Work:** diagnose 0 results on 25H2; keep read-only; fail-soft.

### 4. Domain-scoped discovery expansion (Firewall first among gaps)

**Insert at:** new `IInventoryCollector` + matching catalog entries with ProductDomain.Firewall + relationships.  
**Work:** read-only netsh/registry/WMI patterns; document elevation limits honestly.  
**Rule:** catalog explanations land with the collector.

### 5. Optional `--domain=` CLI filter

**Insert at:** CLI flags only; filter via SettingsQuery.

### 6. Observation vs baseline (compare-only)

**Insert at:** desired-state fields already exist on ManagedObject shape; compare-only report section.  
**No enforcement.**

### 7. Optional formal RiskAssessment feature

Separate pure computation with documented rules — **must not** reuse “High-Risk Configured count” as a fake score.

### 8. Controlled change design only (docs first)

Elevation-on-demand, per-setting warnings, reversibility. **No implementation** until human explicitly requests.

---

# CODE REVIEW NOTES (foundation pass, 2026-07-24)

**Fixed during pass:**

- Precedence logic centralized (no longer only inline in RelationshipBinder)  
- `GetSettingsNeedingReview` operator-precedence bug  
- CLI full-report `??` / pattern-match compile errors (CS0019 / CS0165)  
- Search term length capped; display strings sanitized  
- Dictionary builds tolerant of duplicate ObjectIds via GroupBy  

**Security / safety:**

- All registry opens use `writable: false`  
- Process launches use fixed argument strings — no user-controlled injection  
- No elevation, no writes, no network product telemetry  
- Untrusted discovered values treated as display text only  

**Non-blocking residual notes:**

- Default enum value for unset `ProductDomain` is `ConsentStore` (0); catalog authors must set domain explicitly (all 65 do)  
- CapabilityCollector still returns 0 on test host  
- Only a subset of possible Windows overlaps are wired as relationships  
- Explanation text is mostly catalog-derived + light inference, not full editorial content for every setting  
- No TUI host yet  

---

# KNOWN GAPS (final v0.6)

| Gap | Severity | Notes |
|-----|----------|-------|
| Capabilities = 0 | Medium | Collector issue on test host |
| No firewall surface | Medium | Domain reserved; collector not started |
| Limited relationship graph | Medium | Only known pairs wired; expand curated |
| MDM / SecurityBaseline ranks unused | Low | Enum ranks ready; no collectors yet |
| Catalog curated (~65), not full GPO | By design | Expand by domain |
| Risk list ≠ security score | Documentation | Do not market as score |
| No baselines / recommended sets | Planned optional | Compare-only first |
| No TUI/GUI host | Planned | Models ready |
| No remediation | Deferred | Until authorised |

---

# BUILD & RUNTIME POLICY

```
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --full
dotnet run -c Release -- --help
```

Confirm: inventory counts, validator 65/0, domain-grouped high-risk lines, conflict decision cards (when conflicts exist), safety confirmation.

---

# HOW THE APP WORKS (MEDIUM-LEVEL — FOR NEXT CHAT)

1. **Discover** — collectors read identity, packages, services, tasks, ConsentStore, and curated policy keys into a domain-organized snapshot.  
2. **Define** — static catalog explains each setting (name, domain, risk, description, rationale).  
3. **Bind** — privacy/policy binders attach live values and tag the configuration layer.  
4. **Relate + resolve** — RelationshipBinder links known pairs; PolicyPrecedenceResolver computes effective value, source, confidence, and a human-readable reason (or Unknown when unsafe to guess).  
5. **Query** — SettingsQuery answers domains, conflicts, related settings, “needs review,” explanations.  
6. **Navigate (data)** — NavigationBuilder builds domain trees and detail cards for any future UI.  
7. **Present** — CLI prints summary, high-risk watch list, and decision cards for conflicts.  

Discovery exists to support **understanding**, not silent system change.

---

# END OF DOCUMENT
