# Windows Privacy Platform
## Roadmap

**Last updated:** 2026-07-24  
**Current completed milestone:** Prototype **v0.8**  
**Document role:** Ordered priorities, not calendar commitments. Preserve product identity on every horizon.

---

## Permanent constraints (all horizons)

- Read-only exploration until a **separate**, explicitly authorized controlled-change design exists.  
- No registry/service/task/firewall/policy writes in the current pipeline.  
- No elevation requirement for understanding mode.  
- No privacy/security **score** as a product feature.  
- No fear-based or “optimizer” language.  
- Unknown remains valid. Evidence over assumption.  
- Catalog-first: understand domain → catalog + explanation → collector → relationships.  

---

## v0.9 — Intelligence foundation (next)

**Theme:** Evidence maturity, value semantics, relationship exploration, careful domain depth.

### 1. True evidence model

- Make every important observation answer “What exactly proves this?”  
- Surface CollectorName / EvidenceSource / AlternativeSources / Notes on detail cards consistently.  
- Support multi-source disagreement as first-class conflict evidence (not hidden).  
- Keep Models pure; collectors still produce facts only.  

### 2. Value semantics (KnowledgeBase)

- Reusable mapping from raw tokens (0/1/2/3, Allow/Deny, …) → human meaning.  
- Owned by knowledge layer, not ad-hoc UI switches.  
- Unknown raw values stay Unknown — never guess.  

### 3. Relationship exploration API

- Query shapes such as: everything affecting microphone access; what controls Windows Update; why effective differs from user selection.  
- Build on SettingsQuery + StructuredRelationships; still curated edges.  
- Presentation remains neutral.  

### 4. Machine understanding expansion

- Improve best-effort Secure Boot / TPM / BitLocker / virtualization / Defender visibility **when read-only sources exist**.  
- Always label confidence and Unknown reasons.  

### 5. Domain expansion (careful)

Priority candidates (catalog + explanation before deep collectors):

- Microsoft Defender (deeper than current policy probes)  
- Windows Update (deeper)  
- Firewall **rule inventory** (still read-only)  
- Startup / services / tasks (understanding-oriented, not “disable these”)  

Order for each domain: understand → catalog → explanation → collector → relationships.

### 6. GUI preparation

- No large GUI required in v0.9.  
- Strengthen models so a future GUI can offer: Machine | Security | Privacy | Updates | Applications | Network | System.  
- Detail view contract: Explanation · Observed · Evidence · Relationships · (future History).  

### 7. Comparison system (design / light scaffolding only if safe)

- Plan scan history and compare-only diffs (“what changed since last week?”).  
- No destructive actions. Prefer explicit design notes before heavy implementation.  

---

## v1.0 vision

A stable, trustworthy **read-only** Windows privacy and security **knowledge product**:

- High-quality curated catalog  
- Honest effective-layer reasoning  
- Clear provenance and Unknowns  
- Navigable TUI and/or thin GUI  
- No scores, no silent writes  

Success looks like: a technical user trusts the explanations enough to teach others how Windows configuration actually layers.

---

## Far future (design-doc only until authorized)

Controlled, reversible change:

- Separate architecture pass  
- Elevation only when required  
- Per-setting warnings  
- Prefer reversible actions  
- Full auditability  
- **Never** mixed silently into the current read-only pipeline  

---

## Explicit non-goals (near term)

- Bulk ADMX / gpedit clone  
- Automatic relationship inference via ML  
- Privacy or security scoring dashboards  
- One-click harden / “fix all”  
- Network product telemetry  
- Feature count for its own sake  

---

## How to pick the next code change

1. Does it improve understanding, trust, evidence quality, or maintainability?  
2. Does it preserve read-only and one-way dependencies?  
3. Is catalog/explanation ready before a new collector?  
4. Will a future session understand it from Status docs alone?  

If any answer is no, rethink before coding.
