# Windows Privacy Platform
## Project Documentation — Engineering Handbook

**Applies to:** Prototype **v0.7**  
**Last updated:** 2026-07-24  
**Document role:** Architecture, design philosophy, trust model, and security philosophy  

This is not release notes. It is the long-form engineering handbook for the repository.

---

# 1. Why this project exists

Windows privacy and security configuration is **layered**: user preferences, application settings, Group Policy, alternate policy stores, MDM, and baselines can all influence the same conceptual feature. The Settings UI often shows one surface; registry and policy show others. Tools that only dump keys, or that “optimize” without explanation, leave people with data but not understanding.

Windows Privacy Platform exists to make effective configuration **legible**:

- what a setting is  
- why Windows provides it  
- which layer appears to win  
- what related settings exist  
- what remains unknown  

Understanding is the product. Collection is infrastructure.

---

# 2. Core philosophy

## 2.1 Explain before change

Changing Windows without understanding layer precedence creates false confidence. The platform invests in explanation first so any future change design (if ever authorized) starts from honesty.

## 2.2 Never pretend certainty

Windows itself is sometimes ambiguous. Dual policy stores, missing MDM collection, and elevation-limited APIs produce incomplete evidence. The correct product behavior is to **show uncertainty**, not invent Enabled/Disabled.

## 2.3 Unknown is acceptable

Unknown is a first-class state in confidence, effective value, and inventory surfaces (for example, capabilities returning empty). Unknown builds trust; silent substitution destroys it.

## 2.4 Relationships matter more than isolated values

A ConsentStore value without AppPrivacy context is incomplete. Relationships (overrides, conflicts, related features) are a defining differentiator.

## 2.5 Layer precedence is core

Effective configuration is not “the last registry key we read.” Precedence lives in one module (`PolicyPrecedenceResolver`) so rules stay auditable.

## 2.6 Read-only first

Read-only is a feature. It allows exploration on production machines without fear. Writes require a separate architectural commitment.

## 2.7 Transparency over automation

Automation that hides reasoning trains users to trust outputs they cannot inspect. This product prefers explicit reasons and provenance.

## 2.8 Knowledge before remediation

The KnowledgeBase and catalog explanations are long-term intellectual property. Remediation without knowledge is the opposite of the mission.

## 2.9 Quality over quantity

Prefer hundreds of excellent explanations over tens of thousands of unlabeled ADMX rows.

## 2.10 Trust is the product

Every screen should increase trust through transparency, consistency, and honesty — not through scores or marketing language.

---

# 3. Security and safety philosophy

These are permanent architectural choices for the current product generation:

| Constraint | Why |
|------------|-----|
| No registry writes | Prevents accidental damage; keeps the tool safe to run anywhere |
| No elevation | Avoids UAC prompts and privileged mutation paths |
| No service/task/package/policy edits | Inventory must not become control plane |
| No remediation | Remediation needs design for reversibility, warnings, and audit |
| No optimization claims | Optimization implies judgment the platform refuses to pretend |
| No privacy/security score | Scores invent certainty and invite misuse |
| Treat inventory as untrusted input | Registry strings and process output are display-only, never executed |
| Sanitize display text | Control characters stripped before UI |
| Fixed process argument strings | No injection of discovered content into shells |

Read-only is not a temporary limitation; it is the trust boundary of v0.x.

---

# 4. Architecture overview

## 4.1 Seven-project layout

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

Explicit composition. No dependency injection container. Dependency direction is one-way and intentional.

### Why seven projects?

- **Models** can stay pure and testable without Windows APIs.  
- **Scanner** isolates OS interaction and binding.  
- **Validator** stays structural so “validation” is never confused with scoring.  
- **CLI** stays a host so future GUI can reuse the same models without rewriting logic.  

## 4.2 KnowledgeBase

In-memory repository of catalog entries after bind. Today it is a simple store; conceptually it is the home of the project’s curated knowledge asset.

## 4.3 Scanner and collectors

Collectors implement `IInventoryCollector` and fill `InventorySnapshot`. Rules:

- read-only  
- fail-soft  
- never terminate the whole scan on one failure  
- no user-controlled command injection  

Collector philosophy: partial inventory is better than a crash; empty capability lists are better than elevated writes.

## 4.4 Binding

`InventoryStateBinder` orchestrates:

- `PrivacyBinder` — ConsentStore / privacy preferences → UserPreference observations  
- `PolicyBinder` — curated policy probes → MachinePolicy / AlternatePolicyStore / UserPreference  
- `RelationshipBinder` — curated edges + calls precedence resolver  

## 4.5 Resolution

`PolicyPrecedenceResolver` owns:

- Consent vs AppPrivacy force codes  
- dual machine policy path comparison  
- generic layer-rank resolution  

Outputs `ConfigurationResolution`: effective value, source layer, confidence, reason, conflict flag, raw observations.

## 4.6 Explanation

`SettingExplanationFactory` builds documentation-style cards from catalog definition fields plus domain knowledge composition. Presentation layers format; they do not invent policy meaning.

## 4.7 Navigation

`NavigationBuilder` builds domain → feature → setting trees and `SettingDetailView` cards. This is the **application API** for any UI.

## 4.8 TUI

`TuiHost` in the CLI project is a pure Console explorer (`--tui`). It consumes navigation/query models only. Alternate screen buffer is used when available for stable repaints.

## 4.9 Relationship graph

Edges are `SettingRelationship` with `RelationshipKind`. Graph is **curated**, not inferred. Presentation maps kinds to human phrases (“Controlled by”, “Potential conflicts”).

## 4.10 Catalog philosophy

Each entry aims toward a miniature article: friendly name, overview, purpose, impact, relationships, unknowns. Growth is domain-by-domain with explanations landing with discovery paths.

## 4.11 Trust model

| Kind of content | Trust |
|-----------------|-------|
| Catalog text (names, rationales, factory prose) | Trusted editorial |
| Discovered registry/process values | Untrusted display data |
| Resolution reasons | Derived logic; must cite layers honestly |
| Impact labels (H/M/L) | Static significance tags — **not** live scores |

---

# 5. Pipeline

```
CLI flags
  → collectors → InventorySnapshot (domain sections)
  → ManagedObjectCatalog
  → PrivacyBinder / PolicyBinder (observations + layers)
  → RelationshipBinder + PolicyPrecedenceResolver
  → KnowledgeBase
  → SchemaValidator.ValidateAll
  → SettingsQuery / NavigationBuilder
  → CLI report | TUI
  → safety confirmation (non-TUI)
```

Do not shortcut stages (for example, reading registry inside the TUI).

---

# 6. Product domains

Primary navigation axis. Every catalog ObjectId has exactly one `ProductDomain`.

ConsentStore, AppPrivacy, Telemetry, WindowsUpdate, Defender, Search, Edge, ActivityHistory, CloudContent, Advertising, Location, Biometrics, Device, Speech, Firewall (reserved), Other.

---

# 7. Coding standards

- Target .NET 8; nullable reference types on  
- No business logic in UI hosts  
- Catalog-first, explanation-first domain expansion  
- Collectors remain read-only and fail-soft  
- Composition over hardcoding sprawl; still prefer explicit tables over magic  
- Small focused classes  
- Maintainability before cleverness  
- Match existing style; no drive-by renames  
- Avoid unnecessary NuGet dependencies  

---

# 8. Current capabilities (v0.7)

- Multi-collector inventory  
- ~65 explained catalog settings  
- Layer observations and known-pair effective resolution  
- SettingsQuery application API  
- NavigationBuilder domain trees  
- CLI summary, high-impact watch list, conflict cards, `--full`  
- Interactive read-only TUI  
- Neutral impact language and Observed/Interpretation separation in presentation  

---

# 9. Current limitations

- Capability enumeration often empty without elevation  
- MDM/baseline layers ranked but not collected  
- Relationship graph incomplete by design (curated)  
- Explanation depth uneven across ObjectIds  
- No Firewall domain content yet  
- No GUI  
- No baselines / comparison / history  
- No remediation  
- RiskLevel enum name is historical; UI says “impact”  

---

# 10. Roadmap (priorities, not dates)

**v0.8:** Runtime verification of v0.7; Firewall catalog+collector; more relationships/explanations; optional domain filter.  

**v0.9:** Curated expansion of Defender/Update/Telemetry/Edge; compare-only baselines; provenance consistency.  

**v1.0 vision:** Stable read-only knowledge product people trust to learn Windows configuration.  

**Far future:** Separate controlled-change design only with explicit authorization.

---

# 11. Onboarding checklist

1. Read `Status/AI_Handoff.md`  
2. Read this handbook  
3. Read `Status/Current_Status.md`  
4. Build and run CLI + `--tui` on Windows  
5. Open one ConsentStore setting and one policy conflict card; notice Observed vs Interpretation  
6. Before coding: identify which layer (catalog, collector, binder, resolver, presentation) owns the change  

---

# 12. Non-goals

- Registry cleaner  
- Privacy tweaker / optimizer  
- One-click fix  
- Benchmark or score product  
- Compliance certification engine  
- Full gpedit/ADMX clone  
- Automatic relationship inference from ML  

---

# 13. Related documents

- `Status/AI_Handoff.md` — continuity and insertion rules  
- `Status/Current_Status.md` — live snapshot  
- `Status/Prototype_v0.1_Implementation_Map.md` — file map  
- `README.md` — public overview  
