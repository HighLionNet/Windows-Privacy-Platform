# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.6 (Bind + Validate + Risk Summary) — **hardware verified**  
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

### Runtime pipeline (v0.6 actual order)

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
4. **ManagedObjectCatalog** (Models) — static explained settings (privacy + policy batches; `All` combined).  
5. **InventoryStateBinder** (Scanner) — sets each catalog item’s `CurrentState` from snapshot; builds `ObservationSummary`.  
6. **InMemoryKnowledgeBaseRepository** — stores catalog entries.  
7. **SchemaValidator.ValidateAll** — structural rules (ObjectId, ObjectName, Description, ObjectType, SchemaVersion).  
8. **CLI report** — default: Observation & Risk Summary + High-Risk Configured Items; `--full`: complete categorized dump.  
9. Safety confirmation lines.

**There is no firewall collector.** Firewall is planned, not implemented.

---

# VERIFIED RUNTIME (v0.6 on Windows 11 Pro 25H2 build 26200)

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

Pipeline ~2 seconds. Safety confirmation present. No elevation, no writes.

### Critical interpretation of the “risk” output (do not misread)

- **Not an overall security score.** There is no 0–100 grade and no machine pass/fail.  
- **Risk (H/M/L catalog)** = static tags on catalog *definitions*, not a live score of the PC.  
- **High-Risk Configured Items** = catalog entries tagged High that have a real observed value (not “Not configured”). It is a **watch list** for human review.  
- High-risk does **not** mean “misconfigured.” Example: Location=Deny still appears under high-risk *topic* because location is high-impact.  
- Numeric GPO values (0/1/2) are reported raw; the app does **not** yet interpret “0 is good/bad” per setting.

### Verified overlap example (motivates future effective-layer work)

On the test machine, telemetry appeared under **two** paths with **different** values (GPO Policies path vs CurrentVersion Policies path). AppPrivacy GPO values and ConsentStore Allow/Deny can both appear for related capabilities. Discovery is correct; **effective-setting resolution is not implemented.**

---

# PRODUCT INTENT (OWNER DIRECTION — BIND NEXT WORK TO THIS)

## Navigation / taxonomy (target UX shape)

Settings should eventually be navigable by **product domains**, not only raw ADMX-style names:

- App permissions / ConsentStore / AppPrivacy  
- Firewall (not started)  
- Defender  
- Windows Update  
- Telemetry / diagnostic data  
- Search / Cortana  
- Edge  
- Activity History / Timeline  
- Cloud content / consumer experiences  
- A dedicated **GPO / Policy** area: large, clearly labeled, human names + short rationales — **easier than gpedit** is a primary product value  

Group by **program/feature domain first**, then by policy vs user preference within the domain.

## Overlap and override (must design before expanding blindly)

Windows layers can override each other:

1. Per-user UI / ConsentStore (HKCU)  
2. Machine AppPrivacy and other GPOs (often HKLM Policies)  
3. Alternate policy stores (e.g. CurrentVersion\Policies vs SOFTWARE\Policies)  
4. Future: MDM/Intune, security baselines  

**Requirement:** when two surfaces touch the same capability (e.g. camera), the model must eventually show:

- Each layer’s raw value  
- Which layer is **effective** (or “unknown / conflict”)  
- That GPO can shadow Settings UI  

Without this, expanding the catalog creates duplicate “high risk” rows and confuses users.

## Coverage vs complexity (hard constraint)

- **Do not** import all of gpedit/ADMX in one version.  
- **Do** grow **domain by domain** (Update, Defender, Firewall, App privacy, Telemetry, Edge, …) with curated high-value settings + human labels.  
- Every new setting needs: stable ObjectId, human name, description, rationale, risk tag, discovery path, domain/subcategory.  
- Collectors stay fail-soft; missing keys → Not configured / skip, never throw out of Collect.

## Optional future product features (approved as ideas, not implemented)

- Overall **risk assessment** feature (explicitly designed, not the current H/M/L catalog counts)  
- **Recommended settings sets** / baselines (e.g. “privacy-hardened consumer”, “managed enterprise”) — observation vs desired state  
- Still **no remediation** until authorised; baselines first as **compare-only**

## Explicitly not next

- Interactive TUI/GUI  
- Writes / elevation helpers  
- “Score” that pretends current High-Risk list is a security grade  

---

# FUTURE STEPS (ORDERED — WHERE TO INSERT IN THE PIPELINE)

Use this sequence. Each step should leave the solution build-green and read-only.

## Step A — Domain taxonomy on the model (Models first)

**Insert at:** `ManagedObjectCatalog` + possibly new fields on `ManagedObject` (e.g. `ProductDomain` or reuse/strengthen `SubCategory` + `FeatureCategory` consistently).

**Work:**

- Define a fixed list of **product domains** (Firewall, Defender, WindowsUpdate, Telemetry, AppPrivacy, ConsentStore, Search, Edge, ActivityHistory, CloudContent, Advertising, Location, Biometrics, Device, …).  
- Assign every existing catalog ObjectId to exactly one primary domain.  
- Report grouping should prefer domain, then subcategory.  
- Document domain list in Status when added.

**Why first:** all later collectors and reports hang off stable domains. Avoids rework.

## Step B — Effective layer metadata (Models + Binder)

**Insert at:** Models (layer enum / fields on ManagedObject or a small related type); **InventoryStateBinder** (compute effective state); CLI report (show layers).

**Work:**

- Represent layers: `UserPreference`, `MachinePolicy`, `AlternatePolicyStore`, etc.  
- For linked settings (e.g. camera ConsentStore + AppPrivacy LetAppsAccessCamera), store relationship ids (`RelatedFeature` / `ConflictsWith` already exist on ManagedObject — use them).  
- Binder outputs both raw states and a best-effort `EffectiveState` + `EffectiveSource` when determinable; otherwise `Conflict` / `Unknown`.  
- Report must explain conflicts, not hide them.

**Why before mass GPO expansion:** prevents double-counting and false “high risk” noise.

## Step C — CapabilityCollector fix (Scanner)

**Insert at:** `CapabilityCollector` only.

**Work:** diagnose why count is 0 on 25H2 (elevation, PowerShell errors, parsing). Keep read-only. Log stderr on failure at Debug level if useful. Do not block other work if still 0 after best effort.

## Step D — Domain-scoped discovery expansion (Scanner + Catalog together)

**Insert at:** new or extended collectors in Scanner; matching catalog entries in Models; wire collector in CLI list.

**Preferred domain order (suggested):**

1. **Firewall** — new read-only collector (e.g. netsh or registry/WMI query patterns that work non-elevated where possible; document elevation limits honestly).  
2. **Defender** — deepen beyond current policy probes if safe read-only APIs exist.  
3. **Windows Update** — deepen schedule/channel related readable values.  
4. **App privacy / ConsentStore** — already strong; keep aligned with AppPrivacy GPO via relationships.  
5. **Telemetry** — already partially present; resolve dual-path effective value.  
6. **Edge / Search / Activity / Cloud** — expand curated sets, not entire ADMX.

**Rule:** no domain lands without catalog explanations (name, description, rationale, risk, domain).

## Step E — Report UX without interactivity (CLI)

**Insert at:** `Program.cs` report writers (or a small report helper class in CLI only — avoid new projects unless necessary).

**Work:**

- Group output by product domain.  
- Default remains concise (summary + high-risk / conflicts).  
- Flags stay non-interactive (`--full`, maybe later `--domain=Firewall`).  
- Still no prompts.

## Step F — Observation vs baseline (Models + Validator or CLI compare-only)

**Insert at:** after bind; optional desired-state fields on ManagedObject (`DesiredState` already exists on the type); compare-only report section.

**Work:**

- Define one optional baseline profile as data (not enforcement).  
- Report “matches baseline / differs / not observed.”  
- Do **not** auto-remediate.

## Step G — Optional formal risk assessment feature

**Insert at:** new pure computation after bind (CLI or thin helper); separate from structural SchemaValidator.

**Work:**

- If implemented, name it clearly (`RiskAssessment` / `ExposureSummary`) and document methodology.  
- Must not reuse “High-Risk Configured count” as a fake score without rules.  
- Prefer transparent rule lists over opaque numbers.

## Step H — Relationships graph (after domains + layers)

**Insert at:** catalog metadata + report section “Related settings.”

Use existing ManagedObject list fields (`Requires`, `ConflictsWith`, `RelatedFeature`).

## Step I — Controlled change design only (docs first)

**Insert at:** Status design note only until authorised.

Elevation-on-demand, per-setting warnings, reversibility. **No implementation** until human explicitly requests.

## Step J — Terminal UI (last among UX)

Only after domains, effective layers, and report are understandable in CLI form.

---

# KNOWN GAPS (v0.6)

| Gap | Severity | Notes |
|-----|----------|-------|
| Capabilities = 0 | Medium | Collector issue on test host |
| No firewall surface | Medium | Planned domain |
| No effective-layer resolution | High for product clarity | Dual telemetry paths already visible |
| Catalog is curated (~65), not full GPO | By design | Expand by domain |
| Risk list ≠ security score | Documentation | Do not market as score |
| No baselines / recommended sets | Planned optional | Compare-only first |

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

Confirm: inventory counts, policy configured count, validator batch, observation summary, safety confirmation.

---

# END OF DOCUMENT
