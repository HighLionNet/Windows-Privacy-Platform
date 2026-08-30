# Current Status

**Release state:** 2.6.1 is the live product on `main`. Frozen snapshot: `v2.6.1`. Product parent remains v2.5.1 @ `b06efc9`.
**Not live:** version 2.6.0 is retired. Do not restore its native write backends.

## Shipped behavior (2.6.1)

WPF policy hub with four exclusive sections: Privacy, Security, Network, and Troubleshoot/Explore. The main window uses WPF-UI 4.3.0 chrome. Dialogs remain standard `Window` instances styled with WPP tokens. Curated registry Settings retain verified write. Firewall writes remain the twelve profile properties. Security Center antivirus registrations are observed fail-soft and never authorize writes. Distinct evidence states remain visible. No synthetic score.

Safety_Model for this phase: Troubleshoot/Explore is read-only (scan, native handoff, fixed probes). Live inventory cannot gain a `WritableTarget`.

See `Status/Safety_Model.md` and `Status/Architecture.md`.
