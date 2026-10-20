# AIGuiders.Platform.Modeling.Cockpit.Ids

IDE Display System overlay hits: palette hits as go/tool + score, ordered by score.

## What you get

- `IdsFeatureHit` — one palette hit: `Go` (route), `Tool` (organ tool), `Score` (rank)
- `IdsFeatureHit.make` — constructor helper
- `IdsFeatureHit.byScoreDesc` — order hits by score, descending

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Cockpit.Ids

let ranked =
    [ IdsFeatureHit.make "open-file" 90 "editor.open"
      IdsFeatureHit.make "nav-jump" 70 "editor.goto" ]
    |> IdsFeatureHit.byScoreDesc
    |> List.ofSeq
```

## Problem

- A display overlay ranks many candidate features per frame; raw tuples rot into positional access and ad-hoc sorts at every call site.

## Solution

- One record with named fields and one canonical ordering — the overlay pipeline and its consumers share the same hit shape.
- Battle-tested as the F# SSOT for the IDS overlay pipeline ([CIDE ADR-0079](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0079-ide-display-system-ids-overlay-pipeline.md)).

## When not to use

- You need the overlay pipeline itself (projection, transport, drawing) — this package is the hit shape only.

## Learn more

- [CIDE ADR-0079 — IDS overlay pipeline](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0079-ide-display-system-ids-overlay-pipeline.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
