# Architecture

## Product shape

The WPF desktop application is the only supported product surface. Its dependency direction is:

```text
Models → Core / Logging / KnowledgeBase → Validator / Scanner → App
```

There is no CLI, background agent, service, driver, web backend, or plugin runtime.

## Catalog and inventory split

`ManagedObjectCatalog` defines curated policy knowledge. Finalization applies value semantics, technical location, structured narrative, applicability, explicit exclusion decisions, and fixed write authorizations. `CatalogPolicy` exposes only writable, relevant registry policies as Settings; unapproved policy definitions remain internal references.

`DynamicInventoryCatalog` converts live bulk services, tasks, packages, features, capabilities, and firewall rules into stable System Explorer view models. Static native anchors are omitted from the runtime catalog so live collectors are the single source for component inventory. Dynamic entries cannot acquire write targets.

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

The main shell exposes an evidence-based overview, editable privacy/security domains, a virtualized System Explorer, search, and About. Small categories are flattened inline; larger categories receive drill-down pages. Product and build identity comes from assembly metadata.

## Mutation pipeline

Registry policy and firewall-profile operations implement the verified-write contract. The UI cannot construct targets. `PolicyChangeService` accepts only a complete registry catalog target, enforces mode/elevation/applicability boundaries, requires confirmation, writes the typed value, and verifies the independent read-back.

Services, scheduled tasks, packages, Windows features, capabilities, arbitrary firewall rules, BitLocker, User Account Control, and live dynamic inventory have no backend route.

## Release pipeline

`Directory.Build.props` is the sole product-version source. Local and CI packaging publish a framework-dependent win-x64 app, optionally Authenticode-sign the executable from environment-provided credentials, add first-run instructions, create a zip and SHA-256 checksum, and upload the archive. Version tags additionally create the GitHub Release.
