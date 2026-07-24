# Windows Privacy Platform
## Architecture

**Applies to:** Prototype **v0.8**  
**Last updated:** 2026-07-24  
**Document role:** Engineering architecture reference. Do not redesign the seven-project layout without explicit human approval.

---

## 1. Design goals

- Keep Models free of OS I/O and side effects.  
- Isolate all discovery in Scanner collectors (fail-soft, read-only).  
- Centralize layer precedence in **one** module: `PolicyPrecedenceResolver`.  
- Keep CLI/TUI as presentation hosts only.  
- Prefer composition and explicit tables over magic inference.  
- Preserve Unknown; never invent certainty.  

---

## 2. Solution structure

```
Source/
  WindowsPrivacyPlatform.sln
  WindowsPrivacyPlatform.Models/
  WindowsPrivacyPlatform.Core/
  WindowsPrivacyPlatform.Logging/
  WindowsPrivacyPlatform.KnowledgeBase/
  WindowsPrivacyPlatform.Validator/
  WindowsPrivacyPlatform.Scanner/
  WindowsPrivacyPlatform.CLI/
```

Dependency direction (strict):

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

No reverse references. No DI container; composition is explicit in `Program.cs`.

Target frameworks:

- Models, Core, Logging, KnowledgeBase, Validator: `net8.0`  
- Scanner, CLI: `net8.0-windows`  

External packages (minimal):

- `System.ServiceProcess.ServiceController`  
- `System.Management` (optional WMI path in identity collector; fail-soft)  

---

## 3. Layer responsibilities

### Models

Pure data and pure composition:

- `ManagedObject` / `ManagedObjectCatalog` — curated knowledge definitions  
- `InventorySnapshot` + domain sections (`IdentityInventory`, `NetworkingInventory`, …)  
- `MachineOverview` — device context for Home / landing (not a settings tree)  
- `ConfigurationObservation` / `ConfigurationResolution` / `SettingObservation`  
- `SettingExplanation` + `SettingExplanationFactory`  
- `SettingsQuery` — UI-independent query API  
- `NavigationBuilder` / `NavigationNode` / `SettingDetailView`  

**Forbidden in Models:** registry access, process starts, service control, writes.

### Core

Shared primitives (`OperationResult`, `PathConstants`, `PlatformException`).

### Logging

Audit logging interface and implementation. No business decisions.

### KnowledgeBase

In-memory repository of catalog entries after bind. Conceptual home of curated knowledge assets. Today: simple store; future: value semantics, richer evidence metadata.

### Validator

Structural completeness only (`SchemaValidator`). Validation is **not** a privacy/security score.

### Scanner

- `IInventoryCollector` implementations  
- `InventoryScanner` orchestration  
- `InventoryStateBinder` + domain binders (`PrivacyBinder`, `PolicyBinder`, Firewall mapping)  
- `RelationshipBinder` (curated edges)  
- `PolicyPrecedenceResolver` — **only** place for effective-layer rules  

### CLI

- Flag parsing, pipeline wiring, report writers  
- `TuiHost` — keyboard explorer; presentation only  

---

## 4. Runtime pipeline

```
Discover → Model → Validate → Bind → Resolve → Explain → Navigate → Present
```

1. Collectors fill `InventorySnapshot`.  
2. Catalog definitions load from `ManagedObjectCatalog`.  
3. Binders attach live values + `ConfigurationLayer` + provenance fields.  
4. RelationshipBinder wires edges and calls PolicyPrecedenceResolver for known pairs.  
5. KnowledgeBase stores bound objects.  
6. SchemaValidator runs structural checks.  
7. SettingsQuery / NavigationBuilder / MachineOverview feed presentation.  
8. CLI report or TUI renders; non-TUI prints safety confirmation.  

---

## 5. Configuration layers and precedence

Conceptual strength (documentation order):

```
SecurityBaseline > MDMPolicy > MachinePolicy > AlternatePolicyStore
  > ApplicationPreference > UserPreference > Unknown
```

Implemented resolution helpers:

- `ResolveConsentVsAppPrivacy`  
- `ResolveAlternateMachinePolicyPaths`  
- `ResolveByLayerRank`  

Rules:

- Never silently guess.  
- Always emit human-readable `ResolutionReason`.  
- Conflicts are explicit (`HasConflict`).  
- Ties at same rank → Unknown confidence, not a random winner.  

---

## 6. Evidence / provenance (v0.8 foundation)

`ConfigurationObservation` carries:

| Field | Intent |
|-------|--------|
| Layer | Configuration layer classification |
| RawValue | Observed token (display text) |
| SourcePath / Hive | Location evidence |
| ObservedAt | Timestamp |
| ConfidenceScore | Numeric 0–100 style signal |
| CollectorName | Which collector produced the row |
| EvidenceSource | Human-readable source label |
| AlternativeSources | Cross-check list |
| CollectionNotes | Limitations / conflicts / method notes |
| EffectiveConfidence | High / Medium / Low / Unknown |

Collectors must not invent provenance. Missing sources → Unknown + notes.

---

## 7. Machine Overview vs configuration exploration

**Machine Overview** answers “What is this computer?” using best-effort identity and platform service visibility.

**Configuration exploration** answers “What settings exist and who controls them?” via domain → category → setting → explanation card.

Do not dump hardware fields into setting cards or relationship graphs as if they were GPO settings.

---

## 8. Catalog and KnowledgeBase philosophy

- Catalog-first domain expansion: human name, domain, rationale, impact tag, discovery path, relationships, then collector.  
- Quality over quantity; no bulk ADMX import as a near-term path.  
- Explanations written as neutral technical documentation (Microsoft-doc + educator tone).  
- Separate **Observed** facts from **Interpretation**.  
- Never “you should disable this” as product voice.  

---

## 9. Relationship model

- `SettingRelationship` + `RelationshipKind`  
- Curated pairs in `RelationshipBinder`  
- Presentation maps kinds to human phrases (“Controlled by”, “Potential conflicts”)  
- No automatic ML inference in this phase  

---

## 10. UI contract

Any future GUI **must** consume:

- `SettingsQuery`  
- `NavigationBuilder` / `SettingDetailView`  
- `MachineOverview`  
- `SettingExplanation`  

Do not invent a parallel domain model inside a UI project. Do not put registry reads in the UI.

TUI (`TuiHost`) is the current proof of that contract.

---

## 11. Safety architecture

| Constraint | Mechanism |
|------------|-----------|
| No writes | Collectors open registry read-only; no SetValue / service change APIs |
| No elevation | No manifest requireAdministrator; no UAC helpers |
| Fail-soft | Collector exceptions caught; pipeline continues |
| Untrusted inventory | Display sanitize; never execute discovered strings |
| Fixed shell args | CapabilityCollector uses constant command strings |

---

## 12. Extension checklist (before PR)

- [ ] Read-only?  
- [ ] No elevation?  
- [ ] Models free of OS I/O?  
- [ ] Precedence only in PolicyPrecedenceResolver?  
- [ ] UI free of business decisions?  
- [ ] Unknown preserved?  
- [ ] Catalog explanation quality acceptable?  
- [ ] Provenance honest (no invented sources)?  
- [ ] Solution builds Release on Windows?  

---

## 13. Explicitly out of architecture scope (current phase)

- Remediation / write path in the same pipeline  
- Scoring engines  
- Full firewall rule control plane  
- Bulk ADMX clone  
- Silent system modification “for testing”  
