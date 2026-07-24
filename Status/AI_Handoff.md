# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.5 (Model + Policy Discovery + Categorized Report)  
**Last Updated:** 2026-07-24

---

# PURPOSE

Primary continuity document. Every AI session must read this completely before any code change.

---

# PROJECT VISION

Local, declarative privacy intelligence platform for Windows.

Long-term goal: a centralized, categorized, well-explained interface where a semi-technical user can understand and safely adjust Windows privacy / policy / registry-related settings without memorizing GPO paths or PowerShell.

Development order is fixed:

1. Discover
2. Model
3. Validate
4. Report
5. Understand relationships
6. Only then consider controlled, reversible remediation (with elevation only when required)

Philosophy: **Understand first. Change later.**

---

# CURRENT STATE (2026-07-24)

## v0.4 finalized (hardware verified)

- Six collectors: Identity, Capability, Package, Service, ScheduledTask, Privacy
- Release build: 0 errors, 0 warnings
- Runtime Windows 11 Pro 25H2: Identity correct, Packages 165, Services 303, Tasks 247, Privacy 17, Capabilities 0
- Safety confirmation present; no elevation, no writes

## v0.5 implemented (hardware verify pending)

- **PolicyCollector** added: read-only probes of telemetry, Windows Update, Delivery Optimization, Defender, Search, Activity History, Cloud Content, Advertising, Location, AppPrivacy, Edge, Biometrics, Find My Device
- **ManagedObjectCatalog** expanded (privacy + policy) with Description, RiskLevel, Rationale, SubCategory
- **Categorized console report** joins catalog explanations to observed inventory values
- InventorySnapshot.PolicySettings; CLI banner v0.5
- Still strictly read-only

---

# ACTIVE REPOSITORY

GitHub: HighLionNet/Windows-Privacy-Platform (main)  
Local: C:\Windows Privacy Platform

Folders: Archive / KnowledgeBase / Source / Status

---

# DEVELOPMENT MODEL

ChatGPT — architecture, safety, continuity, review  
Grok — direct GitHub implementation  
Human — local build + runtime verification, direction approval

---

# MANDATORY RULES

1. Every change must leave the solution compiling.
2. Runtime check after every successful build.
3. Distinguish Implemented vs Planned.
4. Never redesign working architecture without approval.
5. Update Status documents at end of session.
6. No write paths, no elevation, no GUI until the model/reporting layer is useful.

---

# SAFETY RULES (ABSOLUTE FOR CURRENT PHASE)

Strictly read-only. Prohibited: registry writes, service/task/package/capability/policy changes, elevation, UAC, remediation, rollback, recovery, network, telemetry.

Future controlled changes (when authorised) must:
- Prompt for elevation only when the specific change requires it
- Warn on sensitive settings
- Prefer reversible actions
- Never silently modify the system

---

# WHERE WE ARE RELATIVE TO THE END VISION

| Layer                              | Status                |
|------------------------------------|-----------------------|
| Discover (inventory)               | Strong (v0.4–v0.5)    |
| Model (explained ManagedObjects)   | Started (v0.5 catalog)|
| Validate (beyond structural)       | Minimal               |
| Report (categorized, human view)   | Started (v0.5 console)|
| Relationships                      | Not started           |
| Controlled change + elevation      | Deferred              |
| Terminal / GUI                     | Deferred              |

---

# NEXT IMPLEMENTATION PRIORITIES

1. Hardware-verify v0.5 (build green, policy probe counts, categorized report readable).
2. CapabilityCollector follow-up if still 0.
3. Expand catalog/probes for gaps found at runtime.
4. Design (not implement) controlled-change contract after report layer is solid.
5. Terminal UI only after model + report can explain what the user is looking at.

---

# BUILD & RUNTIME POLICY

```
dotnet build -c Release
dotnet run --project Source\WindowsPrivacyPlatform.CLI -c Release
```

Confirm inventory counts, policy probe counts, catalog KB load, categorized report, validation, and safety confirmation after every meaningful change.

---

# ARCHITECTURE RULES

Seven-project solution mandatory.  
Scanner + CLI = net8.0-windows.  
Others = net8.0.  
Explicit composition only. No DI container.  
Models stay free of business logic.

---

# END OF DOCUMENT
