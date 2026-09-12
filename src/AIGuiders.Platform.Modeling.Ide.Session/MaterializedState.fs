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

    let markStale (node: GraphNodeId) (state: MaterializedState) =
        match Map.tryFind node state.Entries with
        | None -> state
        | Some entry ->
            { state with
                Entries = Map.add node { entry with Stale = true } state.Entries }

    let evict (node: GraphNodeId) (state: MaterializedState) =
        { state with Entries = Map.remove node state.Entries }

    /// §5.2 + I6: FileChange does not evict M; coarser scopes evict affected capabilities.
    /// based on adr: docs/math/ide-session/02-invalidation.md §5.2 I1, I4, I6
    module Invalidation =
        let private affectedProjectsFromFilePatch (patch: SessionPatch) (graph: SolutionGraph) =
            let paths =
                [ yield! patch.FileSystem.Writes |> List.map fst
                  yield! patch.FileSystem.Deletes

                  for oldPath, newPath in patch.FileSystem.PathRenames do
                      yield oldPath
                      yield newPath

                  yield! patch.Graph.FileOwnershipUpdates |> List.map fst ]

            paths
            |> List.choose (fun path -> Map.tryFind path graph.FileOwnership)
            |> List.distinct

        let private subtreeNodeIds (graph: SolutionGraph) (projectId: ProjectId) =
            seq {
                yield GraphNodeId.project projectId

                match graph |> SolutionGraph.tryFindProject projectId with
                | None -> ()
                | Some project ->
                    for cap in project.Capabilities do
                        yield GraphNodeId.capability projectId cap.Kind
            }

        let private evictSubtree (graph: SolutionGraph) (projectId: ProjectId) (state: MaterializedState) =
            let victims = subtreeNodeIds graph projectId |> Set.ofSeq

            { state with
                Entries = state.Entries |> Map.filter (fun node _ -> not (Set.contains node victims)) }

        let private markCompilerServicesStale (projectId: ProjectId) (state: MaterializedState) =
            state |> markStale (GraphNodeId.capability projectId CompilerServices)

        let forScope (scope: InvalidationScope) (graph: SolutionGraph) (patch: SessionPatch) (state: MaterializedState) =
            match scope with
            | FileChange -> state
            | ProjectFileCrud ->
                (state, affectedProjectsFromFilePatch patch graph)
                ||> List.fold (fun acc projectId -> markCompilerServicesStale projectId acc)
            | ProjectCrud ->
                (state,
                 patch.Graph.ProjectMetadataUpdates |> List.map (fun p -> p.Id))
                ||> List.fold (fun acc projectId -> evictSubtree graph projectId acc)
            | SolutionProjectCrud ->
                (state, patch.Graph.ProjectsRemoved)
                ||> List.fold (fun acc projectId -> evictSubtree graph projectId acc)
