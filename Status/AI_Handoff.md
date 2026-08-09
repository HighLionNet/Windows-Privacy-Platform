# AI Handoff — v2.0 in progress (2026-08-09)

## Done this session
- P0 Unknown preservation
- CategoryView raw-value-only buttons (no numbering)
- WritableTarget model + deny-by-default PolicyChangeService
- Firewall write boundary
- Version 2.0.0 + About rewrite
- CLI removed from solution
- Catalog WritableTarget attachment for non-firewall concrete paths

## Next priorities (in order)
1. Confirm Release build on Windows after pull
2. Add test project: Unknown semantics, IsConfigured, WritableTarget gate, precedence
3. ScanService concurrent-scan protection
4. SafeProcessRunner for collectors
5. CapabilityCollector state model
6. GitHub Actions build/test
7. LICENSE + SECURITY.md + CONTRIBUTING

## Do not
- Reintroduce Unknown → Not configured
- Invent 0/1 options without ValueSemantics
- Write firewall rules via registry
- Infer write targets from Observation alone
