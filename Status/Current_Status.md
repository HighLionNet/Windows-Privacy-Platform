# Current Status

## Release state

The v2.6.2 implementation is on branch `v2.6.2`, based on `main` commit `d51088935092f5f0f565b002bf1fc228e8de9203`. When verification and packaging succeed, `main` and `v2.6.2` must point to the same commit.

## Implemented

- Compact WPF-UI shell: caption-row File/Edit/View/Settings/Help, smaller identity/context chrome, full-width search, full-control ComboBox hit target, Hub-first rail, and section-only category lists.
- Privacy Featured/All pages and ConsentStore/AppPrivacy outcome grouping without deleting catalog entries.
- Edge presence/version/path, WebView2 presence/version/path with explicit runtime label, observed default-browser ProgId, Windows Settings handoffs, and documented Edge first-run/sidebar/shopping/diagnostic-data/feedback policy rows.
- Layered DNS snapshot: active interfaces and addresses, resolver source evidence, NRPT, Windows DoH policy, preferred route, VPN/tunnel participation, observed-resolver fixed-name probes, and browser/VPN ExternalApp boundaries.
- Network rail split into DNS, adapters, firewall, and remote access.
- Explore split into eight non-overlapping pages. Optional non-Microsoft services expose start/stop/restart. Non-Microsoft tasks expose enable/disable. All other inventory remains observe-only.
- Fresh-snapshot checks, exact identifiers, source-controlled service/task policy gates, Administrator authority, one confirmation, independent read-back, and local audit for inventory actions.
- Last-good scan preservation after failed/canceled attempts.
- Unsigned hash drift no longer blocks high-impact Settings. Signed builds require a valid HighLionNet publisher chain. Step-up credentials are administrator-checked and securely zeroed.
- Focused `V262AcceptanceTests` plus updated four-section tests.

## Permanently out of scope for this line

No debloat/uninstall mode, AppX or provisioned-package removal, DISM feature/capability changes, Edge/WebView2 uninstall, adapter DNS writes, task delete/create/action editing, service start-type changes, firewall-rule CRUD, BitLocker lifecycle, UAC master, bulk apply, generic command box, background service, CLI, or telemetry.
