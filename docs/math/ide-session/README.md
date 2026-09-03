# IDE Session — матмодель и аксиоматика (v0)

| | |
|---|---|
| **Status** | Draft (evolving with implementation) |
| **Date** | 2026-09-02 |
| **SSOT code** | `AIGuiders.Platform.Modeling.Ide.Session` (guiders-fsharp) |
| **Architecture** | [GUIDERS-ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) |
| **Ownership** | [GUIDERS-FSHARP-ADR-0004](../../adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) |
| **Hub (stable URL)** | [ide-session-axioms-v0.md](../ide-session-axioms-v0.md) |

Документ для обсуждения **законов** модели. F# — нотация; нормативны формулы и инварианты.

**Split by feature** (2026-09-02): монолит разбит на пакет; правь feature-файл, не дублируй.

---

## Карта пакета

| Файл | Тема | Бывшие § |
|------|------|----------|
| [01-core-graph.md](01-core-graph.md) | Сорта, G, policy, M, WF, capability attrs | 0–2.7, 3–4 |
| [02-invalidation.md](02-invalidation.md) | Scope invalidation, Σ_π refine | 5 |
| [03-frozen-snapshots.md](03-frozen-snapshots.md) | freeze, FTC, workspace projection | 2.8–2.8b |
| [04-jobs-lifecycle.md](04-jobs-lifecycle.md) | job template, lifecycle, OD axioms | 2.9, 6, 7 (intro) |
| [05-build-incremental.md](05-build-incremental.md) | MSBuild port, Γ_π, H, sniper emit | 7.2–7.4 |
| [06-transforms.md](06-transforms.md) | Refactor G→G', Hoare, Θ, Code Style | 2.10–2.11 |
| [07-revision-ledger.md](07-revision-ledger.md) | Δ-stream, Git pin, Timeline | 2.12 |
| [08-adaptive.md](08-adaptive.md) | Adaptive topology | 8 |
| [09-decisions.md](09-decisions.md) | Решения / open questions | 9 |
| [10-implementation.md](10-implementation.md) | Code map, example, evolution | 10–12 |
| [11-conformance.md](11-conformance.md) | Golden sessions, Hoare/ST executable proofs | 11 |

---

## Порядок чтения (новый leaf)

1. **01** → **02** → **04** (граф + invalidation + jobs)
2. **03** → **05** (snapshots + build)
3. **06** → **07** (transforms + ledger)
4. **09** → **10** → **11** (decisions + code + conformance)

---

*Обсуждение: правь аксиомы в feature-файлах; F# подстраивается под формулы, не наоборот.*

**Forge render:** GitHub не рендерит LaTeX в preview — human_view: [FORGE-ADR-0071](https://github.com/AI-Guiders/agent-forge/blob/main/design/FORGE-ADR-0071-math-markdown-block-renderer.md) (`docs/math/**` profile).
