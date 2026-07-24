# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.3 (Functional Identity Skeleton)  
**Last Updated:** 2026-07-24

---

# PURPOSE OF THIS DOCUMENT

This document is the primary continuity document for every future AI session.

Every AI assisting with this repository MUST read this document completely before proposing, reviewing, or modifying any code.

This document exists to eliminate assumptions and preserve architectural continuity between AI sessions.

If any statement in this document conflicts with assumptions made by an AI, this document takes precedence.

---

# PROJECT VISION

Windows Privacy Platform is **not** intended to become another Windows tweaking utility.

Its long-term purpose is to become a structured, declarative privacy intelligence platform capable of understanding, modelling, validating and eventually (under tightly controlled conditions) managing Windows privacy-related components.

The project is intentionally developed in layers.

The development philosophy is:

> **Understand first. Change later.**

Everything in the architecture exists to support that philosophy.

The development order is fixed:

1. Discover
2. Model
3. Validate
4. Report
5. Understand relationships
6. Only then consider controlled, reversible remediation

Any attempt to bypass this order violates the project charter.

---

# CURRENT PROJECT STATE

Prototype v0.1 archived under `Archive/v0.1/`.

Prototype v0.2 complete (architecture, build, runtime).

Prototype v0.3 (current):
- First real collector implemented and verified
- Correctly identifies Windows 10 and Windows 11 (build ≥ 22000 rule)
- Reports marketing release (22H2 / 23H2 / 24H2 / 25H2 …) and edition
- Release build clean (0 errors / 0 warnings)
- Runtime verified on Windows 11 Pro 25H2 (build 26200)
- Security / quality review of identity collector and pipeline passed (no vulnerabilities, no race conditions, no elevation or write paths)

The platform can now identify the machine it is running on.

---

# ACTIVE REPOSITORY

GitHub: HighLionNet/Windows-Privacy-Platform (main)  
Local root: C:\Windows Privacy Platform

Top-level folders:

Archive  
KnowledgeBase  
Source  
Status

---

# DEVELOPMENT MODEL

ChatGPT — architecture, safety, continuity, review, documentation  
Grok — direct repository implementation via GitHub connector  
Human — objectives, local build + runtime verification, final approval

---

# DEVELOPMENT RULES

1. Every change must preserve a successful build.
2. Every logical task should leave the repository compiling.
3. Every AI session ends with updated continuity documentation.
4. Distinguish Implemented vs Planned.
5. Never redesign working architecture without explicit approval.

---

# PROTOTYPE SAFETY RULES

These rules remain absolute.

The platform remains strictly read-only.

Prohibited:

- Registry modification / writes
- Service modification / start / stop
- Scheduled task modification
- Package or capability removal
- Group Policy or Local Security Policy changes
- Elevation helpers or UAC prompts
- Remediation, rollback, recovery, snapshots
- Network communication or telemetry

WindowsIdentityCollector performs **only reads** against Registry.LocalMachine and Environment. It never writes and never elevates.

---

# CURRENT DEVELOPMENT PHASE

Post-v0.2 discovery phase (v0.3 identity skeleton).

Focus: replace remaining placeholder collectors with real, read-only Windows discovery, one collector at a time.

---

# V0.2 / V0.3 OBJECTIVES — FINAL STATUS

| Objective                              | Status      |
|----------------------------------------|-------------|
| Logging project                        | Complete    |
| Collector-based Scanner                | Complete    |
| Preserve seven-project architecture    | Complete    |
| Keep prototype read-only               | Complete    |
| Successful Release compilation         | Verified    |
| Runtime pipeline verification          | Verified    |
| First real collector (Identity)        | Verified    |
| Multi-version support (Win10/11)       | Verified    |
| Security / quality review              | Passed      |

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

Confirm logger, collectors, KnowledgeBase, validation and safety confirmation.

---

# ARCHITECTURE RULES

Seven-project solution remains mandatory.

Current projects:

WindowsPrivacyPlatform.CLI          (net8.0-windows)  
WindowsPrivacyPlatform.Core         (net8.0)  
WindowsPrivacyPlatform.KnowledgeBase(net8.0)  
WindowsPrivacyPlatform.Logging      (net8.0)  
WindowsPrivacyPlatform.Models       (net8.0)  
WindowsPrivacyPlatform.Scanner      (net8.0-windows)  
WindowsPrivacyPlatform.Validator    (net8.0)

No additional projects without explicit task.

---

# KNOWLEDGE BASE NOTE

The duplicate top-level KnowledgeBase folder remains intentional. Do not create further copies.

---

# IMPLEMENTATION STYLE

- Replace complete files where practical
- Prefer small, reviewable changes
- Prefer complete file replacement over fragmented edits

---

# NEXT IMPLEMENTATION TASK

Implement the next real collector (recommended order):

1. CapabilityCollector
2. PackageCollector
3. ServiceCollector
4. ScheduledTaskCollector
5. PrivacyCollector

Each collector must remain strictly read-only. Verify build + runtime after each one.

---

# FIRST REAL DISCOVERY TARGET — COMPLETED

WindowsIdentityCollector:
- Primary path: read-only Registry.LocalMachine\SOFTWARE\Microsoft\Windows NT\CurrentVersion
- Build ≥ 22000 → Windows 11, otherwise Windows 10
- Uses DisplayVersion and EditionID for accurate release and edition
- Fallback: Environment.OSVersion
- No writes, no elevation, graceful failure handling
- Verified on Windows 11 Pro 25H2 (build 26200)

---

# IMPORTANT CONTINUITY RULE

Distinguish:

- Generated code
- Integrated code
- Verified code

Only verified code should be treated as implemented.

---

# END OF DOCUMENT

Any future AI beginning work without reading this document is operating outside the intended project workflow.
