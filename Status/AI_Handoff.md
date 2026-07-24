# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.6 (Bind + Validate + Risk Summary)  
**Last Updated:** 2026-07-24

---

# PURPOSE

Primary continuity document. Every AI session must read this completely before any code change.

---

# PROJECT VISION

Local, declarative privacy intelligence platform for Windows.

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

- **v0.4** hardware verified — live discovery
- **v0.5** hardware verified — policy probes + categorized report (human archived/backed up)
- **v0.6** implemented — InventoryStateBinder, batch SchemaValidator, ObservationSummary, default concise risk report (`--full` for complete dump)

Still strictly read-only. No interactive UI. No elevation. No writes.

---

# ACTIVE REPOSITORY

GitHub: HighLionNet/Windows-Privacy-Platform (main)  
Local: C:\Windows Privacy Platform

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
6. No write paths, no elevation, no interactive UI until the model/reporting layer is useful.

---

# SAFETY RULES (ABSOLUTE FOR CURRENT PHASE)

Strictly read-only. Prohibited: registry writes, service/task/package/capability/policy changes, elevation, UAC, remediation, rollback, recovery, network, telemetry.

---

# WHERE WE ARE RELATIVE TO THE END VISION

| Layer                              | Status                 |
|------------------------------------|------------------------|
| Discover (inventory)               | Strong (v0.4–v0.5)     |
| Model (explained ManagedObjects)   | Strong (v0.5)          |
| Validate (structural batch)        | Improved (v0.6)        |
| Report (summary + optional full)   | Improved (v0.6)        |
| Relationships                      | Not started            |
| Controlled change + elevation      | Deferred               |
| Terminal / GUI                     | Deferred               |

---

# NEXT IMPLEMENTATION PRIORITIES

1. Hardware-verify v0.6.
2. Optional: relationship metadata on catalog objects.
3. CapabilityCollector follow-up if still 0.
4. Design (not implement) controlled-change contract after report is stable.
5. Terminal UI only after model + report can explain findings clearly.

---

# BUILD & RUNTIME POLICY

```
dotnet build -c Release
dotnet run --project Source\WindowsPrivacyPlatform.CLI -c Release
dotnet run --project Source\WindowsPrivacyPlatform.CLI -c Release -- --full
```

---

# ARCHITECTURE RULES

Seven-project solution mandatory.  
Scanner + CLI = net8.0-windows. Others = net8.0.  
Explicit composition only. No DI container.  
Models stay free of business logic.

---

# END OF DOCUMENT
