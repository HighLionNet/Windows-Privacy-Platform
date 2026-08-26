# Architecture

## Product shape

The WPF desktop application is the only supported product surface. Its dependency direction is:

```text
Models → Core / Logging / KnowledgeBase → Validator / Scanner → App
```

There is no CLI, background agent, service, driver, web backend, or plugin runtime.

## Catalog and inventory split

`ManagedObjectCatalog` defines curated settings. Finalization applies value semantics, technical location, structured narrative, applicability, explicit exclusion decisions, and fixed write authorizations. `CatalogPolicy` assigns these entries to the Settings workspace.

`DynamicInventoryCatalog` converts live bulk services, tasks, packages, features, capabilities, and firewall rules into stable view models for System Inventory. Curated native identifiers are omitted from bulk inventory so an item never appears as both a setting and a diagnostic duplicate. Dynamic entries cannot acquire write targets.

## Scan pipeline

```text
fixed collectors
  → InventorySnapshot + structured diagnostics
  → catalog clone
  → state binders and precedence resolution
  → dynamic inventory projection
  → applicability evaluation
  → validator
  → last-good ScanService state
```

Collectors are fail-soft, cancellation-aware, and preserve unknown/error/access-denied states. Process-backed collection uses `SafeProcessRunner` with a fixed executable, fixed arguments, concurrent output drain, timeout, and no shell.

## Presentation

The main shell exposes Home, domain Settings navigation, System Inventory, conflicts, search, and About. Small categories are flattened inline; categories at or above the central threshold receive drill-down pages. Read-only and not-applicable states are visible at list and detail levels. Product and build identity comes from assembly metadata.

## Mutation pipeline

Registry, service, scheduled-task, per-user AppX, optional-feature, and firewall-profile operations implement the same verified-write contract. The UI cannot construct targets. `PolicyChangeService` accepts only a complete catalog target, enforces mode/elevation/applicability boundaries, requires confirmation, then delegates to a typed backend and verifies the independent read-back.

Arbitrary firewall rules, BitLocker, User Account Control, and live dynamic inventory have no backend route. Native-tool links are a separate fixed launcher contract and never become command authorization.

## Release pipeline

`Directory.Build.props` is the sole product-version source. Local and CI packaging publish a framework-dependent win-x64 app, optionally Authenticode-sign the executable from environment-provided credentials, add first-run instructions, create a zip and SHA-256 checksum, and upload the archive. Version tags additionally create the GitHub Release.
