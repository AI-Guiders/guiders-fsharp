# IDE Session — матмодель и аксиоматика (v0)

| | |
|---|---|
| **Status** | Draft (evolving with implementation) |
| **Date** | 2026-09-02 |
| **SSOT code** | `AIGuiders.Platform.Modeling.Ide.Session` |
| **Package** | **[ide-session/](ide-session/README.md)** — split by feature |
| **Architecture** | [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) |
| **Ownership** | [GUIDERS-FSHARP-ADR-0004](../adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) |

Монолит **разбит по фичам** → [ide-session/README.md](ide-session/README.md).

## Быстрая навигация

| Фича | Файл |
|------|------|
| Core graph, WF | [01-core-graph.md](ide-session/01-core-graph.md) |
| Invalidation, Σ_π | [02-invalidation.md](ide-session/02-invalidation.md) |
| Frozen snapshot, FTC | [03-frozen-snapshots.md](ide-session/03-frozen-snapshots.md) |
| Jobs, lifecycle | [04-jobs-lifecycle.md](ide-session/04-jobs-lifecycle.md) |
| Build, sniper emit | [05-build-incremental.md](ide-session/05-build-incremental.md) |
| Refactor, Hoare, style | [06-transforms.md](ide-session/06-transforms.md) |
| Δ-stream, Timeline | [07-revision-ledger.md](ide-session/07-revision-ledger.md) |
| Adaptive | [08-adaptive.md](ide-session/08-adaptive.md) |
| Decisions | [09-decisions.md](ide-session/09-decisions.md) |
| Implementation | [10-implementation.md](ide-session/10-implementation.md) |

---

*Stable link для ADR/Forge: этот файл. Нормативное содержание — в `ide-session/*.md`.*
