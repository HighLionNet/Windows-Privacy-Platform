# Review — v2.5.0

## Delivered in this source release

- View-only/Admin wording throughout the UI, equal startup choices, CredUI password verification, bounded authorization attempts, and UAC self-relaunch when an elevated token is needed.
- Apply keeps the authorized Admin process open. Leaving Admin confirms the operator and relaunches View-only through the Windows shell to drop the elevated token.
- Identity-first Overview, linked posture tiles, high-risk-only findings, compact settings cards, structured setting details, labeled filters, and virtualized Services/System Explorer lists.
- App Settings with session lifetime, default dialog highlight, four live themes, window/scanning preferences, local-data disclosure, executable hash, signing state, and authorization-catalog hash.
- Single-instance focus, bounded command-line parsing, authorization-table integrity checks, restricted local-data ACLs, previous-hash audit chaining, and best-effort Windows process mitigations.

## Verification performed

- Release solution build and automated tests pass on Windows 11 x64.
- The real Release GUI was inspected in Windows Light, Slate Light, Navy Dark, and Ember Dark at 200% display scaling.
- Startup, Overview, settings cards, App Settings, Services, maximized layout, and a restored 1000×600-DIP window were inspected.

## Honest limits

- WPP verifies an administrator password with CredUI, but Windows UAC still follows the machine's configured consent policy.
- The Admin presentation and writer share one process; there is no elevation broker or IPC service in v2.5.0.
- Child-process blocking is not enabled globally because WPF's per-process mitigation has no safe executable allowlist and WPP requires fixed collectors plus token-dropping self-relaunch. Fixed paths, structured arguments, bounded output, and command-line rejection remain enforced instead.
- Win32k lockdown is not enabled because WPF depends on Win32k. DEP, ASLR, strict-handle, extension-point, and image-load policies are applied best-effort where Windows supports them.
- Audit previous-hash chaining exposes many edits or truncations but does not make a local file immutable.
- This run produced a framework-dependent Release executable; installer, archive, signing, and public release distribution were intentionally out of scope.
