package: AIGuiders.Platform.Modeling.Cockpit.Channels
tagline: Cockpit channel shapes: annunciator lamp algebra, IDE Health and Environment Readiness channel DTOs, CDP instruments (QRH, arch board).

## What you get

- `AnnunciatorLampLevel` (`Ok` / `Advisory` / `Caution` / `Critical`) + `AnnunciatorLamp.severityRank` / `maxLevel` — EICAS strip priority, master lamp from a strip
- `AnnunciatorLampItem` — one lamp cell: id, title, detail, level, short label
- `IdeHealth.decideScope` — CCU decision: project scope iff a startup project is set and a project signal exists
- `IdeHealth.summarizeDebug` — debug summary line (idle / running / paused · frames, vars)
- `EnvironmentReadinessCellIds` — stable cell ids for the Environment Readiness deck
- `EnvironmentReadiness.mergeExtension` — merge extension rows onto a core lamp snapshot (core first)
- CDP instruments: `QrhPage` / `QrhStep` / `QrhSuggest` (+ `Qrh.relatedMinusHot`), `ArchBoardDoc` / `ArchRoleSlot` / `ArchEdge` (+ `ArchBoard.canonicalChain`)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Cockpit.Channels

let strip =
    [ { Id = "build"; Title = "Build"; Detail = "ok"; Level = Ok; LampShortLabel = "BLD" }
      { Id = "tests"; Title = "Tests"; Detail = "3 failed"; Level = Caution; LampShortLabel = "TST" } ]

let master = AnnunciatorLamp.maxLevel strip   // Caution
```

## Problem

- Every cockpit channel (IDE Health, Environment Readiness, QRH) keeps re-deriving the same things in host code:
  - which lamp level wins on a strip
  - whether health scope is solution- or project-wide
  - what a readiness row is and how extension rows merge

## Solution

- Channel DTOs and lamp algebra as named F# shapes with pure functions — hosts consume, not re-implement.
- Battle-tested as the C#-parity shapes of the Cascade IDE cockpit ([CIDE ADR-0097](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md) · [CIDE ADR-0099](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0099-ide-databus-typed-events-and-projections.md)).

## When not to use

- You need event transport or dispatch policy — take the DataBus family; this package is channel shapes and algebra.
- You need routing decisions (MFD/go-verb) — that is the Cds package.

## Learn more

- [CIDE ADR-0097 — cockpit compute units → channel DTOs](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)
- [CIDE ADR-0099 — IDE DataBus typed events and projections](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0099-ide-databus-typed-events-and-projections.md)
