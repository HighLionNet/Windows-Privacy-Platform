# Start here

This archive is a portable, framework-dependent Windows desktop release of Windows Privacy Platform 2.6.2.

1. Install the .NET 8 Desktop Runtime for Windows x64 from Microsoft if needed.
2. Extract every file in this archive to a permanent folder that your user account can write to.
3. Run `WindowsPrivacyPlatform.exe`; do not run it while it is still inside the zip.
4. Start in **View-only** for device analysis. Choose **Administrator** only for curated registry Settings or a confirmed, fresh-snapshot service/task action. Registry options stay pending until one batch confirmation.
5. The first successful launch offers to create Desktop and Start Menu shortcuts. The app records the choice locally and does not ask again.

The release has no background service, driver, cloud account, telemetry, or automatic policy application. Removing the extracted folder uninstalls the program; shortcuts can then be deleted normally.

Windows may show a reputation warning for an unsigned community build. Check the release asset's SHA-256 file against the downloaded zip. Official signed builds additionally expose a valid Authenticode signature in the executable's Properties dialog.

Per-user changes are refused if Windows elevation uses a different administrator account, because that token's HKCU is not the initiating user's registry hive. Reopen the app as the account whose setting should change.
