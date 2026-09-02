# GUIDERS-FSHARP-ADR-0006: ADR lifecycle and verifiable facts

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-09-02 |
| **Tags** | #guiders #adr #governance #conformance #ggl |
| **Related** | [status-lifecycle.md](./status-lifecycle.md) · [Cascade IDE status-lifecycle](https://github.com/KarataevDmitry/cascade-ide/blob/main/docs/adr/status-lifecycle.md) · [ide-session §11](../math/ide-session/11-conformance.md) · [card GGL](https://github.com/AI-Guiders/agent-notes/blob/main/knowledge/work/projects/aiguiders-open/guiders-federation/card-ggl-federation-language-v0.md) |

## Context

Cascade IDE maintained a two-level ADR model: **Accepted** (decision) vs **Implemented** (code). Guiders Federation dropped this discipline during the Modeling/Execution split. Without it, ADRs become prose-only and agents treat "merged doc" as "shipped behavior".

## Decision

Adopt the Cascade lifecycle for all Guiders federation ADRs:

```text
Draft → Accepted → Accepted · Implemented
                  ↘ Superseded / Deprecated
```

Optional ADR body structure for normative decisions:

```text
facts:
  golden: GS*
  hoare: ...
  wf: WF*
end facts
prose:
  ## Context / Decision / Consequences (human)
end prose
```

| Transition | Gate |
|------------|------|
| → **Accepted** | Review; decision recorded |
| → **Accepted · Implemented** | `Sat(facts)` on `main` OR explicit evidence row in ADR registry |
| Stays **Accepted** only | Strategy, partial strangler, or open ports |

**Implemented** is never inferred from Accepted alone.

## Consequences

- [README.md](./README.md) registry is SSOT for implementation column.
- Golden sessions in `Modeling.Ide.Session` are the first facts backend.
- GGL / `.ggpl` will treat `based on adr:…` with normative vs informative link strength (future).

## Non-goals

- Retrofitting all historical platform ADRs in one PR (use [platform registry](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/adr-registry-v1.md)).
- Mandatory facts block for every ADR today — start with federation + conformance ADRs.
