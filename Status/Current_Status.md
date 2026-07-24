# Windows Privacy Platform
## Current Status — Prototype v0.7

**Document role:** Authoritative snapshot of what is live in the repository *right now*.  
**Last updated:** 2026-07-24  
**Current development version:** Prototype **v0.7**  
**Previous archive:** Prototype v0.6 FINAL (understanding foundation)  
**Runtime target:** Windows 11 Pro 25H2 (build 26200 verified in v0.6; v0.7 requires local re-verification after documentation freeze)  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation.

For continuity, architecture depth, and roadmap, also read:

- `Status/AI_Handoff.md` — next-engineer continuity  
- `Status/Project_Documentation.md` — architecture and philosophy handbook  
- `Status/Prototype_v0.1_Implementation_Map.md` — file and pipeline map  
- `README.md` — public project overview  

---

## One-sentence product identity

Windows Privacy Platform is a **local, read-only privacy and security knowledge explorer** for Windows: it discovers configuration, explains it in human language, resolves effective layers where known, and lets a user navigate the result without changing the system.

It is **not** a registry cleaner, optimizer, tweaker, score engine, compliance product, or remediation tool.

---

## What v0.7 delivered

v0.7 is the first milestone that is intentionally **human-facing** rather than report-only.

| Area | Status in v0.7 |
|------|----------------|
| Thin read-only TUI (`--tui`) | **Implemented** — keyboard domain → category → setting → documentation card |
| Explanation quality | **Raised** — documentation-style narratives, neutral impact labels, misconceptions, unknowns |
| Card hierarchy | **Improved** — Observed vs Interpretation vs Related vs Provenance |
| Relationship presentation | **Humanized** — “Controlled by”, “Also related to”, “Potential conflicts” |
| Relationship coverage | **Expanded** (still curated) — Advertising, Location, Activity, Search groups + prior Consent↔AppPrivacy |
| CapabilityCollector | **Hardened + transparent** — multi-shell/DISM attempts; zero results explained as *Unknown*, not absence |
| Impact language | **De-judged** — “high-impact watch list”, not a security score |
| CLI default report | Still available; language aligned with explanation philosophy |
| Architecture | **Unchanged** seven-project layout and pipeline |

---

## Verified capabilities (functional)

### Pipeline (unchanged order)

```
Discover → Model → Validate → Bind → Resolve → Explain → Navigate → Present
```

### Inventory surfaces

| Surface | Mechanism | Typical notes |
|---------|-----------|---------------|
| Identity | HKLM NT\CurrentVersion | Version, edition, build |
| Capabilities | PowerShell / pwsh / DISM | Often empty without elevation; treated as Unknown |
| Packages | Get-AppxPackage | Current user |
| Services | ServiceController | Read-only list |
| Scheduled tasks | schtasks CSV | Read-only list |
| Privacy ConsentStore + prefs | HKCU | ~30 privacy-related values |
| Curated policy probes | HKLM/HKCU | ~79 probes; many “Not configured” |

### Knowledge and intelligence

| Component | Role |
|-----------|------|
| ManagedObjectCatalog | ~65 curated explained settings, each with `ProductDomain` |
| SchemaValidator | Structural validation only (not a score) |
| PrivacyBinder / PolicyBinder | Attach live values + configuration layers |
| RelationshipBinder | Curated graph edges |
| PolicyPrecedenceResolver | **Only** place for layer precedence rules |
| SettingExplanationFactory | Documentation-style explanation cards |
| SettingsQuery | UI-independent query API |
| NavigationBuilder | Domain tree + SettingDetailView |
| TuiHost | Presentation-only keyboard explorer |

### Presentation modes

| Mode | Command | Behavior |
|------|---------|----------|
| Default CLI | `dotnet run -c Release` | Observation summary, high-impact watch list, conflict cards |
| Full report | `--full` | Domain-grouped catalog dump with What/Why |
| TUI | `--tui` | Interactive explorer |
| Help | `--help` | Usage |

---

## Product domains in catalog

ConsentStore · AppPrivacy · Telemetry · WindowsUpdate · Defender · Search · Edge · ActivityHistory · CloudContent · Advertising · Location · Biometrics · Device · Speech · Firewall (**reserved, no entries yet**) · Other

---

## Safety confirmation (permanent for this phase)

- No registry writes  
- No service, task, package, capability, policy, or firewall modifications  
- No elevation / UAC  
- No remediation or “fix” paths  
- No privacy score or security score  
- No product network telemetry  
- Discovered strings treated as display text only (sanitized; never executed)  

---

## Known limitations (honest)

1. **Capability collection** on Windows 11 25H2 frequently returns zero without elevation; the product now states this as Unknown rather than implying nothing is installed.  
2. **Catalog size** is curated (~65), not a full ADMX or Settings app mirror.  
3. **Explanations** are substantially improved but not yet miniature encyclopedia articles for every ObjectId.  
4. **Relationship graph** is curated pairs only — not inferred, not exhaustive.  
5. **MDM / SecurityBaseline** ranks exist in the model; collectors for those layers are not implemented.  
6. **Firewall domain** is reserved only.  
7. **No GUI**, no baselines, no historical snapshots, no comparison mode.  
8. **TUI** is console-based (alternate screen buffer where supported); not a full Terminal.Gui app.  
9. **RiskLevel enum** remains H/M/L in the data model; presentation uses neutral “impact” language. The enum name is historical.  

---

## What must not be broken

1. Seven-project dependency direction: Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI.  
2. Models contain data and pure composition only — **no registry logic**.  
3. Precedence rules live **only** in `PolicyPrecedenceResolver`.  
4. UI (CLI/TUI) is **presentation only**.  
5. Collectors are fail-soft and read-only.  
6. Catalog-first when expanding domains.  
7. Unknown must remain visible; never invent Enabled/Disabled from absence of data.  

---

## Immediate next engineering priorities (after docs)

1. Local `dotnet build -c Release` and runtime verification of `--tui` on Windows 11 25H2.  
2. Optional: curated Firewall catalog + collector (still read-only).  
3. Continued explanation quality and relationship pairs — quality over quantity.  
4. CapabilityCollector deeper diagnosis only if transparency proves insufficient.  

---

## Overall assessment

v0.7 completes the transition from “scanner that prints reports” to “knowledge platform you can explore.” The architecture is stable. The product value is explanation, effective-layer honesty, and navigable understanding — not feature count.
