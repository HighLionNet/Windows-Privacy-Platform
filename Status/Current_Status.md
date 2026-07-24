# Windows Privacy Platform
## Current Status — Prototype v0.8

**Document role:** Authoritative snapshot of what is live in the repository *right now*.  
**Last updated:** 2026-07-24  
**Current development version:** Prototype **v0.8**  
**Previous:** Prototype v0.7  
**Safety posture:** Strictly read-only. No writes. No elevation. No remediation.

---

## One-sentence product identity

Windows Privacy Platform is a **local, read-only privacy and security knowledge explorer** for Windows: it discovers configuration, explains it in human language, resolves effective layers where known, surfaces machine context and evidence provenance, and lets a user navigate the result without changing the system.

---

## What v0.8 delivered

| Area | Status |
|------|--------|
| Machine Overview model + CLI/TUI surface | **Implemented** — separate from domain trees |
| ConfigurationObservation provenance fields | **Implemented** |
| Multi-source WindowsIdentityCollector | **Implemented** (registry + runtime + optional WMI; fail-soft) |
| FirewallCollector + curated Firewall catalog | **Implemented** (profiles, defaults, logging summary, MpsSvc) |
| TUI Home → Machine Overview / Explore Domains | **Implemented** |
| Architecture | Unchanged seven-project layout |

---

## Safety confirmation

- No registry writes  
- No service, task, package, capability, policy, or firewall modifications  
- No elevation / UAC  
- No remediation or “fix” paths  
- No privacy score or security score  
- No product network telemetry  

---

## Known limitations

1. Secure Boot / TPM / BitLocker / Entra join often remain **Unknown** without elevation or additional providers.  
2. WMI may be restricted; identity falls back gracefully.  
3. Firewall coverage is profile-level only (not per-rule inventory).  
4. MDM / SecurityBaseline layers still ranked but not fully collected.  
5. Catalog remains curated (privacy + policy + small Firewall set).  
6. Local Windows runtime verification of v0.8 still required on target hosts.  

---

## Immediate next priorities

1. Runtime verification on Windows 11 25H2.  
2. Stronger provenance display on detail cards.  
3. Curated Defender / Update expansion.  
4. Optional `--domain=` filter.  
