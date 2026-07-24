# Windows Privacy Platform
## AI Project Handoff Document

**Document Status:** Authoritative Continuity Document  
**Applies To:** Prototype v0.2 Development  
**Supersedes:** Previous AI Project Handoff for Prototype v0.1

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

Prototype v0.1 was completed successfully.

The entire v0.1 repository has been archived.

The archive exists under:

Archive/v0.1/

The archived copy is considered the historical baseline and must not be modified.

All current development now occurs only in the active repository.

---

# ACTIVE REPOSITORY

Current active repository root:

C:\Windows Privacy Platform  (or GitHub HighLionNet/Windows-Privacy-Platform)

Current top-level folders:

Archive
KnowledgeBase
Source
Status

The previous documentation folders (Docs, Specifications, Prompts, Reviews, Versions) have been intentionally removed from the active working tree after their contents had served their purpose during Prototype v0.1.

Historical copies remain inside the archived v0.1 snapshot.

---

# DEVELOPMENT MODEL

Development now follows a three-part workflow.

## ChatGPT

Acts as:

- technical lead
- architecture reviewer
- continuity manager
- safety reviewer
- prompt author
- project historian

ChatGPT does **not** produce implementation code for the repository.

Instead ChatGPT:

- evaluates architectural decisions
- detects design regressions
- reviews Grok output
- produces implementation prompts
- maintains continuity documentation
- updates project status documents

---

## Grok

Grok performs implementation.

Responsibilities include:

- editing repository files
- creating new source files
- refactoring code
- preserving architecture
- keeping the solution buildable

Future Grok sessions will interact directly with the repository through a GitHub connector.

The workflow is intentionally changing from:

ChatGPT
↓

Prompt

↓

Manual copy

↓

Repository

to:

ChatGPT
↓

Implementation prompt

↓

Grok (GitHub Connector)

↓

Repository

↓

Build

↓

Review

This reduces transcription errors and keeps repository history cleaner.

---

# DEVELOPMENT RULES

The following rules are mandatory.

## Rule 1

Every change must preserve a successful build.

Large unverified changes are prohibited.

---

## Rule 2

Every logical task should leave the repository compiling.

---

## Rule 3

Every AI session ends with updated continuity documentation.

The Status documents are considered part of the implementation.

---

## Rule 4

Future AI sessions must distinguish between:

Implemented

and

Planned

Never describe planned work as completed work.

---

## Rule 5

Never redesign working architecture without explicit approval.

Incremental evolution is preferred.

---

# PROTOTYPE SAFETY RULES

These rules remain absolute.

The prototype remains strictly read-only.

The following are prohibited:

- Registry modification
- Registry writes
- Service modification
- Service start/stop
- Scheduled task modification
- Package removal
- Capability removal
- Group Policy modification
- Local Security Policy modification
- Windows feature modification
- Recovery implementation
- Snapshot implementation
- Rollback implementation
- Elevation helpers
- UAC prompts
- Administrator password prompts
- Hidden write paths
- Silent Windows modification

If an implementation proposal violates any of these rules, work must stop until explicitly authorised.

---

# CURRENT DEVELOPMENT PHASE

Prototype v0.2

This version is focused entirely on improving architecture.

Prototype v0.2 is NOT introducing remediation.

Prototype v0.2 is introducing the first scalable discovery architecture.

---

# CURRENT V0.2 OBJECTIVES

The active goals are:

1. Implement the Logging project.
2. Replace the monolithic scanner with a collector framework.
3. Preserve the seven-project architecture.
4. Keep the prototype entirely read-only.
5. Maintain successful compilation after each logical task.
6. Prepare for future real read-only Windows collectors.

---

# V0.2 ARCHITECTURAL DIRECTION

The scanner is evolving from:

InventoryScanner

↓

Placeholder data

to:

InventoryScanner

↓

Collector Framework

↓

Individual Collectors

↓

InventorySnapshot

This allows future collectors to be added independently without redesigning the scanner.

---

# CURRENT COLLECTOR PLAN

The initial collector framework consists of:

WindowsIdentityCollector

CapabilityCollector

PackageCollector

ServiceCollector

ScheduledTaskCollector

PrivacyCollector

During the current stage these collectors remain placeholders.

Real Windows discovery will be introduced incrementally after the architecture is verified.

---

# LOGGING DIRECTION

Logging did not exist in Prototype v0.1.

Prototype v0.2 introduces:

IAuditLogger

AuditLogger

AuditEventType

The logger is intentionally lightweight.

Current implementation target:

Console logging only.

Future expansion:

Additional logging sinks.

No external dependencies.

---

# CURRENT IMPLEMENTATION STATUS

Architecture work has been designed and reviewed.

Implementation code has been integrated into the repository by Grok and is build-clean on GitHub.

Specifically:

Architecture review:
COMPLETE

Safety review:
COMPLETE

Code review:
COMPLETE

Repository integration:
COMPLETE

Build alignment (ScanResult.Message + IValidationRule.Name):
COMPLETE

Compilation verification:
PENDING (local Windows + .NET 8 after `git pull origin main`)

Runtime verification:
PENDING (local after pull)

---

# BUILD POLICY

After every logical implementation task:

Run:

dotnet build -c Release

If build fails:

Fix immediately before continuing.

No additional features should be implemented while compilation is broken.

**Important (2026-07-24):** Any local build error that matches the old attachment mismatches (missing ScanResult.Message or IValidationRule.RuleId/Description) indicates a stale local tree. Run `git pull origin main` first.

---

# RUNTIME POLICY

After every successful build:

Run the CLI.

Verify:

- application starts
- scanner executes
- validator executes
- knowledge base functions
- logging functions
- safety confirmation still appears

---

# ARCHITECTURE RULES

The seven-project solution remains mandatory.

No additional projects may be added without an explicit task.

Current projects:

WindowsPrivacyPlatform.CLI

WindowsPrivacyPlatform.Core

WindowsPrivacyPlatform.KnowledgeBase

WindowsPrivacyPlatform.Logging

WindowsPrivacyPlatform.Models

WindowsPrivacyPlatform.Scanner

WindowsPrivacyPlatform.Validator

---

# KNOWLEDGE BASE NOTE

The duplicate top-level KnowledgeBase folder remains.

It is intentionally retained.

Future AI must avoid creating additional copies.

---

# IMPLEMENTATION STYLE

Future code changes should:

Replace complete files where practical.

Avoid partial patches unless necessary.

Keep every task small.

Keep every task reviewable.

Prefer complete file replacement over fragmented edits.

---

# NEXT IMPLEMENTATION TASK

1. `git pull origin main`
2. Verify successful build and runtime of the integrated v0.2 architecture.
3. Only after successful verification should work begin on real Windows discovery.

---

# FIRST REAL DISCOVERY TARGET

The first live collector should be:

WindowsIdentityCollector

Replace placeholder values with read-only Windows identity information.

No registry writes.

No elevation.

No Windows modification.

---

# IMPORTANT CONTINUITY RULE

Future AI must distinguish between:

Generated code

Integrated code

Verified code

Only verified code should be treated as implemented.

Never assume generated code has already been merged into the repository.

---

# END OF DOCUMENT

Any future AI beginning work without reading this document is operating outside the intended project workflow.
