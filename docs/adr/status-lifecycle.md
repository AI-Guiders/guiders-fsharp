# ADR status lifecycle (Guiders Federation)

**Origin:** [Cascade IDE status-lifecycle](https://github.com/KarataevDmitry/cascade-ide/blob/main/docs/adr/status-lifecycle.md) — same two-level model.

## Two levels

| Level | Question |
|-------|----------|
| **First tag** | Is the decision adopted? (`Proposed` / `Accepted` / `Superseded` / `Deprecated`) |
| **Second tag** | Is the obligation **implemented in code**? (`Implemented`, `In progress`, …) |

**Accepted** = we agreed this is the norm.  
**Accepted · Implemented** = norm **and** current codebase satisfies it (with evidence: tests, golden sessions, ports).

Do **not** use `Implemented` for strategy-only ADRs or partial strangler scope — use `Accepted · In progress` or `Accepted (partial)` in prose.

## Second-tag vocabulary

| Tag | Meaning |
|-----|---------|
| **Implemented** | Scope done; conformance/golden where applicable |
| **In progress** | Accepted direction; vertical slice shipping |
| **—** | Decision only; implementation not started or N/A |

## Verifiable facts (future)

ADR body may include:

```text
facts:
  golden: GS1..GSn
  hoare: ...
end facts
prose:
  ...
end prose
```

Transition to **`Accepted · Implemented`** requires `Sat(facts)` on `main` (see [GUIDERS-FSHARP-ADR-0006](./GUIDERS-FSHARP-ADR-0006-adr-lifecycle-verifiable-facts.md)).

## When updating

1. ADR header `**Status**`
2. Row in [README.md](./README.md)
3. One commit per logical status change
