# Architecture

## Product shape

The WPF desktop application is the only supported product surface. Its dependency direction is:

```text
Models → Core / Logging / KnowledgeBase → Validator / Scanner → App
```

There is no CLI, background agent, service, driver, web backend, or plugin runtime.

## Catalog and inventory split

`ManagedObjectCatalog` defines curated policy knowledge. Finalization applies value semantics, technical location, structured narrative, applicability, explicit exclusion decisions, and fixed write authorizations. `CatalogPolicy` exposes only writable, relevant registry policies as Settings; unapproved policy definitions remain internal references.

`DynamicInventoryCatalog` converts live bulk tasks, packages, features, capabilities, services, and firewall rules into stable read-only explorer models. Services also have a focused top-level evidence projection through `ServiceInspection`. Static native anchors are omitted from the runtime catalog so live collectors are the single source for component inventory. Dynamic entries cannot acquire write targets.

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

The main shell exposes an identity-first Overview, category-first editable privacy/security domains, System Explorer, a virtualized read-only Services page, search, App Settings, and About. Navigation has history, breadcrumbs, and labeled search/filter controls. Four token-driven themes are selectable at runtime. Search and Overview results land on category lists with highlighted cards; detail is explicit. Product and build identity comes from assembly metadata.

## Mutation pipeline

Registry policy and firewall-profile operations implement the verified-write contract. Selecting an option creates only pending UI state. `PolicyChangeService` accepts at most 32 unique changes, verifies the compiled authorization-table hash, compares each complete runtime target to the compiled catalog authorization, enforces identity/elevation/applicability/value boundaries, pre-reads, confirms the batch once, writes typed values, and verifies independent read-back. Cross-account HKCU changes are refused. Apply rescans and keeps the authorized Admin session open; leaving Admin relaunches View-only unelevated.

Services, scheduled tasks, packages, Windows features, capabilities, arbitrary firewall rules, BitLocker, User Account Control, and live dynamic inventory have no backend route.

## Release pipeline

`Directory.Build.props` is the sole product-version source. Local and CI packaging publish a framework-dependent win-x64 app, optionally Authenticode-sign the executable from environment-provided credentials, remove PDBs, reject development/secret-like files, add instructions and a content-hash manifest, statically audit the ZIP, create a SHA-256 checksum, and upload the archive. Version tags additionally create the GitHub Release.
