# Windows Privacy Platform
## Roadmap

**Last updated:** 2026-07-24  
**Current completed milestone:** Prototype **v0.9**  
**Document role:** Ordered priorities, not calendar commitments. Preserve product identity on every horizon.

---

## Permanent constraints (all horizons)

- Read-only exploration until a **separate**, explicitly authorized controlled-change design exists.  
- No registry/service/task/firewall/policy writes in the current pipeline.  
- No elevation requirement for understanding mode.  
- No privacy/security **score** as a product feature.  
- No fear-based or “optimizer” language.  
- Unknown remains valid. Evidence over assumption.  
- Catalog-first: understand domain → catalog + ValueSemantics + explanation → collector → relationships.  
- **Windows meaning lives in catalog maps, not resolvers/CLI/UI.**  

---

## Completed: v0.9 — Knowledge semantics foundation

Shipped (see `Status/History/v0.9.md`):

- ValueSemantics + ValueSemanticsInterpreter  
- Educational resolution / confidence reasons  
- Resolver free of hardcoded numeric semantic maps (verified)  
- Provenance on Privacy/Policy binders  
- Expanded relationship kinds  
- Catalog WhenIgnored / CommonMisconception scaffolding  

---

## Next — v0.10 / presentation and depth

**Theme:** Make knowledge visible; deepen selected domains; still read-only.

### 1. Surface semantics and evidence in UI

- TUI/CLI detail cards: SemanticValue, ConfidenceReason, EvidenceSource, WhenIgnored  
- Conflict cards already educational; keep consistent  

### 2. Relationship exploration API

- Query shapes on SettingsQuery / StructuredRelationships (“everything affecting microphone”)  
- Still curated edges  

### 3. Expand ValueSemantics coverage

- Catalog-first maps for remaining high-traffic ObjectIds  
- Never invent maps in resolvers  

### 4. Machine understanding

- Best-effort Secure Boot / TPM / BitLocker / Defender visibility when read-only sources exist  
- Always label confidence and Unknown reasons  

### 5. Domain depth (careful)

- Microsoft Defender (deeper)  
- Windows Update (deeper)  
- Firewall **rule inventory** (read-only)  
- Services / tasks understanding-oriented  

### 6. GUI preparation

- Strengthen detail-view contract only; no large GUI required yet  

### 7. Comparison (design / light scaffolding)

- Scan history / compare-only diffs — design notes before heavy implementation  

---

## v1.0 vision

Stable, trustworthy **read-only** Windows privacy and security **knowledge product**: curated catalog, honest effective-layer reasoning, clear provenance and Unknowns, navigable TUI and/or thin GUI — no scores, no silent writes.

---

## Far future (design-doc only until authorized)

Controlled, reversible change as a **separate** architecture pass.

---

## Explicit non-goals (near term)

Bulk ADMX; ML relationship inference; scoring dashboards; one-click harden; product telemetry; feature count for its own sake.

---

## How to pick the next code change

1. Does it improve understanding, trust, evidence quality, or maintainability?  
2. Does it preserve read-only and one-way dependencies?  
3. Are meanings in catalog ValueSemantics, not resolver switches?  
4. Will a future session understand it from Status docs alone?  

If any answer is no, rethink before coding.
