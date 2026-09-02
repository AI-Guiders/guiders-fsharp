# GUIDERS-FSHARP-ADR-0004: Ide Session modeling ownership

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-09-02 |
| **Tags** | #guiders #fsharp #ide #solution #lifecycle #modeling |
| **Related** | [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) · [GUIDERS-ADR-0063](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md) · [GUIDERS-FSHARP-ADR-0002](./GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) · [GUIDERS-FSHARP-ADR-0005](./GUIDERS-FSHARP-ADR-0005-federation-reframe-cdp-features.md) · [GUIDERS-ADR-0061](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md) |

## Decision

Federation **IDE Solution Session** IR (graph, lifecycle phases, orchestration policy) is owned by:

```text
AIGuiders.Platform.Modeling.Ide.Session       — F# SSOT (guiders-fsharp)
AIGuiders.Platform.Modeling.Ide.Session.Ports — parser traits (slnx, tsconfig, …)
```

`AIGuiders.Platform.Execution.Ide.Session` (guiders-platform) hosts the orchestrator runtime only.

See [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) for full normative model.

**Math (axioms):** [ide-session/](../math/ide-session/README.md) (hub: [ide-session-axioms-v0.md](../math/ide-session-axioms-v0.md)) — evolving formal layer; F# is notation.

## Consequence for this repo

- New language = new `ProjectKind` + adapter — modeled here, not in CDP warm hooks.
- `DotNetWorkspace.Core` is a **port**, not session SSOT.
- LRC adapters consume `ICompilerServices` from orchestrator — they do not own project lifecycle.
