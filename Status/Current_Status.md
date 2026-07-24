# Windows Privacy Platform
## Current Status

**Project Name:** Windows Privacy Platform  
**Current Development Version:** Prototype v0.5 (Model + Policy Discovery + Categorized Report)  
**Previous Versions:** v0.4 (Live Discovery) → v0.3 (Identity) → v0.2 (Architecture) → v0.1 (Archived)  
**Document Status:** Authoritative Current State  
**Last Updated:** 2026-07-24

---

# Purpose

This document records the exact current state of the active repository.

Future AI sessions must treat this document as the authoritative description of the repository's current state.

---

# Current Development Phase

Prototype v0.5 — Discover + Model + first Report layer.

## v0.4 (finalized, verified on Windows 11 Pro 25H2)

| Collector                  | Result on test machine |
|----------------------------|------------------------|
| WindowsIdentityCollector   | Windows 11 Pro / 25H2 / 26200 |
| CapabilityCollector        | 0 (improved paths; may still return 0 without elevation on some builds) |
| PackageCollector           | 165 packages |
| ServiceCollector           | 303 services |
| ScheduledTaskCollector     | 247 tasks |
| PrivacyCollector           | 17 privacy settings |

Build: 0 errors, 0 warnings. Runtime verified. Safety confirmation present.

## v0.5 (implemented; local runtime verification pending)

Added:

- **PolicyCollector** — table-driven read-only probes of high-value GPO/policy/preference registry values (telemetry, Windows Update AU/schedule, Delivery Optimization, Defender, Search/Cortana, Activity History, Cloud Content, Advertising, Location, AppPrivacy LetApps*, Edge privacy policies, Biometrics, Find My Device). Missing values recorded as `Not configured`.
- **InventorySnapshot.PolicySettings** + `PolicySettingInfo`
- **ManagedObjectCatalog** expansion — privacy batch + policy batch; `All` combined list with Name, Description, Category/SubCategory, RiskLevel, Rationale, ControlLevel
- **Categorized console report** in CLI — groups catalog by SubCategory, shows current observed value when matched
- CLI loads full catalog into KnowledgeBase; version banner v0.5

Still strictly read-only. No elevation. No writes. No remediation.

---

# Current Objective

Stay inside Discover → Model → Validate → Report.

Immediate priorities after v0.5 local verify:

1. Confirm PolicyCollector counts and categorized report on real 25H2 hardware.
2. Continue expanding catalog + probes for remaining high-value surfaces as gaps appear.
3. Improve CapabilityCollector if still 0.
4. Do **not** introduce write paths, elevation, or GUI until report layer is validated.

---

# Repository Structure

Archive/  
KnowledgeBase/  
Source/  
Status/

Seven projects under Source/. Scanner + CLI target `net8.0-windows`.

---

# Implementation Status Summary

| Area                         | Status                                      |
|------------------------------|---------------------------------------------|
| Architecture (7 projects)    | Complete + verified                         |
| Logging                      | Complete + verified                         |
| KnowledgeBase (memory)       | Complete + verified                         |
| Validator (structural)       | Complete + verified                         |
| Identity / Services / Packages / Tasks | Live + verified                    |
| Privacy (ConsentStore + related) | Live + verified                         |
| Policy / GPO surface probes  | Live (v0.5) — verify on hardware            |
| Capabilities                 | Live; may return 0 on some builds           |
| ManagedObject catalog        | Live (v0.5 privacy + policy batches)        |
| Categorized console report   | Live (v0.5)                                 |
| Write / remediation paths    | Explicitly deferred                         |
| Elevation handling           | Explicitly deferred                         |
| Terminal / GUI               | Explicitly deferred                         |

---

# Safety Status

Strictly read-only. No registry writes, no service/task/package/policy changes, no elevation, no remediation, no network, no telemetry.

---

# Pending Work (ordered)

1. Local build + runtime verification of v0.5 on Windows 11.
2. CapabilityCollector follow-up if still 0.
3. Expand probes/catalog for any missing high-value surfaces identified at runtime.
4. Design (not implement) controlled-change contract only after report layer is solid.
5. Terminal UI preferred over full GUI after model/report is useful.

---

# Explicitly Deferred

Remediation, GPO/registry writes, elevation helpers, rollback, recovery, GUI frameworks, network features, outbound telemetry.

---

# Risks

Architectural: LOW  
Safety: LOW  
Discovery quality (Capabilities=0): MEDIUM  
Policy surface completeness: MEDIUM — probes cover major areas; not every GPO in existence  
Scope creep toward UI/writes too early: MEDIUM — resist

---

# Overall Status

v0.1 archived.  
v0.2 architecture complete.  
v0.3 identity working.  
v0.4 live discovery working (verified).  
v0.5 model + policy discovery + categorized report implemented (pending hardware verify).

Philosophy remains mandatory: **Understand first. Change later.**
