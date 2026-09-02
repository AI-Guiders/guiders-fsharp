# GUIDERS-FSHARP-ADR-0002: Modeling vs Execution (GDL + Notations → F#)

| | |
|---|---|
| **Status** | Accepted (2026-09-02; `Platform.Modeling` / `Platform.Execution` prefix same day) |
| **Tags** | #guiders #fsharp #gdl #notations #ir #modeling #execution #federation |
| **Related** | [GUIDERS-FSHARP-ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) · [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) · [GUIDERS-ADR-0057](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md) · [GUIDERS-ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md) · [GUIDERS-ADR-0058](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md) · CIDE [0036](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/en/adr/0036-cds-channel-compositor-surface-pipeline.md) · [0067](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/en/adr/0067-graph-backed-surfaces-contract.md) · [0115](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/en/adr/0115-cds-graph-backed-shared-layer.md) |

## Context

Federation has two **model-heavy** hyperlanes:

| Hyperlane | Today (C#) | Question |
|-----------|--------------|----------|
| **GDL** (`Authoring.*`) | parse `*.{quarry}.gdl` → declare IR | Who owns parsers + IR SSOT? |
| **Notations** (`Notations.*`) | parse wire → normalized IR | Who owns grammar rules + IR SSOT? |

Both are **algebraic**: discriminated shapes, exhaustive `match`, cross-ref validation, conformance vectors — not UI or runtime mechanics.

[ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) placed GDL **spine** and **parse north star** in `guiders-fsharp`. Deck + `PresentationTopology` mirror shipped as `AIGuiders.Platform.Modeling.Gdl.*` (**Phase A done**).

**Notations** ([0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)) follow the same pattern: wire format(s) → branch Core IR → mechanics consume.

One **.NET** world; layer under the existing **Platform** umbrella:

```text
AIGuiders.Platform.Modeling.*     F#   parse, IR, rules, validation, conformance
AIGuiders.Platform.Execution.*    C#   UI, registry, execute, emit host
```

**Why nested under `Platform` (not top-level `AIGuiders.Modeling.*`):**

- Publish/CI globs already target `AIGuiders.Platform.*`
- `Directory.Packages.props` / CPM patterns stay one family
- Migration from flat `Platform.Authoring.*` / `Platform.CommandPlane.*` is a **segment insert**, not a new root
- CDP (`AIGuiders.Cdp.*`), AgentNotes, MCP tools stay **outside** Platform — correct boundary

Repos (`guiders-fsharp`, `guiders-platform`) are transport. The **NuGet prefix** is normative.

## Decision

### 1. Package prefix (normative)

| Prefix | Language | Owns |
|--------|----------|------|
| **`AIGuiders.Platform.Modeling.*`** | F# | GDL + Notations: grammars, IR SSOT, validation, conformance |
| **`AIGuiders.Platform.Execution.*`** | C# | CommandPlane, Studio, Cockpit runtime, emit — **consumers** of Modeling |

**Dependency rule:** `Platform.Execution.*` → `Platform.Modeling.*`. Never a second authoritative IR in Execution.

**Glob patterns (normative intent):**

```text
AIGuiders.Platform.Modeling.*      F# packages (guiders-fsharp)
AIGuiders.Platform.Execution.*     C# packages (guiders-platform)
AIGuiders.Platform.*               umbrella (publish filters, docs — excludes Cdp/AgentNotes)
```

```text
guiders-fsharp                          guiders-platform
├── Platform.Modeling.Gdl.*             ├── Platform.Execution.CommandPlane.*
├── Platform.Modeling.Notations.*       ├── Platform.Execution.Studio.*
├── Platform.Modeling.Cockpit.*           ├── Platform.Execution.Cockpit.*  (DataBus, Transport, Channels, Composition)
├── Platform.Modeling.Navigation          ├── Platform.Execution.Emit.*
├── Platform.Modeling.Graph               └── …
└── …
```

### 2. GDL Modeling packages (target map)

| Package | Owns | Replaces (transitional C#) |
|---------|------|-------------------------|
| `AIGuiders.Platform.Modeling.Gdl.Core` | `GdlProject`, `GdlFragment`, quarry payload spine | — |
| `AIGuiders.Platform.Modeling.Gdl.Authoring` | lexical kit: blocks, tables, import, diagnostics | `Platform.Authoring.Core` |
| `AIGuiders.Platform.Modeling.Gdl.Presentation` | `PresentationTopology`, topology wire parse | `IR.Presentation` |
| `AIGuiders.Platform.Modeling.Gdl.Command` | catalog / bundle IR | `IR.Command` · `Authoring.Command.*` |
| `AIGuiders.Platform.Modeling.Gdl.Cockpit` | cockpit.logic rule graph IR | proposed `Authoring.Cockpit.Logic` |
| `AIGuiders.Platform.Modeling.Gdl.Display` | display binding IR | proposed `Authoring.Display.Binding` |
| `AIGuiders.Platform.Modeling.Gdl.Expression` | shared `ExprNode` for `when` / conditions | proposed `Authoring.Expression` |
| `AIGuiders.Platform.Modeling.Gdl.Parse.*` | quarry parsers (`Deck`, `Catalog`, …) | `Platform.Authoring.*` |
| `AIGuiders.Platform.Modeling.Gdl.Validation` | cross-quarry rules | — |
| `AIGuiders.Platform.Modeling.Gdl.Project` | `*.gdlproj`, import graph | `Authoring.Project` |

**Ship order:** deck + topology + rename (**done**) → catalog → display → cockpit.logic → expression.

### 3. Notations Modeling packages (target map)

| Package | Owns | Replaces (transitional C#) |
|---------|------|----------------------------|
| `AIGuiders.Platform.Modeling.Notations.Core` | profiles, conformance hooks, branch registry | — |
| `AIGuiders.Platform.Modeling.Notations.Keyboard` | `NormalizedKeySequence`, Vim/KeyGesture grammars | `InputNotation.*` · `Platform.Notations.Keyboard.*` |
| `AIGuiders.Platform.Modeling.Notations.Command` | `NormalizedCommandLine`, slash/console path rules | `Platform.Notations.Command.*` |
| `AIGuiders.Platform.Modeling.Notations.Argument` | `NormalizedArguments`, tail profiles | `Platform.Notations.Argument.*` |
| `AIGuiders.Platform.Modeling.Notations.Bracket` | `NormalizedBracketWire`, delimiter profiles | `Platform.Notations.Bracket.*` |

Mechanics in `Platform.Execution.*` call `Platform.Modeling.Notations.*.Parse(wire)` and receive stable IR.

### 4. Cockpit vertical slice ([0057](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md))

Cockpit is **not** one layer. Declare annunciation + pure eval = Modeling (F#); pub/sub + UI subscription = Execution (C#).

```text
*.cockpit.logic.gdl
    → Parse.CockpitLogic          Platform.Modeling.Gdl.Parse.CockpitLogic
    → CockpitRuleGraph IR         Platform.Modeling.Gdl.Cockpit
    → ExprNode (when / derived)   Platform.Modeling.Gdl.Expression
    → evaluate(facts) → outcomes  Platform.Modeling.Cockpit.Rules   ← pure F#
    → trace (rule id, matched when)
         │
         ▼
    event → fact projection        Platform.Modeling.Cockpit.DataBus  ← schema + wiring graph
    DataBus.Publish / subscribe    Platform.Execution.Cockpit.DataBus  ← thin runtime only
    Transport, Channels, CDS       Platform.Execution.Cockpit.*
    EicasStrip, zone visibility      Platform.Execution.Studio.*
```

| Package | Language | Owns |
|---------|----------|------|
| `AIGuiders.Platform.Modeling.Gdl.Cockpit` | F# | `CockpitRuleGraph`, facts/rules/alerting/projectors IR |
| `AIGuiders.Platform.Modeling.Gdl.Expression` | F# | shared `ExprNode` eval substrate (deck + cockpit) |
| `AIGuiders.Platform.Modeling.Gdl.Parse.CockpitLogic` | F# | `*.cockpit.logic.gdl` parser |
| `AIGuiders.Platform.Modeling.Cockpit.Rules` | F# | **headless** `evaluate(graph, facts) → outcomes + trace` — no IO, no WPF |
| `AIGuiders.Platform.Modeling.Cockpit.DataBus` | F# | typed event catalog, dispatch policy, projection/fold wiring graph |
| `AIGuiders.Platform.Execution.Cockpit.DataBus` | C# | `IDataBus` impl: publish/subscribe, threading, bounded channels |
| `AIGuiders.Platform.Execution.Cockpit.*` | C# | Transport, Channels, Composition, CDS snapshot **runtime** |

**Normative:** `Platform.Cockpit.Rules` (proposed in platform ADR-0057 as C#) **moves to F#** as `Platform.Modeling.Cockpit.Rules`. Execution calls Modeling eval; it does not re-implement rule matching.

**Split from `.deck`:** `.deck` owns `eicas when alerts` (projection hook); `.cockpit.logic` owns what counts as alert and severity ([0057](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md) §3).

### 5. Execution packages (target map)

| Package | Owns | Replaces (flat `Platform.*` at 0.31.x) |
|---------|------|----------------------------------------|
| `AIGuiders.Platform.Execution.CommandPlane.*` | catalog/registry, execute, completion | `Platform.CommandPlane.*` |
| `AIGuiders.Platform.Execution.Studio.*` | WPF / product surfaces | `guiders-wpf`, Studio hosts |
| `AIGuiders.Platform.Execution.Cockpit.*` | transport, channels, composition, **DataBus runtime** | `Platform.Cockpit.*` **minus** rule eval and event schema |
| `AIGuiders.Platform.Execution.Emit.*` | Roslyn `*.g.cs` from Modeling IR | platform emit tools |
| `AIGuiders.Platform.Execution.MCPlane` | agent envelope | `Platform.MCPlane` |
| `AIGuiders.Platform.Execution.Routing` | intent route/execute seam | `Platform.Routing` |

Flat `AIGuiders.Platform.Authoring.*`, `Platform.IntermediateRepresentation.*`, `Platform.Notations.*` (model halves) → **abandon at 0.31.x**; new work ships under `Platform.Modeling.*` only.

### 6. IR lives in Modeling — Execution consumes

| IR class | SSOT | Execution access |
|----------|------|------------------|
| Declare IR (GDL) | `Platform.Modeling.Gdl.*` | PackageReference; optional `[<CLIMutable>]` on hot records |
| Wire IR (Notations) | `Platform.Modeling.Notations.*` | same |
| Cockpit eval | `Platform.Modeling.Cockpit.Rules` | `evaluate()` call from fold adapter |
| DataBus schema + wiring | `Platform.Modeling.Cockpit.DataBus` | event types, policy, projection graph; optional `[<CLIMutable>]` |
| DataBus runtime | `Platform.Execution.Cockpit.DataBus` | `InMemoryDataBus`, thread marshaling |
| Runtime-only views | `*.g.cs` emit | generated snapshots where WPF/registry need it |

### 7. NuGet identity migration

**Target IDs** use `AIGuiders.Platform.Modeling.*` / `AIGuiders.Platform.Execution.*`.

| Transitional | Target |
|--------------|--------|
| flat `AIGuiders.Platform.Authoring.*` | `AIGuiders.Platform.Modeling.Gdl.*` (per quarry) |
| `AIGuiders.Platform.IntermediateRepresentation.*` | absorbed into `Platform.Modeling.*` |
| `AIGuiders.Platform.Notations.*` | `AIGuiders.Platform.Modeling.Notations.*` |
| `AIGuiders.Platform.CommandPlane.*` | `AIGuiders.Platform.Execution.CommandPlane.*` |
| `AIGuiders.Platform.Cockpit.*` (runtime) | `AIGuiders.Platform.Execution.Cockpit.*` |
| `AIGuiders.Platform.Cockpit.DataBus` (events + policy) | `AIGuiders.Platform.Modeling.Cockpit.DataBus` (F#) + `Execution.Cockpit.DataBus` (runtime) |
| proposed `Platform.Cockpit.Rules` | `AIGuiders.Platform.Modeling.Cockpit.Rules` (F#) |

**Default path:** flat `Platform.*` at **0.31.x** stops receiving releases; new IDs from **1.0** (or next wave). Federation repos update refs in one window. NuGet deprecation/unlist **optional** — downloads are mostly CI restore ([§ nuget reality](https://github.com/NuGet/Home/issues/931)), external adopters negligible.

Namespaces and assembly names **SHOULD** match `PackageId`.

### 8. Boundary: GDL vs Notations (unchanged semantics)

| | GDL | Notations |
|---|-----|-----------|
| **When** | declare-time | runtime wire |
| **Prefix** | `Platform.Modeling.Gdl.*` | `Platform.Modeling.Notations.*` |

No merged mega-AST ([0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) §9).

### 9. Migration phases

Superseded by [§14](#14-migration-phases-revised) — kept for link stability.

```text
Phase A (done)     Platform.Modeling.Gdl.* rename + deck/topology mirror
Phase B           catalog + cockpit.logic (parse, IR, Expression, Cockpit.Rules eval)
Phase C           Platform.Modeling.Notations.* (F#)
Phase D           Platform.Execution.* for runtime; abandon flat Platform.* 0.31.x
Phase E           LSP / toolchain globs: Platform.Modeling.* + Platform.Execution.*
```

**Shim rule:** C# wrapper MAY re-export Modeling types for one release cycle; MUST NOT fork IR shapes.

### 10. Cockpit circuit graph (channels, subsystems, zones)

Cockpit semantics are **graph-shaped**. Today they are implicit across `.deck`, `.cockpit.logic`, product registries, and CIDE ADR prose. **Normative:** federation owns a typed **cockpit circuit graph IR** in F#; Execution owns bus, IO, and mount.

```text
                    ┌─────────────────────────────────────┐
  .deck             │  Platform.Modeling.Cockpit.Topology │
  topology/zones ──►│  hosts, zones, slots, layout board  │
                    └──────────────┬──────────────────────┘
                                   │ mountsIn / routesTo
  .cockpit.logic    ┌──────────────▼──────────────────────┐
  facts/rules ─────►│  Platform.Modeling.Cockpit.Circuit    │
                    │  subsystems, channels, projectors      │
                    └──────────────┬──────────────────────┘
                                   │ feeds / projectsTo
  pure eval         ┌──────────────▼──────────────────────┐
  facts + rules ───►│  Platform.Modeling.Cockpit.Rules     │
  routing algebra ─►│  Platform.Modeling.Cockpit.Cds         │
                    └──────────────┬──────────────────────┘
                                   │ outcomes + trace
                    ┌──────────────▼──────────────────────┐
  runtime           │  Platform.Execution.Cockpit.*        │
                    │  DataBus, CCU IO, CDS snapshot, UI   │
                    └─────────────────────────────────────┘
```

| Node kind | Examples | Authored by |
|-----------|----------|-------------|
| `Host` | Pfd, Forward, Mfd | `.deck` topology |
| `Zone` / `Slot` | `spec-tree`, `eicas`, `forward-editor` | `.deck` zones / layout |
| `Channel` | IdeHealth, EnvironmentReadiness, Eicas | federation catalog + circuit |
| `Subsystem` | git, build, debug, LSP | circuit (feeds channel) |
| `Instrument` | `workspace_navigation_map`, `solution_explorer_tree` | circuit + composer registry |
| `Alert` / `Projector` | `need-commit` → Eicas | `.cockpit.logic` |

| Edge kind | Meaning |
|-----------|---------|
| `feeds` | subsystem → channel (raw signal contract) |
| `foldsTo` | channel → CCU snapshot step (declarative; impl stays Execution) |
| `routesTo` | CDS decision → host/zone (pure algebra in `Cockpit.Cds`) |
| `projectsTo` | alert → projector target (Eicas, slash-hint) |
| `mountsIn` | instrument → slot |

**CCU clarification ([CIDE 0097](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/en/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)):** CCU **implementations** stay Execution (IO, fold over DataBus). The **declared fold topology** (`foldsTo` edges, input/output DTO shapes) MAY live in circuit IR for conformance and codegen — but CCU is **not** a generic graph runtime node.

| Package | Language | Owns |
|---------|----------|------|
| `AIGuiders.Platform.Modeling.Cockpit.Topology` | F# | zones, layout board, slot graph (from `.deck`) |
| `AIGuiders.Platform.Modeling.Cockpit.Circuit` | F# | subsystem ↔ channel ↔ zone ↔ instrument wiring |
| `AIGuiders.Platform.Modeling.Cockpit.Cds` | F# | `AttentionRouting*` shapes + pure router |
| `AIGuiders.Platform.Modeling.Cockpit.Rules` | F# | `evaluate(graph, facts) → outcomes + trace` |
| `AIGuiders.Platform.Execution.Cockpit.*` | C# | DataBus, Transport, Channels, Composition, surface mount |

### 11. Graph-backed surfaces (Semantic Map, Navigation, content graphs)

Graph-backed cockpit instruments ([CIDE 0067](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/en/adr/0067-graph-backed-surfaces-contract.md), [0115](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/en/adr/0115-cds-graph-backed-shared-layer.md)) split the same way: **document + layout algebra** = Modeling; **adapters + Skia/Avalonia** = Execution.

```text
Roslyn / Git / HCI adapter          Platform.Execution.Cockpit.Graph.*
        │                                      │
        ▼                                      │
  wire JSON (transport)                        │
        │                                      │
        ▼                                      │
  GraphDocumentJson.Parse ──────────► Platform.Modeling.Graph.Document
        │                                      │
        ▼                                      │
  layout engine (pure) ─────────────► Platform.Modeling.Graph.Layout
        │                                      │
        ▼                                      ▼
  GraphLayoutScene (IR)              Skia render, hit-test host (Execution)
```

| Concern | Modeling (F#) | Execution (C#) |
|---------|---------------|----------------|
| **Semantic Map / control-flow / GitMap content** | `GraphDocument`: nodes, edges, `graph_kind`, `relation_kind`, anchor | `IGraphDataSource` adapters (Roslyn, workspace index) |
| **Wire parse** | `GraphDocumentJson` / navigation wire → IR | HTTP/MCP transport only |
| **Layout** | `IGraphLayoutEngine` pure transforms → `GraphLayoutScene` | viewport metrics, animation, Skia draw |
| **Navigation scene** | `NavigationScene`, merge/cap policy | `Navigation.Code` Roslyn builder, file IO |
| **Melody / command tree** | `MelodyGraphEdge`, chord projection rules | registry lookup, execute, completion UI |
| **Interaction policy** | pan/zoom limits, hit-test rules, Dark Cockpit declutter laws | input events, focus |

| Package | Owns | Replaces (transitional) |
|---------|------|-------------------------|
| `AIGuiders.Platform.Modeling.Graph.Core` | `GraphNode`, `GraphEdge`, `GraphDocument`, `GraphKind` | `CascadeIDE.Cockpit.Graph.*` types |
| `AIGuiders.Platform.Modeling.Graph.Layout` | layout engines, `GraphLayoutScene` | `Cockpit/Graph/Layout/*` pure stages |
| `AIGuiders.Platform.Modeling.Graph.Wire` | JSON/subgraph/related parse + validation | `GraphDocumentJson` |
| `AIGuiders.Platform.Modeling.Navigation` | `NavigationScene`, caps, preset merge | `Platform.Navigation` model half |
| `AIGuiders.Platform.Execution.Cockpit.Graph` | `IGraphDataSource`, adapters, Skia surface host | product `Cockpit.Graph` IO half |
| `AIGuiders.Platform.Execution.Navigation` | Roslyn scene builder, persistence | `Platform.Navigation.Code` |

**Normative:** one `GraphDocument` spine for Semantic Map, trace-flow, GitMap, and future graph instruments. Domain-specific **adapters** stay Execution; **node/edge algebra + layout** stay Modeling.

### 12. Full `IntermediateRepresentation.*` absorption

All nine flat `Platform.IntermediateRepresentation.*` packages are **Modeling** targets — not a parallel C# IR family.

| Transitional C# | F# target |
|-----------------|-----------|
| `IR.Presentation` | `Platform.Modeling.Gdl.Presentation` ✓ |
| `IR.Command` | `Platform.Modeling.Gdl.Command` |
| `IR.Binding` | `Platform.Modeling.Gdl.Command` (binding slice) |
| `IR.Melody` | `Platform.Modeling.Gdl.Command` (melody slice) |
| `IR.Invocation` | `Platform.Modeling.Notations.Command` |
| `IR.Argument` | `Platform.Modeling.Notations.Argument` |
| `IR.Keyboard` | `Platform.Modeling.Notations.Keyboard` |
| `IR.Bracket` | `Platform.Modeling.Notations.Bracket` |
| `IR.Agent` | `Platform.Modeling.Gdl.Agent` |
| `IR.Language` | `Platform.Modeling.Gdl.Language` |

Additional model pockets (same rule):

| Transitional C# | F# target |
|-----------------|-----------|
| `Platform.Documentation.Correspondence.Core` | `Platform.Modeling.Gdl.Correspondence` |
| `Platform.Navigation` (models) | `Platform.Modeling.Navigation` |
| `Platform.Cockpit.Cds` (DTOs) | `Platform.Modeling.Cockpit.Cds` |
| `Combinations` semantics / `CatalogIndex` collision algebra | `Platform.Modeling.Combinations` |
| `Conformance.*` spec vectors | `Platform.Modeling.Conformance.*` |
| `SlashLineResolver` pure resolve | `Platform.Modeling.Notations.Command` |

### 13. F# affinity rule (when to default to Modeling)

Default **F#** when the module is predominantly:

- discriminated unions + exhaustive `match`
- parse → validate → IR pipeline
- pure graph transform (layout, merge, route, evaluate, trace)
- conformance vectors / property tests over algebra

Default **C# Execution** when the module is predominantly:

- `Publish` / `Subscribe` dispatch, locks, `Channel<T>`, UI thread post
- async IO / DAL probes
- WPF / Avalonia / Skia host and bindings
- Roslyn emit, registry singletons, DI composition root
- MCP tool surface and process boundaries

**DataBus split (normative):** event **shapes**, burst/reliable **policy**, subsystem→event→CCU→channel **wiring graph**, and event→fact projection for `.cockpit.logic` = **Modeling (F#)**. `InMemoryDataBus` and host adapters = **Execution (C#)** — thin shell around the schema.

**No second IR in C#** — Execution MAY hold `[<CLIMutable>]` views or thin DTO mappers at the seam; MUST NOT fork shapes.

### 14. Migration phases (revised)

```text
Phase A (done)     Platform.Modeling.Gdl.* rename + deck/topology mirror
Phase B            catalog + cockpit.logic (parse, Expression, Cockpit.Rules, Cockpit.Cds)
Phase B2           Cockpit.Topology + Cockpit.Circuit IR (zones, subsystem wiring)
Phase C            Platform.Modeling.Notations.*
Phase C2           Platform.Modeling.Graph.* + Navigation models
Phase D            Platform.Execution.* for runtime; absorb IR.*; abandon flat Platform.* 0.31.x
Phase E            LSP / toolchain globs: Platform.Modeling.* + Platform.Execution.*
```

### 15. DataBus — schema and wiring graph (Modeling), runtime (Execution)

[CIDE 0099](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/en/adr/0099-ide-databus-typed-events-and-projections.md) already separates **typed domain events** from transport (0094) and CCU convolution (0097). Today `Platform.Cockpit.DataBus` mixes both: event records + policy live beside `InMemoryDataBus`. **Normative split:**

```text
Platform.Modeling.Cockpit.DataBus (F#)
  ├── EventCatalog          BuildStateChanged, GitStateChanged, …
  ├── DispatchPolicy        burst vs reliable per event id
  ├── StratumTag            workspace / solution / IDE (ADR 0095)
  ├── ProjectionGraph       event → fold step → channel DTO field
  └── FactBinding           event/snapshot slot ↔ cockpit.logic fact id

Platform.Execution.Cockpit.DataBus (C#)
  ├── IDataBus              Publish / Subscribe contract (thin)
  ├── InMemoryDataBus       sync + async dispatch, bounded channels
  └── HostAdapter           UI-thread post, product composition root
```

| Today (mixed C#) | Modeling (F#) | Execution (C#) |
|------------------|---------------|----------------|
| `BuildStateChanged`, `GitStateChanged`, … | `EventCatalog` DU / records | publish payload as-is |
| `DataBusEventPolicy.Default` | `DispatchPolicy` table | read policy at route creation |
| implicit VM wiring | `ProjectionGraph` edges | subscribe handlers from graph emit |
| `InMemoryDataBus` | — | sole owner |

**Why F#:** the bus is a **typed graph** — nodes = events/facts/snapshots; edges = `publishes`, `subscribes`, `foldsTo`, `projectsTo`. Exhaustive validation (“every channel input has a source event or fact”) is algebra, not runtime.

**Execution stays thin:** no event taxonomy in C#; no duplicate policy tables. Generated or hand-written adapter: `ProjectionGraph` → `Subscribe<T>` registrations + CCU fold order.

**Link to Circuit (§10):** `feeds` (subsystem → channel) terminates on **event catalog ids**; `foldsTo` (channel → CCU step) is the projection subgraph inside `Modeling.Cockpit.DataBus`.

## Consequences

- Layer visible in package name **and** under `Platform` glob — publish scripts, CPM, docs need one new segment, not a new root.
- F# in `guiders-fsharp`, C# in `guiders-platform` — unchanged; only `PackageId` / namespace alignment.
- `AIGuiders.Cdp.*`, `AgentNotes.*`, MCP packages unaffected.
- Graph-backed instruments (Semantic Map, trace-flow, GitMap) get a **single Modeling spine** instead of per-product graph types.
- Cockpit channel/subsystem wiring becomes **testable IR** instead of implicit product registries.
- DataBus event taxonomy and projection wiring become **declarative F# graph**; C# host is dispatch only.

## Non-goals

- Top-level `AIGuiders.Modeling.*` (rejected — breaks Platform glob family)
- Rewriting CommandPlane **execution engine** in F# (registry, execute, IO)
- Rewriting DataBus **runtime** (`InMemoryDataBus`, threading) in F#
- Single unified AST across GDL + Notations + Graph (separate spines; shared Expression only)
- TS/Kotlin native Notations ports (per [0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md))
