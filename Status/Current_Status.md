# Windows Privacy Platform
## Current Status — Prototype v0.8 (post-implementation)

**Document role:** Authoritative live snapshot. A new engineer or AI session should be able to orient from this file alone, then drill into Architecture, Roadmap, and History.

**Last updated:** 2026-07-24  
**Current development version:** Prototype **v0.8**  
**Previous archived milestone:** Prototype v0.7  
**Runtime target:** Windows 11 (25H2 / build ~26200 class); Scanner and CLI target `net8.0-windows`  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation. No scores.

Companion documents:

| Document | Role |
|----------|------|
| `Status/Architecture.md` | Layer responsibilities, pipeline, dependency rules |
| `Status/Roadmap.md` | Ordered priorities from v0.9 onward |
| `Status/History/v0.7.md` | What v0.7 shipped and why |
| `Status/History/v0.8.md` | What v0.8 shipped and why |
| `Status/AI_Handoff.md` | Continuity rules for implementers (update after major milestones) |
| `Status/Project_Documentation.md` | Long-form philosophy handbook (v0.7 base; still valid) |
| `Status/Prototype_v0.1_Implementation_Map.md` | Historical file map (content tracks later versions) |
| `README.md` | Public overview and run instructions |

---

## 1. Product identity (do not dilute)

Windows Privacy Platform is a **local, read-only Windows privacy and security knowledge explorer**.

It helps humans answer:

- What is this computer?
- What configuration exists?
- Where did that observation come from?
- Which layer appears to control the effective value?
- What related settings matter?
- What remains Unknown?

It is **not**:

- a registry cleaner  
- an optimizer or privacy tweaker  
- a security score generator  
- a compliance certification engine  
- a remediation or auto-hardening tool  
- an XDR replacement  

Philosophy in one line: **Understand first. Change later.**

---

## 2. Current architecture (summary)

Seven C# / .NET 8 projects, one-way dependencies, no DI container:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
```

| Project | Responsibility |
|---------|----------------|
| **Models** | Catalog, InventorySnapshot, MachineOverview, ConfigurationObservation/Resolution, SettingExplanation, SettingsQuery, NavigationBuilder |
| **Core** | OperationResult, PathConstants, PlatformException |
| **Logging** | IAuditLogger / AuditLogger |
| **KnowledgeBase** | In-memory store of bound catalog entries |
| **Validator** | Structural SchemaValidator only (not a score) |
| **Scanner** | Collectors, binders, RelationshipBinder, **PolicyPrecedenceResolver** (sole precedence owner) |
| **CLI** | Flags, pipeline orchestration, reports, **TuiHost** (presentation only) |

Scanner + CLI: `net8.0-windows`. Others: `net8.0`.

Deep detail: `Status/Architecture.md`.

---

## 3. Current data flow

```
CLI flags (--full | --tui | --help | default)
  → InventoryScanner runs IInventoryCollector list
  → InventorySnapshot (domain sections + identity + networking/security)
  → ManagedObjectCatalog.All loaded
  → InventoryStateBinder (PrivacyBinder / PolicyBinder / Firewall mapping)
  → RelationshipBinder + PolicyPrecedenceResolver
  → KnowledgeBase populated
  → SchemaValidator.ValidateAll
  → SettingsQuery + NavigationBuilder + MachineOverview.FromSnapshot
  → CLI default report | --full | TuiHost
  → Safety confirmation (non-TUI)
```

Rules:

- Collectors gather facts (fail-soft, read-only).
- Binders attach observations and layers.
- Resolvers determine effective meaning for known pairs only.
- KnowledgeBase / catalog / SettingExplanationFactory provide understanding text.
- UI (CLI/TUI) only presents model decisions.

---

## 4. Current collectors

| Collector | Mechanism | Notes |
|-----------|-----------|-------|
| WindowsIdentityCollector | HKLM CurrentVersion + RuntimeInformation + optional WMI/CIM | Multi-source; confidence + notes; fail-soft |
| CapabilityCollector | powershell / pwsh / DISM | Often empty non-elevated → treat as Unknown |
| PackageCollector | Get-AppxPackage | Current user |
| ServiceCollector | ServiceController | Read-only list |
| ScheduledTaskCollector | schtasks CSV | Read-only list |
| PrivacyCollector | HKCU ConsentStore + prefs | ~30 privacy values |
| PolicyCollector | Curated HKLM/HKCU probes | ~79 probes; “Not configured” when absent |
| FirewallCollector | FirewallPolicy registry + ServiceController (MpsSvc, WinDefend hint) | Profile-level only; no rule edits |

---

## 5. Current domains (ProductDomain)

ConsentStore · AppPrivacy · Telemetry · WindowsUpdate · Defender · Search · Edge · ActivityHistory · CloudContent · Advertising · Location · Biometrics · Device · Speech · **Firewall** (curated entries live) · Other

Catalog size: privacy batch + policy batch + **Firewall batch (~8 entries)**. Still curated; not an ADMX mirror.

---

## 6. Current capabilities (v0.8)

- Multi-collector inventory into domain-organized InventorySnapshot  
- Machine Overview (device context separate from configuration exploration)  
- ConfigurationObservation provenance fields (CollectorName, EvidenceSource, AlternativeSources, CollectionNotes, EffectiveConfidence)  
- Layer observations + known-pair effective resolution  
- Documentation-style SettingExplanation cards (Observed vs Interpretation)  
- SettingsQuery application API  
- NavigationBuilder domain trees  
- CLI: machine overview + observation summary + high-impact watch list + conflict cards; `--full`; `--tui`  
- TUI Home → Machine Overview | Explore Domains | Search  
- Neutral impact language (not a machine score)  

---

## 7. Safety guarantees (permanent for this phase)

- No registry writes  
- No service, task, package, capability, policy, or firewall rule modifications  
- No elevation / UAC requirement  
- No remediation, “fix”, or optimizer paths  
- No privacy/security product score  
- No product network telemetry  
- Discovered strings treated as untrusted display text (sanitized; never executed)  
- Fixed process argument strings only where shells are used  
- Unknown is first-class; never invent Enabled/Disabled from absence  

---

## 8. Current limitations

1. Secure Boot / TPM / BitLocker / Entra join often remain **Unknown** without elevation or extra providers.  
2. WMI may be restricted; identity collector falls back with notes.  
3. Firewall is **profile-level** only (no per-rule inventory).  
4. MDM / SecurityBaseline ranks exist in the model; collectors for those layers are incomplete.  
5. Relationship graph is **curated**, not inferred.  
6. Provenance fields exist on observations; detail-card UI can surface them more completely.  
7. No value-semantics dictionary in KnowledgeBase yet (raw 0/1/2 often still need catalog prose).  
8. No scan history / comparison mode.  
9. No GUI (TUI only).  
10. RiskLevel enum name is historical; presentation says “impact”.  

---

## 9. Technical debt (honest)

- `Status/AI_Handoff.md` and `Project_Documentation.md` still titled around v0.7 in places; use History + this file as live truth until those are fully refreshed.  
- `Prototype_v0.1_Implementation_Map.md` filename is historical.  
- Some catalog `SchemaVersion` strings still say 0.6 on older entries; Firewall entries use 0.8.  
- Capability enumeration still weak without elevation.  
- Dual KnowledgeBase folder at repo root vs `Source/` — **Source is authoritative for build**.  
- ComponentOwner / FeatureCategory enums contain future-facing values not fully used.  

---

## 10. Presentation modes

| Mode | Command | Behavior |
|------|---------|----------|
| Default CLI | `dotnet run -c Release` | Machine overview + summary + watch list + conflict cards |
| Full | `--full` | Domain-grouped catalog dump |
| TUI | `--tui` | Home / Machine Overview / Domains / detail |
| Help | `--help` | Usage |

---

## 11. Immediate next direction (see Roadmap.md)

v0.9 themes: true evidence model maturity, value semantics in KnowledgeBase, relationship exploration API, careful domain expansion (Defender / Update), GUI preparation models — still read-only, still no remediation.

---

## 12. Build and run

```powershell
cd Source
dotnet build -c Release
cd WindowsPrivacyPlatform.CLI
dotnet run -c Release
dotnet run -c Release -- --tui
```

Confirm on Windows: inventory lines, Machine Overview block, Firewall profile counts, validator pass counts, TUI Home, safety confirmation, no UAC.
