# GUIDERS-FSHARP-ADR-0003: Model extraction matrix (all Platform entities)

| | |
|---|---|
| **Status** | Accepted (2026-09-02) |
| **Tags** | #guiders #fsharp #modeling #execution #extraction #platform |
| **Related** | [GUIDERS-FSHARP-ADR-0002](./GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) · [GUIDERS-PLATFORM-ARCHITECTURE-HUB](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/GUIDERS-PLATFORM-ARCHITECTURE-HUB.md) |

## Context

`guiders-platform` ships **114** `AIGuiders.Platform.*` packages. Most bundle **model** (shapes, grammars, graphs, policies) with **mechanics** (IO, registry, UI, Roslyn, bus dispatch). [ADR-0002](./GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) establishes `Platform.Modeling.*` (F#) vs `Platform.Execution.*` (C#). This ADR is the **inventory**: every family, every package, what gets extracted, where it lands.

**Operator intent:** Execution is a **thin drive layer** — publish, subscribe, load file, call Roslyn, mount control. **Strict typing** flows from F# Modeling; C# references Modeling types (or `[<CLIMutable>]` views), never forks shapes.

## Decision

### 1. Three-layer rule (every package)

| Layer | Language | Owns | Must not own |
|-------|----------|------|--------------|
| **Modeling** | F# | records, DU, graphs, parse, validate, eval, trace, conformance | IO, threads, DI roots, UI |
| **Seam** | C# (thin) | `interface`, `delegate`, host contracts referencing Modeling types | duplicate DTO fields |
| **Execution** | C# (thin) | drive: `Publish`, `Load`, `Execute`, `Render`, emit `*.g.cs` | second IR, ad-hoc policy tables |

**Hybrid packages** (today) → **split** into Modeling + Execution; one release-cycle shim allowed, no forked shapes.

### 2. Entity taxonomy (model domains)

```text
GDL declare        catalog, deck, display, cockpit.logic, expression, project import graph
Notations wire     keyboard, command, argument, bracket profiles + normalized IR
Command plane      catalog tree, binding, melody graph, slash resolve algebra, constructors schema
Combinations       fold semantics, overlay/catalog merge policies, catalog index algebra
Cockpit circuit    topology, zones, circuit wiring, CDS routing, rules eval
Cockpit bus        event catalog, dispatch policy, projection/fold graph, fact bindings
Graph surfaces     GraphDocument, layout scene, navigation scene, relation_kind taxonomy
Agent / MC         envelope, detail tiers, pulse truncation rules
Correspondence     CRS wire, anchor graph, forward/reverse map shapes
Navigation         scene IR, caps, preset merge policy
Conformance        spec vectors, expected IR snapshots
Language edits     Locus, TextEdit, BracketAnchorSpan, SniperScope (language-neutral)
Configurations     workspace/project **schema** (not file load)
Documentation        anchor ids, link report shapes
```

Each domain = one or more `Platform.Modeling.*` packages. **No domain keeps authoritative shapes in C#** after extraction.

### 3. Execution thin layer (normative)

Execution **only**:

1. **Drive** — invoke parser/eval from Modeling; publish event; read file; run tool; post to UI thread.
2. **Host** — DI registration, singleton registries, `PackageReference` to Modeling.
3. **Adapt** — `IGraphDataSource`, `ISource<T>`, `IIntentOrgan` implementations that **produce** Modeling IR from external world.
4. **Emit** — Roslyn generators that **read** Modeling IR → `*.g.cs` for planets still on C# idioms.
5. **Render** — WPF/Avalonia/Skia hosts consuming layout IR + snapshots.

Execution **never** defines authoritative event catalogs, merge policies, graph node kinds, or rule eval — only **consumes** them.

### 4. Package extraction matrix

Legend: **M** = Modeling (F#) · **E** = Execution (C#) · **S** = Seam only · **M/E** = split required

#### 4.1 Foundation

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `Abstractions` | M/E | `Modeling.Core` — `IntentOutcome`, `RoutedIntent`, pulse rules | `Execution.Core` — seam types only if needed |
| `Routing` | M/E | `Modeling.Routing` — refusal algebra, route key shapes | `Execution.Routing` — `IIntentOrgan` host |
| `Paths` | M | `Modeling.Core.Paths` — path normalization rules | — |

#### 4.2 GDL / Authoring

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `Authoring.Core` | M | `Modeling.Gdl.Authoring` ✓ | — |
| `Authoring.Deck` | M | `Modeling.Gdl.Parse.Deck` ✓ (mirror) | shim until cutover |
| `Authoring.Command.Catalog` | M | `Modeling.Gdl.Parse.Catalog` | — |
| `Authoring.Command.Bundles` | M | `Modeling.Gdl.Parse.Bundles` | — |
| `Authoring.Conformance` | M | `Modeling.Conformance.Authoring` | runner host in Execution |
| `Authoring.Display.Binding` | M | `Modeling.Gdl.Parse.Display` | — |
| `Authoring.Cockpit.Logic` | M | `Modeling.Gdl.Parse.CockpitLogic` | — |
| `Authoring.Expression` | M | `Modeling.Gdl.Expression` | — |

#### 4.3 IntermediateRepresentation (all → Modeling)

| Package | F# target |
|---------|-----------|
| `IntermediateRepresentation.Presentation` | `Modeling.Gdl.Presentation` ✓ |
| `IntermediateRepresentation.Command` | `Modeling.Gdl.Command` |
| `IntermediateRepresentation.Binding` | `Modeling.Gdl.Command.Binding` |
| `IntermediateRepresentation.Melody` | `Modeling.Gdl.Command.Melody` |
| `IntermediateRepresentation.Invocation` | `Modeling.Notations.Command` |
| `IntermediateRepresentation.Argument` | `Modeling.Notations.Argument` |
| `IntermediateRepresentation.Keyboard` | `Modeling.Notations.Keyboard` |
| `IntermediateRepresentation.Bracket` | `Modeling.Notations.Bracket` |
| `IntermediateRepresentation.Agent` | `Modeling.Gdl.Agent` |
| `IntermediateRepresentation.Language` | `Modeling.Gdl.Language` |

#### 4.4 Notations (parse → Modeling; `.All` bundles → meta only)

| Package | Split | F# target |
|---------|-------|-----------|
| `Notations` | M | `Modeling.Notations.Core` |
| `Notations.Keyboard.*` | M | `Modeling.Notations.Keyboard.*` |
| `Notations.Command.*` | M | `Modeling.Notations.Command.*` |
| `Notations.Argument.*` | M | `Modeling.Notations.Argument.*` |
| `Notations.Bracket` | M | `Modeling.Notations.Bracket` |
| `Notations.Presentation.Topology` | M | `Modeling.Gdl.Presentation` ✓ |
| `InputNotation.*` | — | obsolete shim → delete with Notations cutover |

#### 4.5 Catalog & Combinations

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `Catalog` | M | `Modeling.Catalog` — index, collision, profile algebra | — |
| `Combinations` | M | `Modeling.Combinations` — `CombinationSemantics`, fold laws | — |
| `Combinations.Catalog` | M | `Modeling.Combinations.Catalog` | — |
| `Combinations.Binding` | M | `Modeling.Combinations.Binding` | — |
| `Combinations.Overlay` | M | `Modeling.Combinations.Overlay` | — |
| `Combinations.Project` | M | `Modeling.Combinations.Project` | — |
| `Combinations.Workspace` | M/E | `Modeling.Combinations.Workspace` — overlay rules | `Execution.Combinations.Workspace` — apply at runtime |
| `Combinations.Sources` | E | — | file/project load |
| `Combinations.All` | — | meta-package only | — |

#### 4.6 CommandPlane

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `CommandPlane` | E | — | registry, execute orchestration |
| `CommandPlane.Catalog` | M/E | `Modeling.CommandPlane.Catalog` — descriptor shapes | registry, lookup, execute |
| `CommandPlane.Catalog.CodeGen` | E | — | Roslyn emit from Modeling IR |
| `CommandPlane.Catalog.Sources.*` | E | — | DB/file/TOML/JSON/XML load |
| `CommandPlane.Binding` | M/E | binding IR slice in `Modeling.Gdl.Command` | runtime binding resolve |
| `CommandPlane.Binding.Sources.*` | E | — | sources |
| `CommandPlane.Melody` | M/E | melody graph in `Modeling.Gdl.Command.Melody` | capture stack SM, UI |
| `CommandPlane.Slash` | M/E | `Modeling.Notations.Command.Slash` — `SlashLineResolver` | completion UI, execute hook |
| `CommandPlane.PrefixArmed` | M/E | phrase-slot index algebra | armed state machine |
| `CommandPlane.PrefixArmed.Locale` | M | locale tables | — |
| `CommandPlane.Constructors` | M/E | constructor schema IR | WPF/UI constructors |
| `CommandPlane.ArgSuggestions` | E | — | live suggest (Roslyn/query) |

#### 4.7 Cockpit

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `Cockpit.Abstractions` | S | — | `IChannel`, `ICockpitComputeUnit` seams |
| `Cockpit.Ids` | M | `Modeling.Cockpit.Ids` | — |
| `Cockpit.Cds` | M | `Modeling.Cockpit.Cds` | — |
| `Cockpit.DataBus` | M/E | `Modeling.Cockpit.DataBus` — events, policy, projection graph | `Execution.Cockpit.DataBus` — `InMemoryDataBus` |
| `Cockpit.Channels` | M/E | CCU **decision/snapshot** records, lamp row algebra | CCU **units** with IO, composers calling Modeling |
| `Cockpit.Transport` | E | — | ingress, bounded bus |
| `Cockpit.Composition` | M/E | slot/instrument descriptor schema | compositor host, mount registry |

#### 4.8 Graph, Navigation, Correspondence

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `Navigation` | M | `Modeling.Navigation` | — |
| `Navigation.Policy` | M | `Modeling.Navigation.Policy` | — |
| `Navigation.Code` | E | — | Roslyn scene builder |
| `Documentation.Correspondence.Core` | M | `Modeling.Gdl.Correspondence` | — |
| `Documentation.Correspondence` | M/E | wire shapes | orchestration |
| `Documentation.Correspondence.*` | E | — | resolve/reverse/workspace IO |
| `Documentation.Anchors` | M/E | anchor id grammar | file scan |
| `Documentation.LinkCheck` / `.LinkMutate` / `.Reports` | E | report **shapes** → Modeling if reused | runners |

#### 4.9 MCPlane, Conformance, Language intelligence

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `MCPlane` | M/E | `Modeling.Gdl.Agent` — tier/truncate rules | envelope dispatch host |
| `Conformance.Navigation` | M | `Modeling.Conformance.Navigation` | runner |
| `Conformance.Policies` | M | `Modeling.Conformance.Policies` | runner |
| `Conformance.Schemas` | M | `Modeling.Conformance.Schemas` | runner |
| `LanguageIntelligence` | E | — | orchestration |
| `LanguageIntelligence.Adapters.Roslyn` | E | — | Roslyn |
| `LanguageIntelligence.*` | M/E | `Modeling.Gdl.Language` for neutral edits | adapters |
| `Language.CSharp.*` / `Language.Xml.Anchors` | E | — | language-specific probes |

#### 4.9.1 Language Resolver Center (LRC) — shipped slice (2026-09-03)

| Package | Split | F# target | Status |
|---------|-------|-----------|--------|
| `Platform.Modeling.Language` | **M** | `Kernel.fs` — `LanguageRequest`, `LanguageDiagnostic`, `FindUsagesResult`, `RenameSymbolResult`, … | **shipped** (sibling `guiders-fsharp`) |
| `Platform.Modeling.Language.Adapters.Fcs` | **M** | FCS backend — 7 IDE verbs; rename `apply` via `SessionOrchestrator.applyPatch`; project resolve via ω (`FileOwnership`); active-pattern blocker; workspace scan via `FSharpSymbol.IsEffectivelySameAs` | **shipped** |
| `Platform.Modeling.Language.Adapters.Gdl` | **M** | GDL adapter stubs (deck pilot) | scaffold |
| `Platform.Execution.Language` | **E** | `LanguageResolverCenter`, `ILanguageBackend` federation gateway | **shipped** (sibling `guiders-platform`) |

**Not yet extracted to NuGet** — model not fully separated from platform wave; CDP uses sibling `ProjectReference` via `eng/Guiders.Modeling.props`. F6 (`LanguageIntelligence` adapter consolidation per GUIDERS-ADR-0061) deferred.

#### 4.10 Sources, Configurations, Utilities

| Package | Split | F# target | Execution keeps |
|---------|-------|-----------|-----------------|
| `Sources` | S/E | — | `ISource<T>` seam |
| `Sources.File` / `.Toml` | E | — | transport |
| `Configurations.Workspace` / `.Project` | M/E | settings **schema** IR | — |
| `Configurations.*.Sources` | E | — | load/save |
| `Utilities.Adoption.*` | E | — | audit runners (report shapes → Modeling if shared) |

### 5. Modeling package tree (target, consolidated)

```text
Platform.Modeling.Core
Platform.Modeling.Paths
Platform.Modeling.Routing
Platform.Modeling.Catalog
Platform.Modeling.Combinations.*
Platform.Modeling.Gdl.*
Platform.Modeling.Notations.*
Platform.Modeling.Cockpit.*
  ├── Topology, Circuit, Cds, Rules, DataBus, Ids
Platform.Modeling.Graph.*
Platform.Modeling.Navigation.*
Platform.Modeling.Gdl.Correspondence
Platform.Modeling.Gdl.Agent
Platform.Modeling.Gdl.Language
Platform.Modeling.Language
Platform.Modeling.Language.Adapters.Fcs
Platform.Modeling.Language.Adapters.Gdl
Platform.Modeling.Conformance.*
```

~**45–55** F# packages after split (some slices merge). ~**40–50** Execution packages (mostly Sources, adapters, hosts, emit).

### 6. Extraction discipline

1. **Name the entity first** — e.g. `GitStateChanged` is a bus event node, not a CCU class.
2. **One shape, one owner** — if C# and F# both define it, delete C# shape after shim window.
3. **Graph edges explicit** — wiring tables live in Modeling; Execution codegen or hand-registers from IR.
4. **Conformance follows model** — every Modeling package gets vectors before C# shim removal.
5. **No «utility model» in Execution** — if it's persisted, compared, or validated, it's Modeling.

### 7. Phases (extraction order)

```text
A   ✓  Gdl spine + deck/topology
B      catalog, expression, cockpit.logic, Rules, Cds
B2     Cockpit.Topology, Circuit, DataBus schema
C      Notations.* + IR.* absorption
C2     Graph.*, Navigation, Correspondence
C3     Combinations, Catalog, Routing/MCPlane models
D      Execution.* thin hosts; delete flat Platform.* 0.31.x shapes
E      toolchain / analyzers enforce no model in Execution
```

## Consequences

- Every platform package has a **declared split** — no «we'll figure it out during port».
- Execution project count drops in **authority**; Modeling project count rises — total packages may stay similar, but dependency direction is strict.
- Product repos (cascade-ide, cdp-mcp) adopt Modeling for tests without pulling WPF/runtime.

## Non-goals

- Rewriting Roslyn, Skia, or Avalonia in F#
- Merging all graph types into one mega-`GraphNode` (shared spine yes, one AST no)
- Big-bang single PR for all 114 packages
