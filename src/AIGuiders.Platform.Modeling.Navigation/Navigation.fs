namespace AIGuiders.Platform.Modeling.Navigation

open System.IO

/// <summary>
/// Navigation scene IR (GUIDERS-FSHARP-ADR-0003 §4.8, GUIDERS-ADR-0033) — F# SSOT.
/// Pure shapes: anchor, nodes, edges, caps. Builders live in Execution (Navigation.Code).
/// </summary>
module Schemes =
    [<Literal>]
    let SceneV1 = "navigation_scene/v1"

/// <summary>Scene mode: related neighbors or full subgraph.</summary>
type Mode =
    | Related
    | Subgraph

    override this.ToString() =
        match this with
        | Related -> "related"
        | Subgraph -> "subgraph"

/// <summary>Domain the scene is built from (language discipline is Execution's).</summary>
type Domain =
    | Code
    | Docs
    | Workspace

[<CLIMutable>]
type Anchor =
    { Path: string
      Line: int option
      Column: int option
      SolutionPath: string option }

[<CLIMutable>]
type Node =
    { Id: string
      Path: string
      Kind: string
      Rationale: string option
      RelativePath: string option
      Label: string option }

[<CLIMutable>]
type Edge =
    { FromId: string
      ToId: string
      Kind: string
      RelatedKind: string option }

[<CLIMutable>]
type SceneCaps =
    { MaxRelated: int
      MaxNodes: int
      MaxEdges: int
      Preset: string option
      KindCaps: Map<string, int> option }

[<CLIMutable>]
type Scene =
    { Schema: string
      Mode: Mode
      Anchor: Anchor
      Nodes: Node list
      Edges: Edge list
      Caps: SceneCaps
      Summary: string }

module Scene =
    /// <summary>Empty scene — same summary semantics as the transitional C# shape.</summary>
    let empty (anchor: Anchor) (mode: Mode) (caps: SceneCaps) : Scene =
        let fileName = Path.GetFileName(anchor.Path)
        { Schema = Schemes.SceneV1
          Mode = mode
          Anchor = anchor
          Nodes = []
          Edges = []
          Caps = caps
          Summary = $"Navigation (%s{mode.ToString()}): no neighbors for %s{fileName}." }