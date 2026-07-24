# Windows Privacy Platform
## Complete Technical Documentation

**Document Status:** Living  
**Current Applies To:** Prototype v0.4 (Live Discovery Skeleton)  
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

v0.4 completes a working Discover layer for the major inventory surfaces. Modeling and reporting are the next stages. No remediation exists yet.

---

# 3. Current Capabilities (Verified)

- Windows identity (10/11, edition, build)
- AppX packages (current user)
- Windows services
- Scheduled tasks
- Selected privacy consent settings (HKCU ConsentStore)
- Capabilities query via DISM (currently returns 0 on tested 25H2 machine — known gap)
- Structural validation of ManagedObjects
- In-memory Knowledge Base
- Console audit logging
- Strictly read-only execution

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

CLI → Logger → KnowledgeBase → Scanner → Collectors → Snapshot → test ManagedObject → KnowledgeBase → Validator → console output + safety confirmation

---

# 7–13. Logging / Scanner / Validation / Knowledge Base / Dependencies / Security / Safety

Unchanged in principle from v0.2/v0.3.  
All collectors are now live implementations (read-only).  
No elevation, no writes, no network, no telemetry.

---

# 14. Known Gaps

- CapabilityCollector returns 0 results on tested Windows 11 25H2 (DISM output format or availability).
- PrivacyCollector covers a focused ConsentStore set only.
- No human-readable explanations or categories on ManagedObjects yet.
- No reporting view beyond raw counts.
- No write / elevation / UI paths (intentionally).

---

# 15–19. Workflow, Build, Runtime, Future Expansion, Principles

Build: `dotnet build -c Release` → 0 errors / 0 warnings expected.  
Runtime: `dotnet run` in CLI project → real inventory + safety confirmation.

Future order remains: improve discovery → model with explanations → report/categorize → only then design controlled change + elevation-on-demand → terminal UI preferred over full GUI until the model is solid.

Architectural principles unchanged: preserve seven projects, layered architecture, explicit composition, Models free of logic, Scanner read-only, no speculative redesign.
