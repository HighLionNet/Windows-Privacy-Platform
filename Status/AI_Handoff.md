# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.2  
**Last Updated:** 2026-07-24

---

# PURPOSE

Primary continuity document. Every AI session must read this completely before any code change.

---

# PROJECT VISION

Local, declarative privacy intelligence platform for Windows.  
Development order is fixed and non-negotiable:

1. Discover  
2. Model  
3. Validate  
4. Report  
5. Understand relationships  
6. Only then consider controlled, reversible remediation

Philosophy: **Understand first. Change later.**

---

# CURRENT STATE (2026-07-24)

- Prototype v0.1 archived under `Archive/v0.1/`
- Prototype v0.2 architecture fully integrated
- Release build verified (`dotnet build -c Release` → 0 errors / 0 warnings)
- Runtime verification still pending

---

# ACTIVE REPOSITORY

GitHub: `HighLionNet/Windows-Privacy-Platform` (main)  
Local root: `C:\Windows Privacy Platform`

Top-level folders:
- Archive/
- KnowledgeBase/   (intentional mirror)
- Source/          (seven-project solution)
- Status/

---

# DEVELOPMENT MODEL

- ChatGPT: architecture, safety, continuity, review
- Grok: direct repository implementation via GitHub connector
- Human: define objectives, local build + runtime verification, approve direction

---

# MANDATORY RULES

1. Every change must leave the solution compiling.
2. Prefer small, reviewable commits.
3. Never redesign working architecture without explicit approval.
4. Distinguish Implemented vs Planned.
5. Update Status documents at the end of every session.

---

# SAFETY RULES (ABSOLUTE)

The prototype remains strictly read-only. Prohibited:

- Registry modification
- Service / scheduled-task / package / capability / policy changes
- Elevation, UAC prompts
- Remediation, rollback, recovery, snapshots
- Network communication or telemetry

Any proposal that violates these rules must stop until explicitly authorised.

---

# V0.2 OBJECTIVES — STATUS

| Objective                              | Status     |
|----------------------------------------|------------|
| Logging project                        | Complete   |
| Collector-based Scanner                | Complete   |
| Preserve seven-project architecture    | Complete   |
| Keep prototype read-only               | Complete   |
| Successful Release compilation         | **Verified** |
| Runtime pipeline verification          | Pending    |
| Prepare for real Windows collectors    | Ready      |

---

# BUILD & RUNTIME POLICY

After every logical task:

```
dotnet build -c Release
```

If build fails → fix immediately.

After successful build:

```
dotnet run --project Source\WindowsPrivacyPlatform.CLI -c Release
```

Confirm logger, collectors, KnowledgeBase store, validation, and safety confirmation.

---

# NEXT IMPLEMENTATION TASK

1. Obtain confirmed runtime output of the CLI.
2. Only after runtime success, implement the first real read-only collector:

   **WindowsIdentityCollector**

   - Replace placeholder strings with actual Windows version / edition / build data.
   - Use only read-only APIs (Environment, Registry.LocalMachine read, or WMI read).
   - No writes, no elevation, no UAC.

---

# FIRST REAL DISCOVERY TARGET

`WindowsIdentityCollector` — read-only identity information only.

Subsequent collectors (Capabilities, Packages, Services, Tasks, Privacy) are introduced one at a time after the identity collector is verified.

---

# CONTINUITY RULE

Distinguish:

- Generated code
- Integrated code
- Verified code

Only verified code is treated as implemented.

---

# END OF DOCUMENT
