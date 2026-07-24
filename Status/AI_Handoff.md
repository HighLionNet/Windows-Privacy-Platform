# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Post-v0.2 Development  
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

Prototype v0.1 was completed successfully and archived under `Archive/v0.1/`.

Prototype v0.2 is **complete**:

- Logging project
- Collector-based Scanner
- Structural Validator
- Explicit CLI composition
- Release build verified (0 errors / 0 warnings)
- Runtime pipeline verified (placeholder collectors + full safety confirmation)

Immediately after v0.2 closure the first real Windows collector was added:

**WindowsIdentityCollector** — read-only discovery of Windows version, edition and build number using Registry.LocalMachine (non-elevated) + Environment fallback.

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

WindowsIdentityCollector is the first real collector and performs **only reads** against Registry.LocalMachine and Environment. It never writes and never elevates.

---

# CURRENT DEVELOPMENT PHASE

Post-v0.2 discovery phase.

Focus: replace remaining placeholder collectors with real, read-only Windows discovery, one collector at a time.

---

# V0.2 OBJECTIVES — FINAL STATUS

| Objective                              | Status      |
|----------------------------------------|-------------|
| Logging project                        | Complete    |
| Collector-based Scanner                | Complete    |
| Preserve seven-project architecture    | Complete    |
| Keep prototype read-only               | Complete    |
| Successful Release compilation         | Verified    |
| Runtime pipeline verification          | Verified    |
| First real collector (Identity)        | Implemented |

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

WindowsPrivacyPlatform.CLI  
WindowsPrivacyPlatform.Core  
WindowsPrivacyPlatform.KnowledgeBase  
WindowsPrivacyPlatform.Logging  
WindowsPrivacyPlatform.Models  
WindowsPrivacyPlatform.Scanner  
WindowsPrivacyPlatform.Validator

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

1. Pull latest and confirm WindowsIdentityCollector produces real version/edition/build data.
2. Implement the next real collector (recommended order):
   - CapabilityCollector
   - PackageCollector
   - ServiceCollector
   - ScheduledTaskCollector
   - PrivacyCollector

Each collector must remain strictly read-only.

---

# FIRST REAL DISCOVERY TARGET — COMPLETED

WindowsIdentityCollector has been implemented with:

- Primary path: read-only Registry.LocalMachine\SOFTWARE\Microsoft\Windows NT\CurrentVersion
- Fallback: Environment.OSVersion
- No writes, no elevation, graceful failure handling

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
