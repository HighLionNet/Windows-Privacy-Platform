# Windows Privacy Platform

**Current milestone:** Version **1.6.2**

Local privacy and security **knowledge explorer** for Windows with an explicit, verified Modify path.

> **Understand first. Change deliberately.**

---

## What it does

- **Inspect (default):** Full read-only inventory of privacy ConsentStore values, Group Policy / registry policies, identity, services, packages, scheduled tasks, and firewall profile state.
- **Modify (explicit):** After UAC elevation + session authorize, supported registry-backed settings can be changed. Every change is pre-read, confirmed, applied, and **only reported successful after independent read-back verification**. Audited to `changes.log` / `auth.log`.

Firewall rules are observation-only. This tool does **not** create, edit, enable, or delete firewall rules. Use Windows Firewall with Advanced Security (`wf.msc`) for rule changes.

---

## Architecture

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App (WPF)
```

- **Models** — pure data (catalog, observations, ValueSemantics). No OS I/O.
- **Scanner** — collectors are the only inventory read layer.
- **PolicyChangeService** — sole write path; requires `ElevationService.IsModifyAuthorized`.
- **PolicyPrecedenceResolver** — sole precedence authority.

---

## Building

```powershell
cd Source
dotnet build -c Release
```

## Publishing (win-x64)

```powershell
dotnet publish ".\Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj" -c Release -r win-x64 --self-contained false -o ".\publish"
```

## Running

```powershell
cd Source\WindowsPrivacyPlatform.App
dotnet run -c Release
```

For accurate HKLM / firewall / service reads, run elevated (right-click → Run as administrator, or use the in-app Modify elevation path).

---

## Safety

- Inspect mode never writes.
- Modify requires explicit mode switch, UAC elevation, session authorize, per-change confirmation, and verified read-back.
- Non-concrete paths (service controllers, multi-key summaries) are refused for writes.
- No privacy “score”. No cloud backend. No telemetry of the tool itself.

Repository: [HighLionNet/Windows-Privacy-Platform](https://github.com/HighLionNet/Windows-Privacy-Platform)
