# Windows Privacy Platform
## Current Status — Version 1.6

**Last updated:** 2026-08-09  
**Current development version:** **1.6**  
**Previous:** Version 1.5  
**Runtime target:** Windows 11; net8.0-windows  

---

## What v1.6 delivers

### Category pages
- Taller setting cards with **name + short one-line explanation + current value**.
- **Value buttons** (0 / 1 / mapped labels / Not configured) are the change controls.
- Changes are made from the category list, not the detail page.

### Setting detail
- Minimal: **Status** (Current / Effective / Source) + one organized **Explanation** paragraph.
- No options tables, layer dumps, or related clutter on the detail page.

### Modify mode (real writes)
- `ElevationService` offers **UAC relaunch** when not elevated.
- Session authorization after elevation.
- `PolicyChangeService` writes registry values for catalog settings (DWORD or string; clear = delete value).
- Every write requires confirmation and is logged to `changes.log`.
- Auth decisions log to `auth.log`.

### Safety
- Inspect remains default.
- Non-registry targets (firewall service, wildcards) are refused with a clear message.
- Writes only when `IsModifyAuthorized` (elevated + confirmed).

---

## Build / run

```powershell
cd "C:\Windows Privacy Platform\repo"
git pull origin main
dotnet publish ".\Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj" -c Release -r win-x64 --self-contained false -o "C:\Windows Privacy Platform\bin"
Start-Process "C:\Windows Privacy Platform\bin\WindowsPrivacyPlatform.exe"
```

For Modify: select **Modify** → accept UAC relaunch if needed → authorize session → use value buttons on a category page.
