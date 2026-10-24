# AIGuiders.Platform.Modeling.IdeSession.GateCatalog

Gate catalog IR: gate / reject-when / code rows from ide-session *.catalog.gdl gates tables — data only, no evaluation.

## What you get

- `IdeSessionGateRow` — one gate: `GateId`, `RejectWhen` (condition wire), `Code` (stable diagnostic code)
- `IdeSessionGateCatalog` — `{ SourcePath, Gates: IdeSessionGateRow array }` — the parsed gates table with its provenance

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.IdeSession.GateCatalog

// What *.catalog.gdl "gates table" parses into (Gdl.Parse.IdeSession fills it):
let catalog =
    { SourcePath = "ide.catalog.gdl"
      Gates =
        [| { GateId = "solution-loaded"
             RejectWhen = "phase = unloaded"
             Code = "GATE_SOLUTION_NOT_LOADED" } |] }
```

## Problem

- Session-level preconditions ("don't run this while the solution is unloaded") are usually inline guards:
  - the condition and its message are private to the call site
  - there is no stable code to filter or test against
- So tooling can't enumerate the gates it respects.

## Solution

- Gates become catalog rows: named, with a condition wire and a stable code — enumerable, diffable, testable.
- Kept deliberately tiny: the parser (`.Gdl.Parse.IdeSession`) fills it, the evaluator consumes `RejectWhen` at execution time.
- Battle-tested as the gates vocabulary of the Guiders ide-session stack — [FSHARP ADR-0004](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md).

## When not to use

- Evaluating `RejectWhen` conditions — this is the row IR; the session runtime decides.
- Parsing `*.catalog.gdl` — that is `AIGuiders.Platform.Modeling.Gdl.Parse.IdeSession` (it produces this catalog).
- Full catalog sections (channels, executors) — only the gates table lives here.

## Learn more

- [FSHARP ADR-0004 — ide-session modeling ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
