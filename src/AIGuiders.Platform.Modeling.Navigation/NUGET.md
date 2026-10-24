# AIGuiders.Platform.Modeling.Navigation

Navigation scene IR — the wire contract for "show me what's around this file": seed, nodes, edges, caps.

## What you get

- `Schemes.SceneV1` — schema literal `"navigation_scene/v1"`, the normative wire tag for a navigation scene
- `Mode` — scene grain:
  - `Related` — bounded ring of neighbors around the seed
  - `Subgraph` — bounded subgraph peel
- `Domain` — which world the scene is built from: `Code`, `Docs`, `Workspace`
- `Node` — one projected item: `Id`, `Path`, `Kind`, optional `Rationale`, `RelativePath`, `Label`
- `Edge` — projection edge `FromId`/`ToId` with `Kind` (mirrors the Ide.Session `RelationType` wire, not a parallel enum) and optional `RelatedKind`
- `SceneCaps` — the budget a scene must fit: `MaxRelated`, `MaxNodes`, `MaxEdges`, optional `Preset` and per-kind `KindCaps`
- `Scene` — the full record (schema, mode, seed, nodes, edges, caps, summary) plus `Scene.empty` — an empty scene with the canonical "no neighbors" summary
- The seed is `NavSeed` from `AIGuiders.Platform.Modeling.LanguageIntelligence.Relations` — one anchor shape shared across the family

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Navigation
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

let seed : NavSeed =
    { Path = LogicalPath.Create "src/App/Program.fs"
      Line = Some 12
      Column = None
      Command = None
      Go = None
      Solution = None
      Member = None }

let caps : SceneCaps =
    { MaxRelated = 24
      MaxNodes = 12
      MaxEdges = 24
      Preset = Some "peers_only"
      KindCaps = None }

let scene = Scene.empty seed Related caps
scene.Summary  // "Navigation (related): no neighbors for Program.fs."
```

## Problem

- "Navigate around this file" is the most-used move in a code-aware tool — related neighbors for a symbol, the subgraph under a focus file — and every tool that answers it needs the same things decided:
  - what one neighbor looks like (path? kind? why is it here?)
  - what an edge means, and whose edge vocabulary it uses
  - how big the answer is allowed to be (node/edge/per-kind caps)
- Without a shared contract, each IDE adapter invents its own JSON, and caps enforcement drifts per host.

## Solution

- One pure IR for bounded navigation scenes: nodes, edges, caps, schema tag — hosts become projectors that fill it, not owners of the shape.
- Edge `Kind` deliberately reuses the session `RelationType` wire labels, so scene edges and graph edges speak the same vocabulary.
- The F# SSOT of the Guiders navigation family ([platform ADR-0033](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0033-navigation-family-semantic-scenes.md)); scene projection from the session graph is consumed in this solution by `AIGuiders.Platform.Modeling.Ide.Session` (`SceneProjection`) — extraction mapped in [FSHARP ADR-0003 §4.8](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md).

## When not to use

- Scene building — walking a real language graph (Roslyn, FCS) and filling `Nodes`/`Edges` is Execution's job (`Navigation.Code` in the platform); this package is the contract, its only constructor is `Scene.empty`.
- Presets, kind filters, profiles and default caps — those live in `AIGuiders.Platform.Modeling.Navigation.Policy`; here `SceneCaps` is just the record the budget is written into.

## Learn more

- [ADR-0033 — Navigation family: bounded semantic scenes](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0033-navigation-family-semantic-scenes.md)
- [FSHARP ADR-0003 — Model extraction matrix (§4.8 Navigation)](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
