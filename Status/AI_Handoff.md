# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.4 (Live Discovery Skeleton)  
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

Prototype v0.4 verified:

- All six collectors implemented
- Release build: 0 errors, 0 warnings
- Runtime on Windows 11 Pro 25H2: Identity correct, Packages 165, Services 303, Tasks 247, Privacy 17, Capabilities 0
- Safety confirmation present
- No elevation, no writes

Capabilities returning 0 is a known gap to investigate (DISM output format / availability).

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

| Layer                              | Status        |
|------------------------------------|---------------|
| Discover (inventory)               | Largely done  |
| Model (explained ManagedObjects)   | Not started   |
| Validate (beyond structural)       | Minimal       |
| Report (categorized, human view)   | Not started   |
| Relationships                      | Not started   |
| Controlled change + elevation      | Deferred      |
| Terminal / GUI                     | Deferred      |

We are still early. The discovery skeleton is real and useful as a foundation. Jumping to GUI or write paths now would violate the project charter.

---

# NEXT IMPLEMENTATION PRIORITIES

1. Investigate CapabilityCollector returning 0 on 25H2.
2. Begin defining ManagedObjects for high-value privacy / policy settings with clear Name, Description, Category, RiskLevel, Rationale.
3. Simple categorized console report of inventory.
4. Expand PrivacyCollector carefully.
5. Design (not implement) the future controlled-change contract (elevation-on-demand, warnings, reversibility).
6. Terminal UI only after the model + report layer can explain what the user is looking at.

---

# BUILD & RUNTIME POLICY

```
dotnet build -c Release
dotnet run --project Source\WindowsPrivacyPlatform.CLI -c Release
```

Confirm inventory counts, validation, and safety confirmation after every meaningful change.

---

# ARCHITECTURE RULES

Seven-project solution mandatory.  
Scanner + CLI = net8.0-windows.  
Others = net8.0.  
Explicit composition only. No DI container.  
Models stay free of business logic.

---

# END OF DOCUMENT
