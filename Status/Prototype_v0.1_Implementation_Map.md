# Windows Privacy Platform
## Implementation Map — Prototype v0.4

**Purpose**

Authoritative record of the current implementation after v0.4 live discovery verification.

---

# Current Status

Prototype v0.4 verified on Windows 11 Pro 25H2 (build 26200):

- Build: 0 errors, 0 warnings
- Identity: Windows 11 Pro | 25H2 | 26200
- Packages: 165 | Services: 303 | Tasks: 247 | Privacy: 17 | Capabilities: 0
- Safety confirmation present
- No elevation, no writes

---

# Runtime Pipeline

Unchanged from v0.2 architecture. All collectors now execute real read-only discovery.

---

# Collectors

| Collector                  | Implementation                                      | Status on test machine |
|----------------------------|-----------------------------------------------------|------------------------|
| WindowsIdentityCollector   | Registry NT\CurrentVersion + build≥22000 rule       | Live, correct          |
| CapabilityCollector        | dism /online /get-capabilities parse                | Live, 0 results        |
| PackageCollector           | PowerShell Get-AppxPackage (current user)           | Live, 165              |
| ServiceCollector           | ServiceController.GetServices()                     | Live, 303              |
| ScheduledTaskCollector     | schtasks /query /fo CSV                             | Live, 247              |
| PrivacyCollector           | HKCU ConsentStore (limited capability list)         | Live, 17               |

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

# Known Issue

CapabilityCollector returns 0 on the verified 25H2 machine. Investigate DISM output format / locale / path next.

---

# Next Implementation Targets

1. CapabilityCollector reliability
2. ManagedObject population with descriptions + categories for discovered items
3. Categorized console report
4. Controlled-change design (not implementation) only after model/report layer exists

---

# Deferred

Remediation, GPO/registry writes, elevation-on-demand UI, terminal/GUI frameworks, persistence beyond memory, compliance scoring, relationship graph.
