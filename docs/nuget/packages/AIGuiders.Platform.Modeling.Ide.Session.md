package: AIGuiders.Platform.Modeling.Ide.Session
tagline: IDE session model: solution graphs, project lifecycle phases, capability topologies, relation graphs, frozen snapshots, and the revision ledger — the F# SSOT for tooling session state.

## What you get

- `ProjectNode` + `ProjectKind` — projects as `DotNet` (C#/F#), `Node`, `Gdl`, or `Planet` (language-id keyed); `ProjectId` is a thin alias over the Core identity
- `LifecyclePhase` — `Unloaded → DesignTime → CompileTime → RunTime → TestTime` with `canAdvanceTo` ordering
- Capability model — `CapabilityAttributes` (`Topology`: in-process / out-of-process / subprocess-tool / adaptive; `Warmth`, `CostTier`, `Scope`) and `ProjectCapabilityCatalog.forKind` defaults per project kind
- `SolutionGraph` / `WorkspaceGraph` / `RelationGraph` — projects, documents, project-reference and dependency relations; `GraphPatch`, `GraphValidation`
- `FrozenSnapshot.freezeTree` — immutable freeze of a project closure; `RevisionLedger.reserve` — monotone revisions
- `SessionRuntime` / `SessionOrchestrator` — the session state machine over graph + registry + contents
- Ops layers — `BuildDiagnosticOps` (ingest build diagnostics), `DiagnosticIndexOps`, `DependencyRelationOps`, `CorrespondenceRelation*` (ADR↔code anchors), `Hoare` obligations, `RefactorPlan`, `SceneProjection`, `TypeSystemProfile`, `InvalidationScope`
- `WorkspaceView` — the flat projection consumers see (revision, anchor, mode, projects, documents)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Ide.Session

// Lifecycle ordering
LifecyclePhase.canAdvanceTo DesignTime CompileTime   // true
LifecyclePhase.canAdvanceTo RunTime DesignTime       // false

// A project node with default capabilities for an F# library
let proj =
    ProjectNode.create
        (ProjectId.create @"D:\repo\src\App\App.fsproj")
        (DotNet { Language = FSharp })
        @"D:\repo\src\App\App.fsproj"
        []   // capabilities — ProjectCapabilityCatalog.forKind fills the defaults

// Capability defaults: F# compiler services run out-of-process, design-time
let attrs = CapabilityAttributes.defaults OutOfProcess DesignTime
```

## Problem

- IDE and tooling state scatters across models:
  - project trees know files but not phases; build systems know diagnostics but not which project owns them
  - "what is loaded right now" is answered differently by every consumer — full graph, frozen copy, per-project view
  - revisions are timestamps or counters, each consumer inventing its own
- So two features looking at "the same session" disagree.

## Solution

- One graph + one lifecycle + one ledger: phases order loading, capabilities declare execution topology, freezes give immutable point-in-time views, revisions come from a single ledger.
- Pure model — ports (`.Ports.DotNet`, `.Ports.Workspace`) feed it pre-loaded topology; no disk IO inside.
- Battle-tested as the F# SSOT behind the Guiders session orchestrator — [ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md) · [FSHARP ADR-0004](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md).

## When not to use

- Loading solutions or reading disk — the ports packages do that; this model is pure.
- Running compiler services — capabilities carry topology markers; execution lives in the host.
- Language-level analysis — diagnostics/relations ingest here, parsing and language queries live in the Language packages.
- Small scripts that just need an MSBuild model — this graph buys you lifecycle and freezing; a plain project list is cheaper.

## Learn more

- [ADR-0062 — IDE solution session orchestrator](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md)
- [FSHARP ADR-0004 — ide-session modeling ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md)
