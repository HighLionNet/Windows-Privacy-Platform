# AI Handoff — v2.0 (2026-08-09)

## Done
- P0 Unknown preservation across NavigationBuilder / SettingsQuery / PolicyPrecedenceResolver
- CategoryView: raw-value buttons; notes from DisplayLabel (FormatOptionNote)
- WritableTarget + deny-by-default PolicyChangeService
- Firewall write boundary
- Version 2.0.0 (Directory.Build.props, MainWindow.xaml, About, README)
- CLI removed from solution

## Next
1. Expand catalog policy ObjectIds to full pre-v2 set
2. Test project for Unknown / WritableTarget / precedence
3. Scan generation ID
4. CI + LICENSE + SECURITY.md

## Do not reintroduce
- Unknown → Not configured
- Numbered option buttons
- "Policy value 0." as the only note
- Write targets inferred from Observation alone
