# Windows Privacy Platform
## Complete Technical Documentation

**Document Status:** Living  
**Current Applies To:** Prototype v0.6 (Bind + Validate + Risk Summary)  
**Last Updated:** 2026-07-24

---

# 1. Project Overview

Local, declarative privacy intelligence platform for Windows. Architecture-first. Understand before change.

---

# 2. Guiding Philosophy

Discover → Model → Validate → Report → Relationships → Controlled remediation (future).

v0.6 improves Validate and Report without interactivity or writes.

---

# 3. Current Capabilities

- Full v0.5 discovery (identity, packages, services, tasks, privacy, policy probes)
- ManagedObject catalog with explanations
- Inventory → CurrentState binding
- Batch structural validation of catalog entries
- Observation/risk summary
- Default concise report; `--full` for complete categorized dump
- Strictly read-only

---

# 4–5. Layout & Architecture

Seven projects. Explicit composition. Models free of business logic. Scanner + CLI = net8.0-windows.

---

# 6. Runtime Pipeline (v0.6)

CLI → Scan → Bind CurrentState → KnowledgeBase load → ValidateAll → ObservationSummary → Report (summary or --full) → Safety confirmation

---

# 14. Known Gaps

- Capabilities may be 0 on some hosts
- Relationship graph not started
- No desired-state compliance scoring yet (observation only)
- No write/elevation/UI paths

---

# Build & Run

```
dotnet build -c Release
dotnet run --project Source\WindowsPrivacyPlatform.CLI -c Release
dotnet run --project Source\WindowsPrivacyPlatform.CLI -c Release -- --full
```
