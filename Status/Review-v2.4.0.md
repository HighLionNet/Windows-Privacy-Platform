# Technical Review — v2.4.0

Review date: 2026-08-27  
Baseline: v2.3.5 (`fdff181`)  
Scope reviewed: the complete baseline and final 143-file repository inventory: 1 solution, 8 project files, 92 C# files, 12 XAML files, 4 PowerShell scripts, the workflow and shared props, 22 Markdown documents, and the icon/PNG assets. The ignored `.idea` files were inventoried as local IDE state and were not treated as source. No application, DLL, or test assembly was launched, as required by the v2.4.0 brief.

## 1. Architecture summary

Windows Privacy Platform remains a .NET 8 WPF application with incremental separation between models, collection, interpretation, validation, logging, and presentation.

- `WindowsPrivacyPlatform.Models` owns catalog definitions, applicability, observations, navigation data, write-target contracts, evidence terminology, service filters, and product metadata.
- `WindowsPrivacyPlatform.Scanner` performs read-only Windows collection and binds observations to catalog definitions.
- `WindowsPrivacyPlatform.Validator` validates definitions before the UI consumes them.
- `WindowsPrivacyPlatform.KnowledgeBase` is the in-memory definition repository used during validation.
- `WindowsPrivacyPlatform.Core` contains small platform-independent utilities, now including bounded atomic local-state storage.
- `WindowsPrivacyPlatform.Logging` writes bounded local audit/auth/change logs.
- `WindowsPrivacyPlatform.App` is the WPF shell, settings workflow, system views, elevation gate, and the only registry mutation implementation.
- `WindowsPrivacyPlatform.Tests` contains deterministic unit and Windows host scaffolding. The test project was compiled but not executed in this review.

The authority model is deny-by-default. `ManagedObjectCatalog.Finalize` is the only place that attaches typed `WritableTarget` definitions, and `PolicyChangeService` now independently revalidates the runtime target against the compiled catalog immediately before a change.

## 2. Complete component and data-flow map

### Read path

1. `MainWindow.RunScanAsync` creates a cancellation source and calls `ScanService.RunScanAsync` off the UI thread.
2. `ScanService.RunScanCore` composes `InventoryScanner` with identity, capability, package, service, task, privacy, policy, and firewall collectors.
3. `InventoryScanner.Scan` passes one `CancellationToken` to every collector and returns a completed, warning, partial, cancelled, or failed `ScanResult`.
4. Collectors write only to `InventorySnapshot`. Registry readers use explicit hives/views; external inventory fallbacks use `SafeProcessRunner` with fixed absolute executables and structured arguments.
5. `InventoryStateBinder` binds snapshots to fresh clones of catalog definitions. `DynamicInventoryCatalog` creates read-only explorer objects with no writable target.
6. `ApplicabilityEvaluator` evaluates build, edition, component, and value applicability.
7. `SchemaValidator` validates the in-memory definitions, including duplicate IDs, duplicate write targets, content, narrative, and target contracts. `ScanService` fails closed by moving any invalid editable definition out of the Settings bucket.
8. `SettingsQuery`, `NavigationBuilder`, `PostureAssessment`, `EvidenceStateSemantics`, and `ServiceInspection` create presentation state without acquiring write authority.
9. WPF views render Overview, domain/category settings, System Explorer, Services, search results, and explicit detail disclosures.

### Write path

1. A user explicitly selects Modify. `ElevationService.TryEnterModifyMode` asks once before a Windows-owned UAC relaunch.
2. The elevated relaunch consumes `--authorize-modify` plus a bounded initiating-SID marker. A malformed marker is refused.
3. Category option buttons select a proposed value only; they do not write.
4. `CategoryView.ApplyPending_Click` creates at most 32 typed `PendingPolicyChange` records and calls `PolicyChangeService.TryApplyBatch`.
5. The service rejects duplicate IDs, non-registry targets, incomplete targets, noncatalog targets, unsupported values, wrong applicability, an unelevated machine target, and cross-account HKCU elevation.
6. The service pre-reads each exact hive/view/subkey/value, shows one batch confirmation, revalidates each request, writes with `Microsoft.Win32.RegistryKey`, and independently reads back the exact value and kind.
7. Each result is returned as `PolicyChangeOutcome`; a mismatch or read failure is never reported as success. Results are locally audited and a fresh scan is requested.
8. The elevated process exits after the confirmed batch. System Explorer and Services have no mutation callbacks.

### External process path

- `CapabilityCollector`: fixed `%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe` or `dism.exe` with fixed arguments.
- `PackageCollector`: fixed Windows PowerShell executable and fixed inventory scripts.
- `ScheduledTaskCollector`: fixed `%SystemRoot%\System32\schtasks.exe` query.
- `FirewallCollector`: fixed Windows PowerShell executable and a fixed read-only query.
- `SafeProcessRunner`: rejects nonabsolute/missing executables, uses `ArgumentList`, bounds argument count/length and captured output, sets a fixed working directory, and honors timeout/cancellation.
- `ElevationService`: launches only the resolved current executable with `runas` and fixed marker arguments.
- `AboutView`: opens only the assembly-authored repository URL through the shell.

### Local-file and release path

- Preferences and shortcut decision: `%LocalAppData%\WindowsPrivacyPlatform`, bounded and atomically replaced by `AtomicLocalFile`.
- Audit/auth/change logs: the same application-data root, sanitized and rotated at 2 MiB; no temporary-directory fallback.
- Release: `build-release.ps1` publishes to a repository-contained artifact directory, removes PDBs, rejects source/development/secret-like files, creates a file-hash manifest, verifies the ZIP statically, and writes SHA-256.

## 3. Existing defects

The v2.3.5 implementation had these observable defects:

- `DomainView` flattened small categories into setting links and `SearchResultsView` opened detail directly, violating Category → list → explicit detail.
- `CategoryView` performed a registry operation as soon as an option was clicked and had no pending comparison or batch confirmation.
- `ElevationService` restarted the whole UI and then asked for another Modify authorization, producing redundant permission/confirmation transitions.
- An over-the-shoulder administrator credential could make an HKCU target refer to the administrator account instead of the initiating standard user.
- `PolicyChangeService` trusted a complete runtime target without comparing every field to the compiled catalog definition.
- Registry observations did not retain value kind and sometimes blurred absence, denied access, and failed reads in UI binding.
- `SafeProcessRunner` accepted PATH-resolved executables and a single argument string, and captured output without a memory bound.
- Collector cancellation was not propagated consistently; several process calls used `CancellationToken.None`.
- Service evidence contained only name, startup, and state, with no useful filtering or neutral problem classification.
- Preference/shortcut writes were non-atomic. Audit logs could grow indefinitely and fall back to a shared temporary location.
- Release archives could include PDBs and had no entry-level package audit or manifest.
- Content validation did not reject missing option explanations, duplicated title/description copy, incomplete applicability, or duplicate writable registry targets.

## 4. Security findings

### Critical

None found in the reviewed source.

### High

- **Fixed — runtime target substitution:** `PolicyChangeService.TryApplyCore` accepted any complete `WritableTarget` carried by a `ManagedObject`. It now calls `ManagedObjectCatalog.IsAuthorizedWriteTarget`, comparing kind, hive, view, subkey, value, type, deletion/elevation flags, and value allowlist.
- **Fixed — cross-account HKCU mutation:** `ElevationService.CanModifyHive` now rejects per-user writes when UAC used a different account SID.
- **Fixed — process search-path/argument risk:** `SafeProcessRunner` and its callers now require fixed absolute executables and `ProcessStartInfo.ArgumentList`; arguments and output are bounded.

### Medium

- **Fixed — redundant elevation authorization:** the UAC relaunch marker is consumed once and a batch uses one elevated session, which exits after the operation.
- **Fixed — unbounded/sensitive logs:** `AuditLogger` rotates, sanitizes control characters, caps field length, redacts common secret assignments, and does not use `%TEMP%` as storage fallback.
- **Fixed — release data leakage:** packaging removes PDBs and rejects common source, development, key, certificate, and secret-like filenames before ZIP verification.
- **Remaining — no isolated IPC broker:** the elevated executable still hosts the WPF presentation while a Modify batch is prepared. It accepts no arbitrary paths or commands and exits after the batch, but a future release should split the compiled request executor into a narrowly authenticated broker. This limitation is visible in Modify messaging and is not represented as a password or UAC bypass.
- **Remaining — application-data ACL hardening:** files are stored under the current profile and constrained to that root, but the app does not install a custom Windows ACL or cryptographic tamper-evidence. The UI/documentation calls them local audit records, not immutable security logs.

### Low

- `AtomicLocalFile` defends lexical traversal and uses same-directory replacement, but does not explicitly open paths with reparse-point-resistant Win32 flags. The root is user-owned LocalAppData and no untrusted path is accepted from UI input.
- Service publisher evidence uses file version metadata and embedded-signature presence; it does not perform certificate-chain trust validation. `ServiceCollector` records this as evidence, never a malware verdict.
- The scanner still uses fixed PowerShell scripts for Windows surfaces without a practical direct managed API. Inputs are not user-controlled and execution is read-only, bounded, cancellable, and fixed-path.

### Informational

- GitHub Actions has repository-read permission for build/test and grants contents-write only to the tag/manual packaging job. Package signing values remain secrets and are not echoed intentionally.
- No background agent, service, scheduled task, driver, startup item, generic command runner, or credential capture exists.

## 5. Correctness and Windows-compatibility findings

- `PolicyCollector`, `PrivacyCollector`, `PolicyBinder`, and `PrivacyBinder` now preserve registry kind and explicit Present/NotConfigured/AccessDenied/Error states. Unexpected type is displayed as unknown evidence rather than coerced.
- All write reads and writes use one explicit `RegistryView`, exact hive/subkey/value, and exact `RegistryValueKindExpected`.
- Missing registry values are Not configured only after a successful source read; empty/failed evidence is Not observed, Access denied, Unknown, or Error.
- Catalog defaults now declare Windows 10/11 and minimum build 10240 when a definition omitted applicability metadata. This is a conservative catalog floor, not proof that every option exists on every build; per-value applicability still applies.
- Copilot coverage remains limited to policy mappings already present in the source. `CategoryContent` explicitly distinguishes the legacy Windows integration from the newer app and does not claim total disablement.
- Service enumeration is bounded to 20,000 items and relationship lists to 128. Collector failures remain partial evidence.
- The app is Windows-only WPF and requires .NET 8 Desktop Runtime x64 for the framework-dependent package.

## 6. UI/UX findings

- Domain pages now always present categories, including human category copy; they no longer flatten a small category into a detail route.
- Home findings and global Settings search results navigate to a category list with filter context and a highlighted card. Only “Open setting details” opens the detail view.
- Category cards show title, consequence, observed evidence, scope, applicability, options, pending comparison, and a collapsed technical disclosure.
- Proposed choices are visually and textually distinct from observed state. One “Apply pending” action handles the batch.
- The Services page is a compact top-level, read-only evidence view with literal filters for search, state, startup, publisher, and issue.
- Scan cancellation is explicit, last-good evidence is retained, and the header labels warning/partial/stale evidence.
- Existing centralized `AppStyles.xaml` supplies console colors and focus/selected states. Status always includes text, not color alone.
- One primary outer content scroll surface is retained. Service/settings cards avoid nested list scrollers. Very large settings lists are bounded by the catalog; Services is bounded but not virtualized, an honest performance limitation for unusually large service inventories.
- Dark theme is not implemented. Windows high-contrast certification and screen-reader certification require runtime/manual validation.

## 7. Test gaps

v2.4.0 adds deterministic coverage for settings-list navigation, distinct evidence states, service filtering/classification, malformed targets, runtime-target tampering, editable-copy validation, and local path escape. Existing tests cover catalog identity/applicability and the abstract pre-read/write/read-back contract.

Still requiring later executable/runtime test work:

- A true split-process broker/IPC contract, because no broker exists yet.
- UAC prompt counting and standard-user over-the-shoulder credential behavior.
- Real registry precedence/MDM override behavior and rollback on Windows SKUs.
- UI Automation checks for focus, selection, scrolling, high DPI, and screen readers.
- Fault injection for timeout/partial multi-setting registry writes at the WPF service boundary.
- Authenticode trust-chain verification and representative service trigger/start-failure APIs.
- Corrupted existing log rotation under concurrent writers.

The solution and test assembly were compiled. Tests were deliberately not executed because the task brief prohibited DLL/runtime testing.

## 8. Changes made for v2.4.0

- Version/catalog/CI branch metadata updated to v2.4.0.
- Category-first navigation, scoped search, highlighted results, category copy/counts, pending batches, and expanded detail copy.
- Central `EvidenceStateSemantics`, `CategoryContent`, `SettingsListTarget`, and `SettingOptionLanguage` terminology.
- Typed batch write results, exact catalog revalidation, single relaunch authorization, cross-account HKCU protection, and post-batch elevated-process exit.
- Registry observation status/kind preservation and stricter target/content/duplicate validation.
- Cancellation propagation and fixed-path, argument-safe, bounded process execution.
- Rich read-only service collection, filtering, and neutral evidence classification.
- Atomic preferences/shortcut state, bounded/redacted audit logs, and package manifest/archive audit.
- New unit/integration scaffolding and a manual QA checklist.

No new registry mapping was added. This was deliberate: the review found no additional mapping that could be authoritatively proven from repository evidence alone, and the brief forbids internet tweak-list expansion.

## 9. Items intentionally not changed and why

- BitLocker, UAC policy, arbitrary firewall rule, service, task, package, feature, and capability mutation remain excluded because they need recovery, multi-key coordination, or a different authority model.
- Existing real catalog entries were not renamed wholesale. Central category/option/explanation composition improves wording without silently changing policy semantics.
- No generic rollback profile was added. Deleting an allowed registry value remains the precise “Use Windows default” recovery where the target explicitly supports deletion.
- No dark theme, localization framework, installer, background component, telemetry, cloud account, or synthetic score was added.
- No new Copilot-app-local setting was invented. The UI states the limitation of the legacy policy.
- The WPF/.NET 8 stack was preserved; a framework migration would not improve the write authority model.

## 10. Issues that must remain visible to the user

- Not configured is not Disabled; Unknown, Not observed, Access denied, Unsupported, Stale, and Error are distinct.
- Organization policy or another security product may override a value. Success requires immediate typed read-back, but later enforcement can still change the state; the app rescans.
- Copilot’s legacy Windows policy does not prove that the newer Copilot app is disabled.
- Embedded signature presence and publisher metadata are evidence, not a trust or malware verdict.
- A per-user change is refused when elevation switches to another administrator account.
- Unsigned community builds may show Windows reputation warnings; users should verify the ZIP SHA-256.
- Audit records are local bounded records, not cryptographically tamper-proof event logs.
- UI accessibility, high-DPI behavior, UAC interaction, and real policy precedence still require the manual Windows matrix in `Status/Manual-QA-v2.4.0.md`.
