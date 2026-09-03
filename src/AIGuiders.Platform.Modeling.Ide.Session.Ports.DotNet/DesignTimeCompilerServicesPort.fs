namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open AIGuiders.Platform.Modeling.Ide.Session

/// <summary>ADR-0062 §5 — freeze_tree(ProjClosure) → WorkspaceView + materialize mark in <c>M</c>.</summary>
module DesignTimeCompilerServicesPort =
    let materialize (runtime: SessionRuntime) (filePath: string) : CompilerServicesEnsureResult =
        let graph = runtime.Session.Graph

        match CompilerServicesMaterialization.tryResolveProjectId graph filePath with
        | None -> Failed "file_not_owned_by_session_graph"
        | Some projectId ->
            match SolutionGraph.tryFindProject projectId graph with
            | None -> Failed "project_not_in_graph"
            | Some project ->
                match CompilerServicesMaterialization.tryGetCompilerServices project with
                | None -> Failed "compiler_services_capability_missing"
                | Some cap ->
                    let frozen =
                        FrozenSnapshot.freezeTree
                            (RevisionLedger.currentRevision runtime.Ledger + 1L)
                            graph
                            runtime.Contents
                            (ProjClosure projectId)

                    let view = WorkspaceViewPort.emit graph projectId frozen
                    let node = GraphNodeId.capability projectId CompilerServices
                    let revision = frozen.Revision

                    let materialized =
                        runtime.Materialized |> MaterializedState.mark node revision

                    let session' =
                        runtime.Session |> SolutionSession.withPhase DesignTime

                    let runtime' =
                        { runtime with
                            Session = session'
                            Materialized = materialized }

                    Ensured(
                        { ProjectId = projectId
                          CapabilityNode = node
                          Topology = CompilerServicesMaterialization.resolveTopology cap.Attributes
                          TopologyWire =
                              CompilerServicesMaterialization.resolveTopology cap.Attributes
                              |> ExecutionTopology.toWire
                          LanguageId = CompilerServicesMaterialization.languageIdForProject project
                          Revision = revision
                          WorkspaceView = view },
                        runtime'
                    )
