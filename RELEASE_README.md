# Start here

This archive is a portable, framework-dependent Windows desktop release.

1. Install the .NET 8 Desktop Runtime for Windows x64 from Microsoft if needed.
2. Extract every file in this archive to a permanent folder that your user account can write to.
3. Run `WindowsPrivacyPlatform.exe`; do not run it while it is still inside the zip.
4. Start in **Inspect** for read-only device analysis. Choose **Modify** only when you intend to make one explicitly confirmed change.
5. The first successful launch offers to create Desktop and Start Menu shortcuts. The app records the choice locally and does not ask again.

The release has no background service, driver, cloud account, telemetry, or automatic policy application. Removing the extracted folder uninstalls the program; shortcuts can then be deleted normally.

Windows may show a reputation warning for an unsigned community build. Check the release asset's SHA-256 file against the downloaded zip. Official signed builds additionally expose a valid Authenticode signature in the executable's Properties dialog.
