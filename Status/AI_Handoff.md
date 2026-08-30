# AI Handoff

## Current contract

- Four sections only: Privacy, Security, Network, Troubleshoot/Explore. Hub items are persistent and sit above the section rail.
- `Directory.Build.props` is the version source. The UI and catalog read assembly metadata through `ProductInfoReader`.
- Settings changes remain registry-only through `PolicyChangeService`: compiled target, supported raw value, pre-read, confirmation, typed write, independent value/kind read-back, audit.
- Dynamic inventory always has `WritableTarget == null`.
- Explore actions are separate: `InventoryChangeService` plus `ServiceMutationPolicy` or `TaskMutationPolicy`, fresh exact snapshot row, Administrator authority, one confirmation, live read-back, audit.
- Service actions are runtime start/stop/restart for sufficiently evidenced optional non-Microsoft services. Never change start type.
- Task actions are enable/disable for non-Microsoft paths only. Never create/delete/change `/TR`.
- DNS is layered evidence. Empty and failed evidence remain Unknown/Error. Browser or VPN app DNS is `ExternalApp`, never inferred from Windows DoH.
- Edge, WebView2, and default-browser presence are observations. WebView2 and Edge uninstall are out of scope.
- Unsigned executable hash drift is status-only. Signed high-impact execution requires a valid HighLionNet publisher chain. Catalog authorization integrity remains mandatory.

## UI routing

- Network: `network:dns`, `network:adapters`, `domain:Firewall`, `domain:RemoteAccess`.
- Explore: `explore:windows-services`, `explore:other-services`, `explore:windows-tasks`, `explore:other-tasks`, `explore:system-apps`, `explore:other-apps`, `explore:features`, `explore:firewall-rules`.
- Search and Dashboard still use `SettingsListTarget`; they do not auto-open a setting detail.
- Privacy category pages use Featured/All and outcome families based on `OutcomeConflictEngine.ConsentFamilies`.

## Do not regress

Do not add a fifth section, scores, bulk apply, user-built commands, AppX/DISM removal, firewall-rule CRUD, adapter DNS writes, service start-type changes, task deletion, BitLocker lifecycle, UAC master, or Edge/WebView2 uninstall. Do not route inventory actions through `PolicyChangeService`. Keep WPF-UI pinned at 4.3.0 and modal dialogs as standard `Window`.
