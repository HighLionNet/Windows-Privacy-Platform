# Windows Privacy Platform
## Complete Technical Documentation

**Document Status:** Living  
**Current Applies To:** Prototype v0.3 (Functional Identity Skeleton)  
**Last Updated:** 2026-07-24

---

# 1. Project Overview

Windows Privacy Platform is a local, declarative privacy intelligence platform for Windows.

Its long-term purpose is to discover, model, validate, understand and eventually (under a separately authorised development phase) perform controlled, reversible remediation of Windows privacy-related configuration.

The platform is intentionally architecture-first.

Every Windows-managed entity will ultimately become a formally defined ManagedObject stored inside the platform's Knowledge Base.

Unlike traditional Windows tweaking tools, the platform is designed around understanding Windows before attempting to modify it.

This philosophy governs every architectural decision.

---

# 2. Guiding Philosophy

The platform follows one immutable development sequence.

1. Discover
2. Model
3. Validate
4. Report
5. Understand relationships
6. Perform controlled remediation (future work only)

Prototype v0.2 completed the architectural foundation for stages 1–3.

Prototype v0.3 delivered the first real discovery capability (Windows identity) while remaining strictly read-only.

No remediation exists. No Windows modifications occur.

---

# 3. Prototype Objectives

**v0.2 (achieved)** verified that the platform architecture can safely:

• Construct a layered runtime  
• Discover system state through a structured Scanner  
• Represent Windows entities as ManagedObjects  
• Store ManagedObjects inside a Knowledge Base  
• Validate ManagedObjects against structural rules  
• Record runtime activity through an Audit Logger  
• Complete the pipeline without modifying Windows

**v0.3 (achieved)** added the first real, multi-version Windows identity collector and confirmed clean build + runtime + security posture.

---

# 4. Current Repository Layout

Current repository structure:

Archive/  
KnowledgeBase/  
Source/  
Status/

Archive contains immutable historical versions.

Prototype v0.1 has been archived under Archive/v0.1/.

Development occurs exclusively against the active Source directory.

The top-level KnowledgeBase folder intentionally mirrors the KnowledgeBase implementation used by the Source project.

No additional duplicate implementations should be created.

---

# 5. Solution Architecture

The solution consists of seven projects.

WindowsPrivacyPlatform.Models — pure data structures (ManagedObject, InventorySnapshot, ValidationResult, ScanResult, ComplianceReport, AuditEntry, Enums). No business logic.

WindowsPrivacyPlatform.Core — shared infrastructure (OperationResult, PlatformException, PathConstants). No ElevationHelper.

WindowsPrivacyPlatform.Logging — runtime audit logging (AuditEventType, IAuditLogger, AuditLogger). Console only, thread-safe, UTC timestamps.

WindowsPrivacyPlatform.KnowledgeBase — storage of ManagedObjects (InMemoryKnowledgeBaseRepository). Memory only.

WindowsPrivacyPlatform.Scanner — discovery. InventoryScanner orchestrates collectors. Collectors own data acquisition. Targets `net8.0-windows`.

WindowsPrivacyPlatform.Validator — structural validation (SchemaValidator, RequiredFieldRule). Required fields: ObjectId, ObjectName.

WindowsPrivacyPlatform.CLI — operator entry point. Explicit composition, no DI container, no command parsing yet. Targets `net8.0-windows`.

---

# 6. Runtime Pipeline

CLI → AuditLogger → KnowledgeBase → InventoryScanner → Collectors → InventorySnapshot → ManagedObject → KnowledgeBaseEntry → SchemaValidator → ValidationResult → Console Output

Every component has a single responsibility. Dependencies flow in one direction only.

---

# 7. Logging Architecture

Console output only. UTC timestamps. Severity levels: Debug, Information, Warning, Error. Thread-safe. No file, network or telemetry. Future sinks can be added without changing callers.

---

# 8. Scanner Architecture

InventoryScanner coordinates collectors. Each collector owns one subsystem.

Current collectors:

- WindowsIdentityCollector — **real, read-only** (Registry.LocalMachine + Environment fallback). Correctly handles Windows 10 and Windows 11 (build ≥ 22000 rule), DisplayVersion and EditionID.
- CapabilityCollector — placeholder
- PackageCollector — placeholder
- ServiceCollector — placeholder
- ScheduledTaskCollector — placeholder
- PrivacyCollector — placeholder

---

# 9. Validation Architecture

Structural only. RequiredFieldRule checks ObjectId and ObjectName. No policy or compliance engine yet.

---

# 10. Knowledge Base

InMemoryKnowledgeBaseRepository. Memory only. No persistence. Interfaces designed for later persistence without redesign.

---

# 11. Dependency Philosophy

Models ← Core ← Logging ← KnowledgeBase ← Validator ← Scanner ← CLI

No circular dependencies. Explicit composition by CLI. No DI framework.

---

# 12. Security Model

Executes under current user context only. No elevation, no UAC, no privilege escalation, no network, no telemetry.

Scanner and CLI target `net8.0-windows` solely to access Registry APIs safely; no elevation is ever requested.

---

# 13. Safety Model

Strictly read-only. Prohibited: registry writes, service/task/package/capability/policy changes, remediation, rollback, recovery, snapshots.

WindowsIdentityCollector performs only non-elevated reads.

A focused security / quality review of the identity collector and surrounding pipeline found no vulnerabilities, no race conditions, no elevation paths and no write paths.

---

# 14. Current Technical Debt (Intentional)

- Console-only logging
- Remaining placeholder collectors
- Memory-only KnowledgeBase
- No CLI argument parser
- No persistence
- No reporting / compliance / relationship engine

---

# 15. Development Workflow

ChatGPT: architecture, design, safety, continuity, documentation.  
Grok: repository modifications via GitHub connector.  
Human: objectives, local build + runtime verification, approval.

---

# 16. Build Expectations

```
dotnet build -c Release
```
Expected: successful build, zero errors, zero warnings preferred.

---

# 17. Runtime Expectations

CLI starts, logger initializes, scanner executes, collectors run (identity collector returns real data), KnowledgeBase stores object, validator validates, safety confirmation displayed. No Windows modifications.

---

# 18. Future Expansion (Recommended Order)

1. Implement remaining real collectors one at a time (still read-only).
2. Lightweight CLI argument parsing.
3. Expand validation rules.
4. Persistent KnowledgeBase (preserve interfaces).
5. Reporting.
6. Relationship modelling.

Only after these layers are mature should any remediation architecture be considered under a separately authorised phase.

---

# 19. Architectural Principles

Preserve the seven-project solution.  
Preserve layered architecture.  
Prefer explicit composition.  
Keep Models free of business logic.  
Keep Scanner read-only.  
Keep Validator independent of Windows.  
Keep Logging independent of storage.  
Avoid unnecessary dependencies.  
Avoid speculative implementation.  
Avoid architectural redesign.

Every change should strengthen the existing foundation rather than replace it.
