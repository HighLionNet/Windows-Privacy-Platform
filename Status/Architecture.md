# Windows Privacy Platform
## Architecture

**Applies to:** Prototype **v0.9.5**  
**Last updated:** 2026-07-24  
**Document role:** Engineering architecture reference. Do not redesign the seven-project layout without explicit human approval.

---

## 1. Design goals

- Keep Models free of OS I/O and side effects.  
- Isolate all discovery in Scanner collectors (fail-soft, read-only).  
- **Catalog / ValueSemantics own Windows meaning**; resolvers never hardcode raw-value maps.  
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

- `ManagedObject` / `ManagedObjectCatalog` — curated knowledge definitions + **ValueSemantics**  
- `ValueMeaning` / `ValueSemanticsInterpreter` — raw → canonical → display; never guess  
- `InventorySnapshot` + domain sections  
- `MachineOverview`  
- `ConfigurationObservation` / `ConfigurationResolution` (incl. ConfidenceReason, SemanticValue)  
- `SettingExplanation` + `SettingExplanationFactory`  
- `SettingsQuery` / `NavigationBuilder`  

**Forbidden in Models:** registry access, process starts, service control, writes.

### Core / Logging / KnowledgeBase / Validator

Primitives; audit log; in-memory bound catalog store; structural schema checks + batch unique-ObjectId guard.

### Scanner

- Collectors: facts + provenance only  
- Binders: attach layers and evidence fields  
- `RelationshipBinder`: curated edges (full AppPrivacy–ConsentStore set); calls resolver with **ManagedObject** definitions  
- `PolicyPrecedenceResolver`: layer rank + apply **catalog canonical** meanings; **no raw-code semantic tables**  

### CLI

Presentation only (`Program.cs`, `TuiHost`).

---

## 4. Runtime pipeline

```
Discover → Model → Validate → Bind → Resolve → Explain → Navigate → Present
```

1. Collectors fill `InventorySnapshot`.  
2. Catalog loads with ValueSemantics maps.  
3. Binders attach live values + layer + provenance.  
4. RelationshipBinder + PolicyPrecedenceResolver (interpreter + educational reasons).  
5. KnowledgeBase stores bound objects.  
6. SchemaValidator structural checks + unique ID.  
7. SettingsQuery / NavigationBuilder / MachineOverview / SettingExplanation feed presentation.  
8. CLI or TUI; non-TUI safety confirmation.  

---

## 5. Value semantics (v0.9+)

| Rule | Detail |
|------|--------|
| Owner | Catalog `ManagedObject.ValueSemantics` + `ValueSemanticsInterpreter` |
| Input | Raw observed token |
| Output | Canonical + DisplayLabel + Description + optional edition/version notes |
| Missing map | Return null / Unknown — **never invent** |
| Resolver | May match on canonical names (e.g. ForceAllow) **after** interpretation; must not map `"1"` → meaning locally |

ConsentStore string tokens (Allow/Deny/Prompt) may be normalized syntactically because those strings are the registry values themselves.

v0.9.5 expands maps for AUOptions, DODownloadMode, MAPS, sample consent, Edge tracking, and binary polarity policies.

---

## 6. Configuration layers and precedence

```
SecurityBaseline > MDMPolicy > MachinePolicy > AlternatePolicyStore
  > ApplicationPreference > UserPreference > Unknown
```

Helpers: `ResolveConsentVsAppPrivacy`, `ResolveAlternateMachinePolicyPaths`, `ResolveByLayerRank`.

Rules: never silently guess; always `ResolutionReason` + `ConfidenceReason`; conflicts explicit; same-rank ties → Unknown.

---

## 7. Evidence / provenance

`ConfigurationObservation`: Layer, RawValue, SourcePath, Hive, ObservedAt, ConfidenceScore, CollectorName, EvidenceSource, AlternativeSources, CollectionNotes, EffectiveConfidence.

Binders (Privacy, Policy, Firewall) populate provenance honestly.

---

## 8. Machine Overview vs configuration exploration

Unchanged: Overview is device context; configuration trees are settings. Do not mix.

---

## 9. Catalog philosophy

- Catalog-first: name, domain, explanation, **ValueSemantics**, WhenIgnored, relationships, then collector.  
- Quality over quantity.  
- Observed facts separate from interpretation.  
- Neutral technical documentation tone.  

---

## 10. Relationship model

`SettingRelationship` + expanded `RelationshipKind` (Overrides, IgnoredWhen, AlternativeStorage, UsuallyConfiguredWith, …). Curated in RelationshipBinder. No ML inference.

---

## 11. UI contract

Future GUI must consume SettingsQuery, NavigationBuilder, MachineOverview, SettingExplanation, and resolution SemanticValue / ConfidenceReason fields. No registry in UI.

---

## 12. Safety architecture

No writes; no elevation; fail-soft collectors; untrusted inventory never executed; fixed shell args only.

---

## 13. Extension checklist (before PR)

- [ ] Read-only?  
- [ ] No elevation?  
- [ ] Models free of OS I/O?  
- [ ] New meanings in catalog ValueSemantics (not resolver switches)?  
- [ ] Precedence only in PolicyPrecedenceResolver?  
- [ ] UI free of business decisions?  
- [ ] Unknown preserved?  
- [ ] Provenance honest?  
- [ ] Release build on Windows?  

---

## 14. Explicitly out of scope (current phase)

Remediation in the same pipeline; scoring; full firewall rule control; bulk ADMX; silent system modification.
