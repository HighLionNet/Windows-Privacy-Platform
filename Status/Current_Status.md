# Windows Privacy Platform
## Current Status — Version 1.6.2

**Last updated:** 2026-08-09  
**Current development version:** **1.6.2**  
**Previous:** 1.6.1 / 1.6  

---

## Trust contract (Modify writes)

1. **Pre-read** independent system value before any change.  
2. User confirms path + current + intended.  
3. Write (DWORD/string/delete) under elevated token, Registry64 view.  
4. **Mandatory read-back** (up to 3 attempts).  
5. **Success only if read-back matches intended state.**  
6. UI refresh only after verified success (or rescan after hard failure for honesty).  
7. Full audit trail: BEFORE / AFTER / VERIFIED or VERIFY_FAIL in `changes.log`.  

PolicyCollector and PolicyChangeService both use **RegistryView.Registry64** and invariant numeric formatting so scan and write cannot disagree on view or string form.

Non-concrete DiscoveryMethod paths (`ServiceController:`, summary aggregates) are refused — never written.

---

## 1.6.2 changes

- **Catalog path purge:** All ConsentStore `DiscoveryMethod` values are now concrete  
  `HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\<capability>\Value`.  
- ContentDelivery path expanded to full concrete location.  
- Firewall service / logging summary remain observation-only (not writable).  
- SchemaVersion / ConfidenceSource bumped to Catalog-v1.6.  

---

## UI

- Category cards: name, short blurb, path/type, current, value buttons  
- Detail: status + single explanation paragraph  
- Elevation: UAC relaunch + session authorize  

---

## Build

```powershell
cd "C:\Windows Privacy Platform\repo"
git pull origin main
dotnet publish ".\Source\WindowsPrivacyPlatform.App\WindowsPrivacyPlatform.App.csproj" -c Release -r win-x64 --self-contained false -o "C:\Windows Privacy Platform\bin"
```
