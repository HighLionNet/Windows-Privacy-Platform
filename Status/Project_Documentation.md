# Windows Privacy Platform
## Complete Technical Documentation

**Document Status:** Living  
**Current Applies To:** Prototype v0.5 (Model + Policy Discovery + Categorized Report)  
**Last Updated:** 2026-07-24

---

# 1. Project Overview

Windows Privacy Platform is a local, declarative privacy intelligence platform for Windows.

Long-term purpose: discover, model, validate, explain, and eventually (under controlled conditions) allow safe, reversible adjustment of Windows privacy-related configuration — without requiring the user to memorize GPO paths, registry locations, or PowerShell.

Architecture-first. Understand before change.

---

# 2. Guiding Philosophy

Fixed sequence:

1. Discover  
2. Model  
3. Validate  
4. Report  
5. Understand relationships  
6. Controlled remediation (future, separately authorised)

v0.4 completed a working Discover layer. v0.5 starts Model + Report: ManagedObject catalog with explanations, PolicyCollector for GPO/policy surfaces, and a categorized console report. No remediation exists yet.

---

# 3. Current Capabilities

## Verified (v0.4 hardware)

- Windows identity (10/11, edition, build)
- AppX packages (current user)
- Windows services
- Scheduled tasks
- Privacy consent settings (HKCU ConsentStore + related preferences)
- Capabilities query (may return 0 on some builds)
- Structural validation of ManagedObjects
- In-memory Knowledge Base
- Console audit logging
- Strictly read-only execution

## Implemented in v0.5 (hardware verify pending)

- PolicyCollector: telemetry, Windows Update (AU/schedule/access), Delivery Optimization, Defender, Search/Cortana, Activity History, Cloud Content, Advertising GPO, Location, AppPrivacy LetApps*, Edge privacy policies, Biometrics, Find My Device
- ManagedObjectCatalog with Description, RiskLevel, Rationale, SubCategory for privacy + policy batches
- Categorized console report linking catalog explanations to observed values

---

# 4. Repository Layout

Archive/ — immutable history (v0.1)  
KnowledgeBase/ — intentional mirror  
Source/ — active seven-project solution  
Status/ — continuity documents

---

# 5. Solution Architecture

Seven projects. Explicit composition. No DI framework.

Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI

Scanner and CLI target net8.0-windows. Others net8.0.

---

# 6. Runtime Pipeline

CLI → Logger → KnowledgeBase → Scanner → Collectors (including PolicyCollector) → Snapshot → full ManagedObjectCatalog → KnowledgeBase → Validator → categorized report → safety confirmation

---

# 7–13. Logging / Scanner / Validation / Knowledge Base / Dependencies / Security / Safety

Unchanged in principle from v0.2/v0.3.  
All collectors are live read-only implementations.  
No elevation, no writes, no network, no telemetry.

---

# 14. Known Gaps

- CapabilityCollector may return 0 on some Windows 11 25H2 configurations.
- Policy probes cover major privacy/security surfaces; not every ADMX setting.
- Report is console text only.
- No write / elevation / UI paths (intentionally).

---

# 15–19. Workflow, Build, Runtime, Future Expansion, Principles

Build: `dotnet build -c Release` → 0 errors / 0 warnings expected.  
Runtime: `dotnet run` in CLI project → inventory counts, policy probe counts, catalog KB load, categorized report, safety confirmation.

Future order remains: verify v0.5 → expand discovery/model gaps → only then design controlled change + elevation-on-demand → terminal UI preferred over full GUI until the model is solid.

Architectural principles unchanged: preserve seven projects, layered architecture, explicit composition, Models free of logic, Scanner read-only, no speculative redesign.
