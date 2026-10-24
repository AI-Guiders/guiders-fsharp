# AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

Pure adapters from pre-loaded .NET solution topology (.slnx / msbuild project rows) into the IDE session graph — no disk IO inside.

## What you get

- `DotNetSlnxGraphPort.buildGraph` — topology rows → `SolutionGraph`: project nodes (C#/F# kinds), project-reference relations, document ownership
- `DotNetSlnxGraphPort.buildProjectNodes` / `buildProjectRefRelations` / `buildDocumentOwnership` — the same steps, individually
- `MsBuildSolutionProvider` — `ISolutionInfoProvider` ("msbuild") over pre-loaded rows: name, fingerprint, entries, relations
- `WorkspaceViewPort.emit` — frozen snapshot + graph → `WorkspaceView` (projects with language ids, unified documents, revision/anchor/mode)
- `DesignTimeCompilerServicesPort.materialize` — ADR-0062 §5 freeze-tree port: resolve the owning project, freeze the closure, emit the view, mark the capability materialized (returns `Ensured` / `Failed` with stable reason strings)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

// You load the rows (msbuild/slnx reader of your choice); the port stays pure:
//   topology : DotNetSolutionTopology   (SolutionPath, Fingerprint, Rows)
let graph = DotNetSlnxGraphPort.buildGraph topology

// Same topology as an ISolutionInfoProvider for composition roots
let provider = MsBuildSolutionProvider.create topology
let entries  = (provider :> ISolutionInfoProvider).Entries()
```

## Problem

- Every tool that wants "the solution as a graph" re-writes the same glue:
  - parse .slnx / walk msbuild projects → project list
  - resolve project references into relations
  - decide which project owns which source file
- The glue is IO-heavy, so the model built on top of it becomes untestable.

## Solution

- IO stays at the edge: callers pre-load topology rows, the port turns them into the session graph purely — the graph layer stays testable with synthetic rows.
- The freeze/view pipeline (`materialize`, `emit`) is the same port the federation orchestrator uses.
- Battle-tested as the .NET session port of the Guiders federation — [FSHARP ADR-0004](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) · [ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md).

## When not to use

- Loading the topology — this port consumes rows; msbuild invocation and file reads are yours (or the Execution layer's).
- Unknown project kinds throw (`Unsupported managed project`) — wrap mixed solutions with your own filter if you expect non-C#/F# entries.
- Non-.NET workspaces — `.Ports.Workspace` covers document-centric graphs.

## Learn more

- [FSHARP ADR-0004 — ide-session modeling ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md)
- [ADR-0062 — IDE solution session orchestrator](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
