package: AIGuiders.Platform.Modeling.Cockpit.DataBus
tagline: Typed in-process event bus shapes: stable event ids with typed payloads, dispatch policy (reliable vs burst), and validated projection graphs (events → fold slots / channel fields).

## What you get

- `EventId` — stable event ids (`BuildStateChanged`, `TestsStateChanged`, `DebugStateChanged`, `GitStateChanged`, `IdeHostStateChanged`, `StartupProjectPathChanged`, `DeskSurfaceBuilt`) with `tryParse` / `all` / `TypeName`
- typed event payloads — records per event (build state, tests summary, git line, LSP host state, startup project path, desk surface built)
- `DispatchPolicy` — per-event reliable vs burst; `defaultPolicy` in parity with the C# runtime
- `ProjectionGraph` — edges from events to `FoldSlot`s (CCU fold accumulators) or `ChannelField`s (channel DTO fields), with `validate` (unknown events, edge shape rules, duplicate fold slots) and `subscribedEvents`
- `EventCatalog.ideHealthEvents` — the events wired into the IDE Health CCU fold

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Cockpit.DataBus

let graph =
    { Channel = ChannelId.ideHealth
      Edges =
        [ ProjectionGraph.folds EventId.BuildStateChanged FoldSlot.BuildSnapshot
          ProjectionGraph.projects EventId.GitStateChanged ChannelField.WorkspaceGit ] }

let issues = ProjectionGraph.validate graph   // { Issues = [] } → IsValid = true
```

## Problem

- An in-process event bus needs the same answers everywhere:
  - which events exist and what each carries
  - which events may drop frames (burst) and which must not (reliable)
  - which events fold into which accumulator slots and which project straight onto channel fields
- Hand-rolled, these live as string switch statements per consumer, with no validation of the wiring.

## Solution

- Events, payloads, dispatch policy and projection wiring are data — one DU, records, sets, and a validated graph instead of scattered switches.
- Battle-tested as the F# SSOT of the IDE DataBus, in parity with the C# runtime ([CIDE ADR-0099](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0099-ide-databus-typed-events-and-projections.md) · [CIDE ADR-0097](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)).

## When not to use

- Cross-process or network messaging — this is a typed in-process bus; transports live elsewhere (threading channels on the C# side).
- You only need channel DTOs or lamp algebra — take the Channels package; the bus feeds it.

## Learn more

- [CIDE ADR-0099 — IDE DataBus typed events and projections](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0099-ide-databus-typed-events-and-projections.md)
- [CIDE ADR-0097 — cockpit compute units → channel DTOs](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)
