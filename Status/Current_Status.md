# Windows Privacy Platform
## Current Status

**Project Name:** Windows Privacy Platform  
**Current Development Version:** Prototype v0.6 (Bind + Validate + Risk Summary)  
**Previous Versions:** v0.5 (archived) → v0.4 → v0.3 → v0.2 → v0.1  
**Document Status:** Authoritative Current State  
**Last Updated:** 2026-07-24  
**v0.6 runtime:** Verified on Windows 11 Pro 25H2 (build 26200)

---

# Purpose

Exact current state of the active repository for future AI sessions.  
For full roadmap logic (domains, effective layers, insertion points), also read `Status/AI_Handoff.md`.

---

# What the app does today (medium-level)

Non-interactive CLI pipeline:

1. CLI starts collectors via InventoryScanner.  
2. Collectors **read** (never write): registry (identity, privacy ConsentStore, curated policy keys), PowerShell (packages; capabilities attempt), ServiceController (services), schtasks (tasks).  
3. Results go into InventorySnapshot.  
4. ManagedObjectCatalog (static explained settings) is bound to live values via InventoryStateBinder (`CurrentState`).  
5. KnowledgeBase stores entries; SchemaValidator batch-checks catalog structure.  
6. CLI prints observation/risk **summary** and high-risk **watch list** (or `--full` dump).  
7. Safety confirmation: no modifications, no elevation.

`bin/` DLLs are **compiled build outputs** from our C# projects + NuGet dependencies — not separately authored binary sources.

**Not present:** firewall collector, interactive UI, writes, overall security score, full GPO library, effective GPO-vs-UI resolution.

---

# Verified runtime sample (v0.6)

```
Identity : Windows 11 Pro | 25H2 | Build 26200
Capabilities : 0 | Packages : 165 | Services : 303 | Tasks : 247
Privacy settings : 30 | Policy probes : 79 (configured: 45)
KnowledgeBase: 65 | Validator: 65 passed / 0 failed
Observed 63 / not 2 | High-risk configured 25 | Medium-risk configured 22
Privacy Allow/Deny/Prompt: 18 / 2 / 0
```

### Meaning of risk lines

- **Not a security score.**  
- Catalog H/M/L tags are static.  
- “High-risk configured” = high-impact topics that have a configured value worth reviewing.  
- Deny on Location/Microphone can still appear under high-risk *category* because the capability is sensitive.

---

# Implementation status

| Area | Status |
|------|--------|
| Seven-project architecture | Complete |
| Discover (7 collectors) | Live; Capabilities often 0 |
| Model catalog (~65 explained objects) | Live |
| Bind CurrentState | Live |
| Structural ValidateAll | Live |
| Concise + full report | Live |
| Product domain taxonomy | **Planned** |
| Effective layer / GPO vs UI | **Planned** |
| Firewall discovery | **Planned** |
| Baselines / recommended sets | **Planned (compare-only first)** |
| Formal risk assessment feature | **Optional planned** |
| Relationships graph | **Planned** |
| Writes / elevation / interactive UI | **Deferred** |

---

# Future steps (ordered — summary)

Full detail and pipeline insertion points: **AI_Handoff.md § FUTURE STEPS**.

1. **Domain taxonomy** on catalog (Models) — Firewall, Defender, Update, Telemetry, AppPrivacy, etc.  
2. **Effective layer + relationships** (Models + InventoryStateBinder + report) — resolve GPO vs ConsentStore vs alternate policy paths; show conflicts.  
3. **CapabilityCollector** reliability pass.  
4. **Expand discovery by domain** (Firewall first among missing surfaces), each with catalog explanations — **not** full gpedit import.  
5. **Report grouped by domain**; non-interactive flags only.  
6. **Optional baselines** (desired vs observed, compare-only).  
7. **Optional transparent risk assessment** (separate from today’s watch list).  
8. Relationships presentation polish.  
9. Controlled-change **design doc only** until authorised.  
10. Terminal UI only after report is navigable and layers are clear.

### Balance rule (owner)

Maximize relevant privacy/security coverage **domain by domain** without a self-strangling “every ADMX setting” model. Human-readable GPO area is a primary value prop; overlap must be explained, not doubled blindly.

---

# Safety

Strictly read-only. No registry/service/task/package/policy writes. No elevation. No remediation. No interactive prompts.

---

# Overall

v0.6 verified. v0.5 archived by human. Next work is taxonomy + effective layers + domain expansion — still understand-first.
