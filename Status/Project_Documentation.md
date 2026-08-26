# Engineering Handbook

## Purpose

Windows privacy and security configuration is layered. User preferences, local policy, device-management policy, alternate policy stores, services, scheduled tasks, installed components, and security-platform controls can all affect a single experience. Windows Privacy Platform exists to make that evidence understandable while keeping its own authority narrow and auditable.

The product is not an optimizer, debloater, compliance score, or generic Windows editor. Its differentiation is the combination of broad local inspection, authored explanation, explicit uncertainty, and narrowly verified changes.

## Design principles

- **Evidence before interpretation.** Collection records where data came from; precedence logic explains how an effective state was chosen.
- **Unknown is honest.** Missing access or collection failure is never translated into “off” or “not configured.”
- **Narrative and technical metadata are separate.** User-facing prose explains meaning; paths and identifiers live in a labeled technical location.
- **Enumeration is not authorization.** Bulk live inventory is read-only and cannot create mutation capability.
- **Authority is static and reviewable.** A change target must be intentionally authored, typed, justified, and tested.
- **One operation at a time.** There are no profiles, bulk hardening buttons, silent remediations, or background agents.
- **Platform handoff is a feature.** High-risk or broad controls open the native Windows management surface instead of imitating it poorly.

## Core components

- **Models:** catalog, narrative, value semantics, applicability, inventory shapes, typed targets, and pure verified-write contracts.
- **Scanner:** fail-soft Windows collectors, state binding, and precedence resolution.
- **Validator:** structural, narrative, exclusion, and write-authorization invariants.
- **KnowledgeBase:** in-memory catalog repository and metadata.
- **Logging:** local operation and authorization audit.
- **App:** WPF navigation, presentation, explicit mode/elevation flow, native-tool launchers, shortcut provisioning, and concrete Windows write backends.

## Catalog lifecycle

Catalog entries are authored as conceptual settings, then finalized centrally. Finalization provides or validates technical location, semantics, narrative, applicability, workspace placement, native handoff, and a write/exclusion decision. Validation occurs before scan results are presented.

Curated settings can bind against live observations. Non-curated live objects are projected into System Inventory after collection with stable derived IDs and read-only exclusion. This preserves discoverability without allowing the installed software population to alter product authority.

## User experience model

Startup presents a factual Inspect/Modify choice. Inspect is safe for broad device understanding. Modify still requires per-operation confirmation and operating-system elevation when necessary.

Settings navigation uses domains and meaningful categories, flattening very small categories to avoid empty-feeling drilldowns. Every row distinguishes writable, view-only, and not-applicable states. Details prioritize summary, day-to-day effect, guidance, consequences, limits, and misconception before technical location.

System Inventory has a persistent diagnostic notice, search, category filtering, current state, and technical location. It does not reuse Settings action controls.

## Release and provenance

Product identity is compiled from shared assembly metadata. About displays app version, build identifier, maintainer identity, repository, and detected Windows edition/build. Release archives are built through the same script locally and in CI, optionally Authenticode-signed without committing credentials, and accompanied by a checksum and deployment instructions.

See `Architecture.md` for the data flow and `Safety_Model.md` for the enforced authority model.
