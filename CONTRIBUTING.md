# Contributing

Windows Privacy Platform treats correctness and bounded authority as product features. Changes should preserve uncertainty, keep discovered data separate from authorization, and avoid claims that are not supported by observed evidence or authoritative Windows documentation.

## Branches

- `main` is always the current product. Clone and open pull requests against `main`.
- Each shipped version has a frozen branch named `vX.Y.Z` (for example `v2.6.1`, `v2.5.1`). Do not rewrite those branches after they exist.
- When `main` moves, fast-forward the live version branch to the same commit. Do not keep a second living copy of the same version.
- Feature and docs branches are temporary. Delete them after the version branch exists.
- Version `2.6.0` is retired. Do not recreate it and do not restore `codex/2.6.0` write backends.
- `Directory.Build.props` is the only literal version string.

Public markdown (`README.md`, `SECURITY.md`, `CONTRIBUTING.md`, and `Status/*` contracts) always describes the version on `main`.

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
- A fixed identifier and fixed operation shape — never a user-controlled shell command.
- Pre-read, confirmation, typed write, independent read-back, and local audit.
- One round-trip contract test per allowlist entry.
- A recovery hint where removal or disabling affects installed functionality.

BitLocker lifecycle, User Account Control master changes, arbitrary firewall rules, and non-curated inventory remain observation-only by design.

## Signing release output

`scripts\sign-release.ps1` signs only the published application executable when signing configuration exists. Set `WPP_SIGN_CERT_PATH` plus optional `WPP_SIGN_CERT_PASSWORD`, or set `WPP_SIGN_CERT_THUMBPRINT` for a certificate in the local-machine store. CI can construct the PFX path from the `WPP_SIGN_CERT_BASE64` repository secret. Never commit certificates, passwords, thumbprints, or generated release output.

## Pull requests

Keep changes reviewable, include regression tests for safety-sensitive behavior, update public and engineering documentation when contracts change, and report actual build/test/publish results. Do not add bulk-apply profiles, generic editors, scoring, application telemetry, or silent persistence.

Engineering contracts live in `Status/Safety_Model.md`, `Status/Architecture.md`, and `Status/AI_Handoff.md`.
