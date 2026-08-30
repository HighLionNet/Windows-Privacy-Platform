# Architecture

## Product shell

The product has four exclusive sections: Privacy, Security, Network, and Troubleshoot/Explore. Hub is persistent navigation, not a fifth section, and appears at the top of the rail: Dashboard, Conflicts, Knowledge Explorer, App Settings, About. A rule separates Hub from the active section title and categories.

The main window remains WPF-UI 4.3.0 `FluentWindow` with `ui:TitleBar` and visible minimize, maximize, and close buttons. File, Edit, View, Settings, and Help live in the thin caption row. The identity and context rows are compact, header search consumes its `*` column, and the complete ComboBox hit target opens the dropdown. Dialogs remain standard WPF `Window` instances. Cards are inset with 2–4 px corners; selected section state uses a shadow.

Privacy keeps the existing `HubNavigation.PrivacyOrder` in one quiet list. Category pages offer Featured and All. ConsentStore and AppPrivacy pairs use `OutcomeGrouping`, whose IDs come from `OutcomeConflictEngine.ConsentFamilies`; the two evidence layers remain distinct.

Network categories are DNS & name resolution, Adapters & LAN, Firewall, and Remote access. Troubleshoot/Explore partitions every live object into one page: Windows services, Other services, Windows tasks, Other tasks, System apps, Other apps, Features & capabilities, or Firewall rules. There is no duplicate System Explorer mega-list.

## Catalog and settings pipeline

`Directory.Build.props` is the application-version source. `ProductInfoReader` supplies the runtime version to the UI and catalog schema; no parallel source literal is maintained.

`ManagedObjectCatalog` is the curated definition source. Finalization applies semantics, explicit authorization, taxonomy, narrative, applicability, and exclusion decisions. `PolicyCollector` reads exact registry sources. `InventoryStateBinder` binds observations. `PolicyChangeService` accepts only complete catalog-backed registry targets, performs pre-read and confirmation, writes the typed value, independently reads value and kind, and audits the result.

`DynamicInventoryCatalog` converts scan rows to presentation objects with `WritableTarget == null`. It is never an authorization source.

## Inventory and DNS pipeline

`ScanService` composes fail-soft collectors and keeps a separate last-good completed result. Canceled or failed attempts cannot replace the snapshot used by actions.

`NetworkingCollector` uses `System.Net.NetworkInformation`, read-only TCP/IP and NRPT registry evidence, `GetBestInterface`, and fully qualified `System32\nslookup.exe` with a fixed query name and observed resolver addresses. It produces `DnsResolutionSnapshot` with distinct evidence states. `BrowserInventoryCollector` observes Edge, WebView2, and the current user's HTTPS default association without turning WebView2 into an uninstall target.

`InventoryChangeService` owns the second mutation class. It revalidates freshness and exact snapshot identity, delegates bounded service runtime work to `ServiceControlService`, and uses fully qualified `System32\schtasks.exe` only for task query/change/query. `ServiceMutationPolicy` and `TaskMutationPolicy` remain pure source-controlled gates.

## Integrity boundaries

`BinaryIntegrityGuard` always reports SHA-256. Unsigned hash drift is status-only. Signed builds must pass Windows Authenticode verification and identify HighLionNet as publisher before high-impact Settings are allowed. `HighImpactStepUpService` validates that the supplied Windows credential token is an administrator and zeroes managed and native credential buffers. `ManagedObjectCatalog.HasValidAuthorizationHash` continues to protect the Settings allowlist independently.
