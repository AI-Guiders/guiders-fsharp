# GUIDERS-FSHARP-ADR-0006: ADR lifecycle as Correspondence extension

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-09-02 |
| **Tags** | #guiders #adr #correspondence #governance #conformance #ggl |
| **Related** | [GUIDERS-ADR-0028](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0028-documentation-guild-correspondence-family.md) · [status-lifecycle.md](./status-lifecycle.md) · [correspondence-kinds.v1.json](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/wire/correspondence/correspondence-kinds.v1.json) · [ide-session §11](../math/ide-session/11-conformance.md) · [card GGL](https://github.com/AI-Guiders/agent-notes/blob/main/knowledge/work/projects/aiguiders-open/guiders-federation/card-ggl-federation-language-v0.md) |

## Context

Cascade IDE maintained a two-level ADR model: **Accepted** (decision) vs **Implemented** (code). Guiders Federation dropped this discipline during the Modeling/Execution split.

**This ADR is not a parallel governance stack.** Lifecycle is a **Correspondence concern**: normative doc fragments ↔ code/graph regions ↔ conformance evidence. Same hyperlane as [GUIDERS-ADR-0028](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0028-documentation-guild-correspondence-family.md) — layer **L1′** (lifecycle state) + **L3** (facts).

## Decision

### 1. Lifecycle tags (first + second level)

```text
Draft → Accepted → Accepted · Implemented
                  ↘ Superseded / Deprecated
```

| Transition | Gate |
|------------|------|
| → **Accepted** | Review; decision recorded |
| → **Accepted · Implemented** | `Sat(facts)` on `main` OR explicit evidence row in ADR registry |
| Stays **Accepted** only | Strategy, partial strangler, or open ports |

**Implemented** is never inferred from Accepted alone.

### 2. Correspondence kinds (wire)

| Kind | Layer | Edge | Example |
|------|-------|------|---------|
| `normates` | L1 + L1′ | ADR fragment → code zone / graph scope | `based on adr:GUIDERS-FSHARP-ADR-0004` |
| `verified_by` | L3 + L1′ | golden / hoare → ADR `facts:` block | `facts: golden: GS1..GS6` |
| `implements` | L2 | symbol → ADR obligation | existing reverse scan |
| `documents` | L1 | forward map | workspace.toml ADR line |

Wire catalog: [`wire/correspondence/correspondence-kinds.v1.json`](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/wire/correspondence/correspondence-kinds.v1.json).

### 3. ADR body structure (normative decisions)

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

`verified_by` edges connect `facts:` rows to golden sessions in `Modeling.Ide.Session` (first backend). MdLinker / Correspondence.Resolve treat broken `normates` / `verified_by` like broken bracket anchors.

### 4. GGL / `.ggpl`

```text
module Session based on adr:GUIDERS-FSHARP-ADR-0004 { ... }
```

`based on` = `normates` edge with strength **normative** (vs informative `related`).

## Consequences

- LC ADR folds into **Documentation.Correspondence.*** — no second doc↔code resolver.
- [README.md](./README.md) registry + Correspondence index share implementation column.
- Pilot (M3): extend `Correspondence.Reverse` to scan `adr:` + `facts:` blocks; golden id → test name.

## Non-goals

- Retrofitting all historical platform ADRs in one PR ([adr-registry-v1](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/adr-registry-v1.md)).
- Mandatory `facts:` block on every ADR before Correspondence pilot ships.
