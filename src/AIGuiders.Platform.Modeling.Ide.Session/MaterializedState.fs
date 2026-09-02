namespace AIGuiders.Platform.Modeling.Ide.Session

type MaterializedCapability =
    { Node: GraphNodeId
      Revision: int64
      Stale: bool }

type MaterializedState = { Entries: Map<GraphNodeId, MaterializedCapability> }

module MaterializedState =
    let empty = { Entries = Map.empty }

    let mark (node: GraphNodeId) (revision: int64) (state: MaterializedState) =
        let entry = { Node = node; Revision = revision; Stale = false }

        { state with
            Entries = Map.add node entry state.Entries }

    let evict (node: GraphNodeId) (state: MaterializedState) =
        { state with Entries = Map.remove node state.Entries }

    /// §5.2 + I6: FileChange does not evict M; coarser scopes evict affected capabilities.
    module Invalidation =
        let forScope (scope: InvalidationScope) (_graph: SolutionGraph) (state: MaterializedState) =
            match scope with
            | FileChange -> state
            | ProjectFileCrud
            | ProjectCrud
            | SolutionProjectCrud -> { Entries = Map.empty }
