# Windows Privacy Platform
## Implementation Map — Prototype v0.6

**Purpose**

Authoritative record after v0.6 bind + validate + risk summary.

---

# Version History (recent)

| Version | Summary |
|---------|---------|
| v0.4 | Live discovery skeleton (verified). |
| v0.5 | PolicyCollector, ManagedObjectCatalog, full categorized report (archived). |
| v0.6 | InventoryStateBinder, batch validation, ObservationSummary, concise default report. |

---

# Collectors

Unchanged from v0.5 set (Identity, Capability, Package, Service, ScheduledTask, Privacy, Policy).

---

# Model / Bind / Validate / Report (v0.6)

| Component | Role |
|-----------|------|
| ManagedObjectCatalog | Privacy + policy explained objects |
| InventoryStateBinder | Maps snapshot → CurrentState; builds ObservationSummary |
| SchemaValidator | ObjectId, ObjectName, Description, ObjectType, SchemaVersion; ValidateAll |
| ObservationSummary | Risk and observation aggregates |
| CLI | Default: summary + high-risk; `--full`: complete dump; `--help` |

---

# CLI flags (non-interactive)

```
(default)   Risk summary + high-risk configured items
--full      Full categorized catalog report
--help, -h  Help text
```

---

# Read-Only Guarantees

No writes. No elevation. No interactive prompts. Collectors query-only.

---

# Known gaps

- CapabilityCollector may return 0 on some builds.
- Relationships between ManagedObjects not populated.
- Compliance baselines (desired vs actual policy) not yet formalized beyond observation.

---

# Deferred

Remediation, writes, elevation UI, interactive TUI/GUI, network, telemetry.
