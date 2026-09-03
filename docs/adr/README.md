# Architecture Decision Records — guiders-fsharp

**Lifecycle:** [status-lifecycle.md](./status-lifecycle.md) (Accepted vs Implemented).

## Index

| ADR | Title | Status | Implementation notes |
|-----|-------|--------|----------------------|
| [0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) | GDL spine ownership | Accepted | Modeling mirror in progress |
| [0002](./GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) | Modeling guild F# ownership | Accepted · In progress | Wave B–D rename ongoing |
| [0003](./GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md) | Model extraction matrix | Accepted | Roadmap; partial packages shipped |
| [0004](./GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) | Ide Session modeling SSOT | Accepted · Implemented | `Modeling.Ide.Session` v0: graph, WF, refactor, orchestrator |
| [0005](./GUIDERS-FSHARP-ADR-0005-federation-reframe-cdp-features.md) | Federation reframe CDP | Accepted · In progress | CDP `cdp_ide_session_scene` dogfood; Correspondence next |
| [0006](./GUIDERS-FSHARP-ADR-0006-adr-lifecycle-verifiable-facts.md) | ADR lifecycle as Correspondence | Accepted | Correspondence L1′/L3; kinds in wire JSON |
| [0007](./GUIDERS-FSHARP-ADR-0007-open-build-ssot-ftc-correspondence.md) | Open build SSOT — FTC + Correspondence | Draft | P0 F# first-class; MSBuild as legacy port |

## Related

- Math: [ide-session axioms](../math/ide-session-axioms-v0.md)
- Platform: [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md)
