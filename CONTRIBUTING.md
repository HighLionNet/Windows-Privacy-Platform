# Contributing

Windows Privacy Platform treats correctness and bounded authority as product features. Changes should preserve uncertainty, keep discovered data separate from authorization, and avoid claims that are not supported by observed evidence or authoritative Windows documentation.

Standing constraints: [Status/AI_Handoff.md](Status/AI_Handoff.md). Authority model: [Status/Safety_Model.md](Status/Safety_Model.md).

## Development workflow

```powershell
dotnet restore .\Source\WindowsPrivacyPlatform.sln
dotnet build .\Source\WindowsPrivacyPlatform.sln -c Release --no-restore
dotnet test .\Source\WindowsPrivacyPlatform.sln -c Release --no-build
```

Before opening a pull request, also run `git diff --check` and `scripts\build-release.ps1` on Windows.

## Catalog work

Every managed object needs a stable ID, technical location, applicability, value semantics where known, complete plain-language narrative, and an explicit write decision. Prose validation intentionally rejects registry paths, service/task identifiers, raw object IDs, and internal observation phrasing.

A non-writable entry must state an `ExclusionReason`. A writable entry must use a complete typed `WritableTarget`; `DiscoveryMethod` and live inventory never authorize a write.

Adding a curated native target requires:

- A short, documented safety justification beside the allowlist entry.
- A fixed identifier and fixed operation shape—never a user-controlled shell command.
- Pre-read, confirmation, typed write, independent read-back, and local audit.
- One round-trip contract test per allowlist entry.
- A recovery hint where removal or disabling affects installed functionality.

Arbitrary firewall rules and non-curated inventory remain observation-only. Service runtime control, if touched, must stay inside `ServiceMutationPolicy`.

## Signing release output

`scripts\sign-release.ps1` signs only the published application executable when signing configuration exists. Set `WPP_SIGN_CERT_PATH` plus optional `WPP_SIGN_CERT_PASSWORD`, or set `WPP_SIGN_CERT_THUMBPRINT` for a certificate in the local-machine store. CI can construct the PFX path from the `WPP_SIGN_CERT_BASE64` repository secret. Never commit certificates, passwords, thumbprints, or generated release output.

## Pull requests

Keep changes reviewable, include regression tests for safety-sensitive behavior, update Safety_Model / Architecture / AI_Handoff when contracts change, and report actual build/test results with branch and commit. Do not add bulk-apply profiles, generic editors, scoring, application telemetry, or silent persistence.
