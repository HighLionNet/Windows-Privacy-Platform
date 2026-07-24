# Windows Privacy Platform
## Architecture

**Applies to:** **Version 1.0**  
**Last updated:** 2026-07-24  
**Document role:** Engineering architecture reference. Do not redesign without explicit approval.

---

## 1. Design goals

- Models free of OS I/O  
- Discovery isolated in Scanner collectors  
- Catalog / ValueSemantics own Windows meaning  
- Precedence only in PolicyPrecedenceResolver  
- CLI / TUI / **App** are presentation hosts only  
- Unknown preserved  

---

## 2. Solution structure

```
Source/
  WindowsPrivacyPlatform.sln
  WindowsPrivacyPlatform.Models/
  WindowsPrivacyPlatform.Core/
  WindowsPrivacyPlatform.Logging/
  WindowsPrivacyPlatform.KnowledgeBase/
  WindowsPrivacyPlatform.Validator/
  WindowsPrivacyPlatform.Scanner/
  WindowsPrivacyPlatform.CLI/
  WindowsPrivacyPlatform.App/          # v1.0 WPF presentation
```

Dependency direction:

```
Models → Core → Logging → KnowledgeBase → Validator → Scanner → CLI
                                                              ↘ App
```

App and CLI both depend on Scanner; neither depends on the other.

---

## 3. Presentation contract

UI consumes only:

- `MachineOverview`
- `SettingsQuery`
- `NavigationBuilder` / `SettingDetailView`
- `SettingExplanation`
- `ManagedObject` observation fields already bound by Scanner

`ScanService` (App) mirrors CLI composition. No registry logic in App.

---

## 4. Safety architecture

No writes; no elevation; fail-soft collectors; untrusted inventory never executed.

---

## 5. Extension checklist

- [ ] Read-only?  
- [ ] No elevation?  
- [ ] Models free of OS I/O?  
- [ ] Meanings in catalog ValueSemantics?  
- [ ] Precedence only in PolicyPrecedenceResolver?  
- [ ] UI free of business decisions?  
- [ ] Unknown preserved?  
