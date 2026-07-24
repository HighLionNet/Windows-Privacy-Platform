# Windows Privacy Platform
## Implementation Map — Prototype v0.5

**Purpose**

Authoritative record of the current implementation after v0.5 model + policy discovery + categorized report.

---

# Version History (recent)

| Version | Summary |
|---------|---------|
| v0.4 | Live discovery skeleton (6 collectors). Verified on Win11 Pro 25H2. |
| v0.5 | PolicyCollector (GPO/policy probes), expanded ManagedObjectCatalog, categorized console report. |

---

# v0.4 verified runtime (Windows 11 Pro 25H2 build 26200)

- Build: 0 errors, 0 warnings
- Identity: Windows 11 Pro | 25H2 | 26200
- Packages: 165 | Services: 303 | Tasks: 247 | Privacy: 17 | Capabilities: 0
- Safety confirmation present

---

# Collectors (v0.5)

| Collector                  | Implementation                                      | Notes |
|----------------------------|-----------------------------------------------------|-------|
| WindowsIdentityCollector   | Registry NT\CurrentVersion + build≥22000 rule       | Hardened value parse |
| CapabilityCollector        | PowerShell Get-WindowsCapability; DISM /English fallback | May return 0 without elevation |
| PackageCollector           | PowerShell Get-AppxPackage (current user)           | Live |
| ServiceCollector           | ServiceController.GetServices()                     | Live |
| ScheduledTaskCollector     | schtasks /query /fo CSV                             | Live |
| PrivacyCollector           | HKCU ConsentStore + related privacy preferences     | Live |
| PolicyCollector (new)      | Table-driven HKLM/HKCU policy/preference probes     | Missing → "Not configured" |

---

# Model layer (v0.5)

- `ManagedObjectCatalog.PrivacySettings` — ConsentStore + advertising/speech/content delivery
- `ManagedObjectCatalog.PolicySettings` — telemetry, update, defender, search, activity, cloud, app privacy, edge, biometrics, device
- `ManagedObjectCatalog.All` — combined
- Fields used for explanations: ObjectName, Description, SubCategory, RiskLevel, Rationale, ControlLevel, DiscoveryMethod

---

# Report layer (v0.5)

CLI prints categorized report: groups catalog by SubCategory, resolves current value from PrivacySettings / PolicySettings, prints description and rationale.

---

# Dependencies

- System.ServiceProcess.ServiceController 8.0.1 (Scanner only)
- No other new third-party packages
- No elevation helpers

---

# Read-Only Guarantees

All collectors open registry keys writable:false or use query-only process invocation.  
No collector writes. No collector requests elevation.  
CLI prints explicit safety confirmation.

---

# Known gaps

- CapabilityCollector may still return 0 on some 25H2 configurations.
- Policy probes cover major privacy/security surfaces; not every ADMX setting in Windows.
- Report is console-only; no terminal UI framework yet.

---

# Next targets

1. Hardware verify v0.5
2. CapabilityCollector follow-up
3. Expand probes/catalog from runtime gaps
4. Controlled-change design (not implementation) after report is solid

---

# Deferred

Remediation, GPO/registry writes, elevation-on-demand UI, terminal/GUI frameworks, persistence beyond memory, compliance scoring, relationship graph.
