# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.6 + Step A (ProductDomain taxonomy) — **hardware verified**  
**Last Updated:** 2026-07-24  
**Audience:** Next AI implementer. Read this entire file before any code change. Do not assume undocumented behavior.

---

# PURPOSE

Primary continuity document. Every AI session must read this completely before any code change.

This document deliberately over-specifies **current behavior**, **verified runtime facts**, **product intent**, and **ordered future work** so a fresh chat can continue without the prior conversation.

---

# PROJECT VISION

Local, declarative privacy intelligence platform for Windows.

Long-term goal: a centralized, **human-navigable** view of privacy and security-related Windows settings — including GPO/policy surfaces with **clear names and explanations** (easier than raw gpedit / ADMX names) — so a semi-technical user can **understand** configuration before any change is offered.

Development order is fixed and non-negotiable for the current phase:

1. Discover  
2. Model  
3. Validate  
4. Report  
5. Understand relationships (including GPO vs UI effective layers)  
6. Only then consider controlled, reversible remediation (elevation only when required)

Philosophy: **Understand first. Change later.**

---

# DEVELOPMENT MODEL

- ChatGPT — architecture, safety, continuity, review  
- Grok — direct GitHub implementation on `HighLionNet/Windows-Privacy-Platform` (main)  
- Human — local build + runtime verification, direction approval  
- Local path: `C:\Windows Privacy Platform`

---

# MANDATORY RULES

1. Every change must leave the solution compiling (0 errors preferred; no new warnings as a goal).  
2. Runtime check on Windows after every meaningful build.  
3. Distinguish **Implemented** vs **Planned** in docs and code comments.  
4. Never redesign the seven-project architecture without explicit approval.  
5. Update Status documents at end of session.  
6. **No write paths, no elevation, no interactive UI** until the model/report/relationship layer is solid.  
7. Prefer small, reviewable changes. Fail-soft collectors (never crash the pipeline).  
8. Models project stays free of business logic (data + static catalog only).

---

# SAFETY RULES (ABSOLUTE FOR CURRENT PHASE)

Strictly read-only. Prohibited:

- Registry writes  
- Service / task / package / capability / policy / firewall changes  
- Elevation / UAC  
- Remediation, rollback, recovery  
- Network calls for product telemetry  
- Interactive prompts that block automation  

Future controlled changes (only when separately authorised) must: prompt for elevation only when needed; warn on sensitive settings; prefer reversible actions; never silently modify the system.

---

# ARCHITECTURE (IMPLEMENTED — DO NOT BREAK)

Seven projects, explicit composition, no DI container:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

- Scanner + CLI target `net8.0-windows`  
- Others target `net8.0`  
- `bin/` and `obj/` are **build outputs** (compiled DLLs/EXE from our `.cs` projects + NuGet deps such as `System.ServiceProcess.ServiceController`). Source of truth is `Source/**/*.cs`. Do not treat `bin` DLLs as hand-authored source.

### Runtime pipeline (v0.6 + Step A actual order)

1. **CLI** (`WindowsPrivacyPlatform.CLI/Program.cs`) starts; parses non-interactive flags only (`--full`, `--help`).  
2. **InventoryScanner** runs collectors in sequence into one `InventorySnapshot`.  
3. **Collectors** (all read-only, fail-soft):
   - `WindowsIdentityCollector` — HKLM NT\CurrentVersion  
   - `CapabilityCollector` — PowerShell Get-WindowsCapability; DISM `/English` fallback (**often returns 0** on test machine)  
   - `PackageCollector` — PowerShell Get-AppxPackage (current user)  
   - `ServiceCollector` — ServiceController.GetServices()  
   - `ScheduledTaskCollector` — schtasks CSV  
   - `PrivacyCollector` — HKCU ConsentStore + related HKCU privacy prefs  
   - `PolicyCollector` — curated HKLM/HKCU policy/preference probes; missing → `"Not configured"`  
4. **ManagedObjectCatalog** (Models) — static explained settings; every entry has exactly one `ProductDomain`.  
5. **InventoryStateBinder** (Scanner) — sets each catalog item’s `CurrentState` from snapshot; builds `ObservationSummary` (includes `ProductDomain` on observed items).  
6. **InMemoryKnowledgeBaseRepository** — stores catalog entries.  
7. **SchemaValidator.ValidateAll** — structural rules (ObjectId, ObjectName, Description, ObjectType, SchemaVersion).  
8. **CLI report** — default: Observation & Risk Summary + High-Risk Configured Items **grouped by ProductDomain then SubCategory**; `--full`: complete dump under `## Domain: …` headers.  
9. Safety confirmation lines.

**There is no firewall collector.** `ProductDomain.Firewall` is reserved. Firewall discovery is planned, not implemented.

---

# PRODUCT DOMAIN TAXONOMY (IMPLEMENTED — Step A)

`ProductDomain` enum on `ManagedObject` (Models). Primary navigation axis for reports.

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

Every one of the 65 catalog ObjectIds is assigned exactly one primary domain. `SubCategory` remains for finer grain within a domain.

---

# VERIFIED RUNTIME (v0.6 + Step A on Windows 11 Pro 25H2 build 26200)

**Build:** `dotnet build -c Release` — 0 Warning(s), 0 Error(s) (2026-07-24).

```
Identity : Windows 11 Pro | 25H2 | Build 26200
Capabilities : 0 | Packages : 165 | Services : 303 | Tasks : 247
Privacy settings : 30 | Policy probes : 79 (configured: 45)
KnowledgeBase: 65 catalog entries
Validator batch: passed=65, failed=0
Observed / not observed: 63 / 2
Policy configured / not: 45 / 34
Risk (H/M/L catalog) : 30 / 26 / 9
Privacy Allow/Deny/Prompt: 18 / 2 / 0
High-risk configured : 25 | Medium-risk configured : 22
```

High-risk list and `--full` report show domain prefixes (e.g. `[ConsentStore/ConsentStore]`, `[AppPrivacy/AppPrivacy]`, `## Domain: CloudContent`). Pipeline ~2–3 seconds. Safety confirmation present. No elevation, no writes.

### Critical interpretation of the “risk” output (do not misread)

- **Not an overall security score.** There is no 0–100 grade and no machine pass/fail.  
- **Risk (H/M/L catalog)** = static tags on catalog *definitions*, not a live score of the PC.  
- **High-Risk Configured Items** = catalog entries tagged High that have a real observed value (not “Not configured”). It is a **watch list** for human review.  
- High-risk does **not** mean “misconfigured.” Example: Location=Deny still appears under high-risk *topic* because location is high-impact.  
- Numeric GPO values (0/1/2) are reported raw; the app does **not** yet interpret “0 is good/bad” per setting.

### Verified overlap example (motivates Step B)

On the test machine, telemetry appeared under **two** paths with **different** values:

- `policy.telemetry.allowtelemetry` → `0 (HKLM)` (SOFTWARE\Policies path)  
- `policy.telemetry.allowtelemetry.currentversion` → `1 (HKLM)` (CurrentVersion\Policies path)  

AppPrivacy GPO values (e.g. camera=`2`) and ConsentStore Allow/Deny both appear for related capabilities. Discovery is correct; **effective-setting resolution is not implemented.**

---

# VERSION HISTORY (VERBOSE)

| Version | What shipped | Status |
|---------|--------------|--------|
| **v0.1** | Initial project skeleton, seven-project solution layout, basic models | Historical |
| **v0.2** | Early collector experiments, identity/package probes | Historical |
| **v0.3** | Expanded inventory surfaces; services/tasks paths | Historical |
| **v0.4** | Live discovery skeleton — multi-collector InventoryScanner into InventorySnapshot | Historical |
| **v0.5** | PolicyCollector (curated probes), ManagedObjectCatalog with human names/rationales, full categorized report | Archived by human |
| **v0.6** | InventoryStateBinder (`CurrentState`), SchemaValidator.ValidateAll, ObservationSummary, concise default report + high-risk watch list, safety confirmation | **Runtime verified** on Win11 Pro 25H2 (build 26200) |
| **Step A (on v0.6)** | `ProductDomain` enum + property; all 65 catalog entries assigned; ObservedItem + binder + CLI report group by domain then SubCategory | **Build + runtime verified** 2026-07-24 |

Next code milestone is **Step B** (effective layers), still under the v0.6 product line until a version bump is explicitly requested.

---

# PRODUCT INTENT (OWNER DIRECTION — BIND NEXT WORK TO THIS)

## Navigation / taxonomy

**Step A delivered** the domain axis. Settings are navigable by product domain in both default and `--full` reports. Continue to keep domain assignment mandatory for every new catalog entry.

## Overlap and override (must design before expanding blindly)

Windows layers can override each other:

1. Per-user UI / ConsentStore (HKCU)  
2. Machine AppPrivacy and other GPOs (often HKLM Policies)  
3. Alternate policy stores (e.g. CurrentVersion\Policies vs SOFTWARE\Policies)  
4. Future: MDM/Intune, security baselines  

**Requirement (Step B):** when two surfaces touch the same capability (e.g. camera), the model must show each layer’s raw value, which layer is **effective** (or conflict/unknown), and that GPO can shadow Settings UI.

Without this, expanding the catalog creates duplicate “high risk” rows and confuses users.

## Coverage vs complexity (hard constraint)

- **Do not** import all of gpedit/ADMX in one version.  
- **Do** grow **domain by domain** with curated high-value settings + human labels.  
- Every new setting needs: stable ObjectId, human name, description, rationale, risk tag, discovery path, **ProductDomain**, subcategory.  
- Collectors stay fail-soft; missing keys → Not configured / skip, never throw out of Collect.

## Optional future product features (approved as ideas, not implemented)

- Overall **risk assessment** feature (explicitly designed, not the current H/M/L catalog counts)  
- **Recommended settings sets** / baselines — observation vs desired state, compare-only first  
- Still **no remediation** until authorised

## Explicitly not next

- Interactive TUI/GUI  
- Writes / elevation helpers  
- “Score” that pretends current High-Risk list is a security grade  

---

# FUTURE STEPS (ORDERED — WHERE TO INSERT IN THE PIPELINE)

Use this sequence. Each step should leave the solution build-green and read-only.

## Step A — Domain taxonomy on the model — **DONE**

**Completed 2026-07-24.** `ProductDomain` enum; field on `ManagedObject`; all catalog entries assigned; report groups by domain then SubCategory. Verified build + runtime on 25H2.

## Step B — Effective layer metadata (Models + Binder) — **NEXT**

**Insert at:** Models (layer enum / fields on ManagedObject or a small related type); **InventoryStateBinder** (compute effective state); CLI report (show layers).

**Work:**

- Represent layers: `UserPreference`, `MachinePolicy`, `AlternatePolicyStore`, etc.  
- For linked settings (e.g. camera ConsentStore + AppPrivacy LetAppsAccessCamera), store relationship ids (`RelatedFeature` / `ConflictsWith` already exist on ManagedObject — use them).  
- Binder outputs both raw states and a best-effort `EffectiveState` + `EffectiveSource` when determinable; otherwise `Conflict` / `Unknown`.  
- Report must explain conflicts, not hide them.  
- Concrete test case already on the machine: dual AllowTelemetry paths (0 vs 1).

**Why before mass GPO expansion:** prevents double-counting and false “high risk” noise.

## Step C — CapabilityCollector fix (Scanner)

**Insert at:** `CapabilityCollector` only.

**Work:** diagnose why count is 0 on 25H2 (elevation, PowerShell errors, parsing). Keep read-only. Log stderr on failure at Debug level if useful. Do not block other work if still 0 after best effort.

## Step D — Domain-scoped discovery expansion (Scanner + Catalog together)

**Insert at:** new or extended collectors in Scanner; matching catalog entries in Models; wire collector in CLI list.

**Preferred domain order (suggested):**

1. **Firewall** — new read-only collector (netsh / registry / WMI patterns that work non-elevated where possible; document elevation limits honestly).  
2. **Defender** — deepen beyond current policy probes if safe read-only APIs exist.  
3. **Windows Update** — deepen schedule/channel related readable values.  
4. **App privacy / ConsentStore** — already strong; keep aligned with AppPrivacy GPO via relationships.  
5. **Telemetry** — already partially present; resolve dual-path effective value (depends on Step B).  
6. **Edge / Search / Activity / Cloud** — expand curated sets, not entire ADMX.

**Rule:** no domain lands without catalog explanations (name, description, rationale, risk, ProductDomain).

## Step E — Report UX without interactivity (CLI)

**Partially done by Step A** (domain grouping). Remaining:

- Optional later flag `--domain=Firewall` (or similar).  
- Conflict/effective-layer presentation once Step B lands.  
- Still no prompts.

## Step F — Observation vs baseline (Models + Validator or CLI compare-only)

**Insert at:** after bind; optional desired-state fields on ManagedObject (`DesiredState` already exists on the type); compare-only report section.

**Work:** define one optional baseline profile as data (not enforcement). Report “matches baseline / differs / not observed.” Do **not** auto-remediate.

## Step G — Optional formal risk assessment feature

**Insert at:** new pure computation after bind (CLI or thin helper); separate from structural SchemaValidator.

**Work:** name it clearly (`RiskAssessment` / `ExposureSummary`); document methodology; must not reuse “High-Risk Configured count” as a fake score; prefer transparent rule lists.

## Step H — Relationships graph (after domains + layers)

**Insert at:** catalog metadata + report section “Related settings.” Use existing `Requires`, `ConflictsWith`, `RelatedFeature`.

## Step I — Controlled change design only (docs first)

**Insert at:** Status design note only until authorised. Elevation-on-demand, per-setting warnings, reversibility. **No implementation** until human explicitly requests.

## Step J — Terminal UI (last among UX)

Only after domains, effective layers, and report are understandable in CLI form.

---

# CODE REVIEW NOTES (Step A session, 2026-07-24)

Reviewed Models (Enums, ManagedObject, Catalog, ObservationSummary), Scanner binder + collectors, CLI report, Validator rules.

**No blocking bugs.** Build 0/0; runtime matches prior v0.6 counts with domain labels.

**Security / safety:**

- All registry opens use `writable: false`.  
- Process launches (PowerShell, DISM, schtasks) use fixed argument strings — no user-controlled injection surface.  
- No elevation, no writes, no network product telemetry, no deserialization of untrusted input.  
- Console output is display-only of observed values.

**Non-blocking observations (do not block Step B):**

- High-risk lines can read `[ConsentStore/ConsentStore]` when domain equals SubCategory — cosmetic.  
- Default enum value for unset `ProductDomain` is `ConsentStore` (0); future catalog authors must set the field explicitly (all current entries do). Optional later: SchemaValidator rule requiring non-default domain, or a dedicated sentinel.  
- `NamesLooselyMatch` in binder uses substring Contains — pre-existing; low risk with curated ObjectIds.  
- PolicyCollector has more probes (79) than policy catalog rows (~39) — intentional curated model.  
- CapabilityCollector still returns 0 on this host — Step C.  
- Dual telemetry paths still both listed — Step B.

---

# KNOWN GAPS (v0.6 + Step A)

| Gap | Severity | Notes |
|-----|----------|-------|
| Capabilities = 0 | Medium | Collector issue on test host (Step C) |
| No firewall surface | Medium | Domain reserved; collector not started (Step D) |
| No effective-layer resolution | High for product clarity | Dual telemetry paths already visible (Step B) |
| Catalog is curated (~65), not full GPO | By design | Expand by domain |
| Risk list ≠ security score | Documentation | Do not market as score |
| No baselines / recommended sets | Planned optional | Compare-only first (Step F) |

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

Confirm: inventory counts, policy configured count, validator batch, observation summary, domain-grouped high-risk lines, safety confirmation.

---

# END OF DOCUMENT
