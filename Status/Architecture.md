# Architecture

**Line:** 2.6.1 on `main` (frozen snapshot `v2.6.1`). Parent: v2.5.1 @ b06efc9.

## Product shape

.NET 8 WPF desktop app only.

```text
Models → Core / Logging / KnowledgeBase → Validator / Scanner → App
```

No CLI, background agent, service, driver, web backend, or plugin runtime.

## Information architecture

```text
Caption: system min / max / close via WPF-UI TitleBar (never hide Close)
Menu: File, Edit, View, Settings, Help
Header: identity, back/forward, search, View-only|Administrator, scan
Context bar: Privacy | Security | Network | Troubleshoot/Explore   + breadcrumbs
Left rail: section title (section color) + that section's categories only
Content: Dashboard or category list or detail or persist page
Footer: status, catalog counts, version
```

Section buttons are exclusive toggles. Selected state is a shadow, not a second control theme. Subcategories exist only in the left rail. The rail header shows the active section title in that section’s color token.

Persistent destinations (reachable from every section): Dashboard, Conflicts, Knowledge Explorer, App Settings, About.

`SettingsListTarget` stands: overview, search, and dashboard findings land on a category list and must not auto-open a detail route.

### Section map (`ProductDomain`)

| Section | ProductDomain values |
|---|---|
| Privacy | ConsentStore, AppPrivacy, Telemetry, Location, CloudContent, Advertising, ActivityHistory, Device, Speech, Clipboard, Copilot, Recall, Search, Edge, Widgets, OneDrive, FamilySafety, Storage, Accessibility |
| Security | Defender, ExploitProtection, BitLocker, Uac, Biometrics, WindowsHello, LocalSecurity, WindowsUpdate, FindMyDevice |
| Network | Firewall, Network, RemoteAccess |
| Troubleshoot/Explore | Not a Settings domain. Hosts System Explorer, Services inventory, tasks/packages/features/capabilities, native handoffs, live probes |

`ProductDomain.Other` is assigned at catalog finalization; it must not remain a fifth rail.

Firewall profile Settings stay `CatalogBucket.Settings` under Network. Live firewall *rules*, services, tasks, packages, features, and capabilities stay `CatalogBucket.SystemInventory` and render in Troubleshoot/Explore. A Network → Firewall category page may *summarize* rule counts and link to Explore; it does not become a generic rule editor in this phase.

## Catalog and inventory

`ManagedObjectCatalog` owns curated knowledge, narratives, applicability, exclusions, and static write authorizations. `CatalogPolicy` exposes writable relevant registry policy as Settings; unapproved definitions stay internal references.

`DynamicInventoryCatalog` projects live inventory into stable explorer models. Dynamic entries cannot acquire `WritableTarget`.

Category pages in Privacy / Security / Network list related inventory as Observe or NativeHandoff rows (category completeness). That projection does not grant a write target.

## Scan pipeline

```text
fixed collectors
  → InventorySnapshot + diagnostics
  → catalog clone + binders + precedence
  → dynamic inventory (read-only)
  → protection-product observation (Security Center / AntivirusProduct)
  → applicability + validator
  → last-good ScanService state
```

Collectors stay fail-soft, cancellation-aware, and preserve unknown / error / access-denied. Process-backed collection uses `SafeProcessRunner` only.

Optional live probes in Troubleshoot/Explore are the same shape: fixed command or existing API, timeout, evidence appended to the snapshot, never a write.

## Mutation pipeline

Unchanged: `PolicyChangeService` for curated registry and firewall-profile targets. Selecting an option is pending UI state only. Apply revalidates `ManagedObjectCatalog.IsAuthorizedWriteTarget` and `ElevationService.CanModifyHive`.

`ServiceControlService` remains the only non-registry mutation path and stays off the Explore rail in this phase.

Do not import `codex/2.6.0` backends (`FirewallRuleChangeService`, `BitLockerLifecycleService`, `FeatureChangeService`, `PackageChangeService`, `ScheduledTaskChangeService`, `DomainAuthorization`, `RollbackBatchStore` as a new product surface).

## Presentation system

Next implementation uses WPF-UI **4.3.0 exact pin** for:

- main `FluentWindow` chrome (Mica / title bar)
- `NavigationView` or equivalent left rail
- `SymbolIcon`
- theme dictionaries bridged through `ThemeManager`

Do not use `FluentWindow` for modal dialogs in this slice. Startup, confirmation, and high-impact dialogs stay `Window` with WPP tokens until a later slice proves a ContentDialog host.

Visual tokens:

- square / 2–4px corners, not pills
- cards and setting bars are inset boxes; they do not bleed to the window edge
- Fluent icons in the rail and context bar
- keep a native `File / Edit / View / Settings / Help` menu; wire existing handlers; do not invent write verbs
- `ui:TitleBar` on the main FluentWindow with ShowClose/ShowMaximize/ShowMinimize true. Extending content into the title bar without TitleBar is a defect.
- hide Apply on non-writable objects

Do not merge WPF-UI theme dictionaries and the full legacy `AppStyles.xaml` as two competing systems. Bridge tokens once in `ThemeManager`.

Copy 2.6 *ideas* (icons, square inset cards). Do not copy 2.6 `MainWindow.xaml` wholesale.

## Identity and version

`Directory.Build.props` remains the only literal version. Live product version is **2.6.1**. Version `2.6.0` is burned on the archive branch `codex/2.6.0`.
