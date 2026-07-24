# Windows Privacy Platform
## Current Status — Prototype v0.9.5 (post-implementation)

**Document role:** Authoritative live snapshot. A new engineer or AI session should be able to orient from this file alone, then drill into Architecture, Roadmap, and History.

**Last updated:** 2026-07-24  
**Current development version:** Prototype **v0.9.5**  
**Previous archived milestone:** Prototype v0.9  
**Runtime target:** Windows 11 (25H2 / build ~26200 class); Scanner and CLI target `net8.0-windows`  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation. No scores.

Companion documents:

| Document | Role |
|----------|------|
| `Status/Architecture.md` | Layer responsibilities, pipeline, value semantics, evidence model |
| `Status/Roadmap.md` | Ordered priorities after v0.9.5 |
| `Status/History/v0.9.md` | Value semantics, educational resolution |
| `Status/History/v0.9.5.md` | Knowledge maturity — full probe catalog, semantics, relationships |
| `Status/AI_Handoff.md` | Continuity rules for implementers |
| `Status/Project_Documentation.md` | Long-form philosophy handbook |
| `README.md` | Public overview and run instructions |

---

## 1. Product identity (do not dilute)

Windows Privacy Platform is a **local, read-only Windows privacy and security knowledge explorer**.

It helps humans answer:

- What is this computer?
- What configuration exists?
- What does the raw value mean?
- Where did that observation come from?
- Which layer appears to control the effective value — and why?
- What related settings matter?
- How confident is the interpretation?
- What remains Unknown?

It is **not**: a registry cleaner, optimizer, tweaker, score engine, compliance suite, remediation tool, or XDR replacement.

Philosophy: **Understand first. Change later.**

---

## 2. Current architecture (summary)

Seven C# / .NET 8 projects, one-way dependencies, no DI container:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

| Project | Responsibility |
|---------|----------------|
| **Models** | Catalog + **ValueSemantics**, InventorySnapshot, MachineOverview, ConfigurationObservation/Resolution, SettingExplanation, SettingsQuery, NavigationBuilder |
| **Core** | OperationResult, PathConstants, PlatformException |
| **Logging** | IAuditLogger / AuditLogger |
| **KnowledgeBase** | In-memory store of bound catalog entries |
| **Validator** | Structural SchemaValidator + batch unique ObjectId guard |
| **Scanner** | Collectors, binders, RelationshipBinder, **PolicyPrecedenceResolver** (precedence only; meaning from catalog maps) |
| **CLI** | Flags, pipeline, reports, **TuiHost** (presentation only) |

---

## 3. Current data flow

```
CLI flags → Collectors → InventorySnapshot → Catalog (with ValueSemantics)
  → Binders (provenance) → RelationshipBinder + PolicyPrecedenceResolver (maps via ValueSemanticsInterpreter)
  → KnowledgeBase → SchemaValidator → SettingsQuery / NavigationBuilder / MachineOverview / SettingExplanation
  → CLI report or TUI → safety confirmation (non-TUI)
```

Rules:

- Collectors: facts only.  
- Catalog / ValueSemantics: meaning only.  
- Resolvers: layer precedence + apply catalog canonical meaning; never invent raw-code maps.  
- UI: presentation only.  

---

## 4. Current collectors

Unchanged from v0.8/v0.9 set: WindowsIdentityCollector, CapabilityCollector, PackageCollector, ServiceCollector, ScheduledTaskCollector, PrivacyCollector, PolicyCollector, FirewallCollector. All read-only, fail-soft.

---

## 5. Current domains

ConsentStore · AppPrivacy · Telemetry · WindowsUpdate · Defender · Search · Edge · ActivityHistory · CloudContent · Advertising · Location · Biometrics · Device · Speech · Firewall · Other

Catalog SchemaVersion: **0.9.5** on entries after attach.

---

## 6. Current capabilities (v0.9.5)

Everything from v0.9, plus:

- **Catalog coverage** aligned with every PolicyCollector probe + expanded ConsentStore capabilities
- **ValueSemantics** maps for AUOptions, Delivery Optimization, MAPS/Spynet, sample submission, Edge tracking prevention, and the full binary polarity set
- **Relationship graph**: 14 AppPrivacy–ConsentStore pairs; additional Defender / Update / Search / Location edges
- **Validator** batch unique-ObjectId detection
- Educational WhenIgnored / CommonMisconception / TypicalEnterpriseUse populated on high-value entries

---

## 7. Safety guarantees (permanent for this phase)

Unchanged: no registry/service/task/policy/firewall writes; no elevation; no remediation; no scores; no product telemetry; Unknown first-class.

---

## 8. Current limitations

1. Secure Boot / TPM / BitLocker / Entra often **Unknown** without elevation.  
2. Firewall **profile-level** only.  
3. MDM / SecurityBaseline collection incomplete.  
4. TUI/CLI do not fully surface SemanticValue / ConfidenceReason on all detail cards.  
5. No relationship “query everything affecting X” API beyond StructuredRelationships.  
6. No scan history / comparison.  
7. No GUI.  
8. Dual KnowledgeBase folder at repo root vs `Source/` — **Source is authoritative**.  

---

## 9. Technical debt

- Root `KnowledgeBase/` folder is non-build duplicate of Source.  
- Capability enumeration weak non-elevated.  
- Some future-facing enums still lightly used.  

---

## 10. Presentation modes

| Mode | Command | Behavior |
|------|---------|----------|
| Default CLI | `dotnet run -c Release` | Machine overview + summary + watch list + conflict cards |
| Full | `--full` | Domain-grouped catalog dump |
| TUI | `--tui` | Home / Machine Overview / Domains / detail |
| Help | `--help` | Usage |

---

## 11. Immediate next direction

See `Status/Roadmap.md` (post-v0.9.5): surface semantics/evidence in presentation, relationship query shapes, careful domain depth — still read-only.

---

## 12. Build and run

```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --tui
```

Confirm: build 0 errors; Machine Overview; conflict/telemetry resolution text educational; AppPrivacy/AllowTelemetry semantic path when maps apply; safety confirmation; no UAC.
