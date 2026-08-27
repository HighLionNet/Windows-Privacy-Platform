# Manual QA Checklist — v2.4.0

This checklist is prepared for a later runtime pass. It was not executed during the source-only implementation task.

## Platform matrix

- [ ] Supported Windows 10 build, x64, local account and Microsoft account.
- [ ] Current Windows 11 build, x64, local account and Microsoft account.
- [ ] Home edition: unavailable Pro/Enterprise options are informational, never editable.
- [ ] Pro, Enterprise, or Education: policy applicability and organization-managed wording are accurate.
- [ ] Standard user with same-account consent elevation.
- [ ] Standard user with over-the-shoulder administrator credentials: HKCU changes are refused; HKLM batch remains catalog-bound.
- [ ] Administrator with filtered token and UAC enabled.
- [ ] Offline device; no collector or UI assumes internet access.

## Evidence and failure cases

- [ ] Missing policy key is Not configured, not Disabled or Unknown.
- [ ] Present 0/1/enumerated values show correct plain-language effects.
- [ ] Unexpected value remains Unknown and is not selected optimistically.
- [ ] Unexpected registry kind is reported distinctly and cannot be changed as if valid.
- [ ] Access denied remains Access denied; failed read remains Error/Not observed.
- [ ] Organization policy override is visible after rescan and not claimed as a permanent change.
- [ ] Partial/cancelled scan retains last-good data and marks it stale/partial as applicable.

## Primary workflow

- [ ] Every domain click opens a category index; every category opens a settings list.
- [ ] Global search result opens the correct category, preserves filter text, and highlights the result without opening detail.
- [ ] Home finding opens the relevant category and highlights the item.
- [ ] “Open setting details” is the only normal route to detail.
- [ ] Category filter handles 0, 1, and many matches; Escape/global search behavior stays predictable.
- [ ] Observed and proposed values remain visibly distinct.
- [ ] Selecting an option does not write before Apply pending and confirmation.
- [ ] One batch produces one UAC transition and one confirmation; elevated process exits afterward.
- [ ] Cancelled batch writes nothing. Partial failure identifies unverified items and a later retry succeeds.
- [ ] Restart/sign-out/app-restart/policy-refresh expectations are visible when cataloged.

## Services and explorer

- [ ] Services page is explicitly read-only and exposes no start/stop/configure action.
- [ ] Name/description/path/tag search and state/startup/publisher/issue filters compose correctly.
- [ ] Missing executable, stopped automatic, denied access, configuration issue, and unknown evidence use neutral wording.
- [ ] Microsoft/non-Microsoft is not presented as safe/malicious.
- [ ] Large service list remains responsive and scrolls in the primary page surface.
- [ ] System Explorer remains read-only for services, tasks, packages, capabilities, features, and firewall rules.

## Interaction and accessibility

- [ ] 100%, 150%, 200%, and mixed-monitor DPI.
- [ ] Minimum window, typical laptop window, maximized, and ultrawide window.
- [ ] Keyboard-only navigation, Enter/Space activation, Ctrl+F, F5/Ctrl+R, Escape, Tab order, and visible focus.
- [ ] Screen reader announces current mode, scan status, setting/evidence label, proposed option, and disabled reason.
- [ ] Windows high-contrast themes retain selected/hover/focus/state distinctions without color alone.
- [ ] Mouse wheel, touchpad, Page Up/Down, Home/End, and return from detail keep predictable scrolling.
- [ ] Tooltips add useful action/unavailability context and do not carry essential-only information.

## Local state and release

- [ ] Corrupted/oversized window preferences are rejected without unsafe bounds or crash.
- [ ] Audit logs rotate, redact test `password=`, `token=`, and authorization headers, and do not grow without bound.
- [ ] Shortcut choice is atomically saved beneath LocalAppData.
- [ ] Unsigned and optionally signed ZIPs pass `scripts/verify-release.ps1`.
- [ ] ZIP contains no PDB/source/key/certificate files; manifest and SHA-256 match.
- [ ] Fresh regular-user download/extract starts from a writable folder with .NET 8 Desktop Runtime installed.
