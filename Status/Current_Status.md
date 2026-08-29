# Current Status

**Release state:** v2.6.1 is the live source line on `v2.6.1`; its product base remains v2.5.1 @ `b06efc9`.
**Contract state:** the four-section hub contract is recorded on `wpp/docs/four-section-hub`; the shell delta is implemented from contract commit `5cb1673`.
**Not live:** `codex/2.6.0` (Fluent + expanded writes). Archive only. Version 2.6.0 is not reused.

## Shipped behavior (v2.6.1 code)

WPF policy hub with four exclusive sections: Privacy, Security, Network, and Troubleshoot/Explore. The main window uses WPF-UI 4.3.0 chrome while dialogs remain standard `Window` instances styled with WPP tokens. Curated registry Settings retain verified write. System Explorer and Services remain inventory. Firewall writes remain the twelve profile properties. Optional non-Microsoft service runtime start/stop/restart remains unchanged. Security Center antivirus registrations are observed fail-soft and never authorize writes. Distinct evidence states remain visible. No synthetic score.

## Four-section product shape

Four sections — Privacy, Security, Network, Troubleshoot/Explore — selected from a thicker context bar. Left rail lists only the active section’s categories under a colored section title. Dashboard, Conflicts, Knowledge, App Settings, About, and session mode persist.

Troubleshoot/Explore is read-only this phase. WPF-UI 4.3.0 is allowed for main chrome and icons only. New write backends are not approved.

See `Status/Safety_Model.md`, `Status/Architecture.md`, `Status/Work/four-section-hub.md`.
