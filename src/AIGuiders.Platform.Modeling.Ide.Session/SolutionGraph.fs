namespace AIGuiders.Platform.Modeling.Ide.Session

type SessionEdgeKind =
    | Requires
    | Invalidates
    | Feeds

type SessionEdge =
    { From: GraphNodeId
      To: GraphNodeId
      Kind: SessionEdgeKind
      Attributes: Map<string, string> }

type DesignTimeLoadPolicy =
    | Lazy
    | Eager

type SessionPolicy =
    { DesignTimeLoad: DesignTimeLoadPolicy
      EvictOnClose: bool }

module SessionPolicy =
    let defaultPolicy =
        { DesignTimeLoad = Lazy
          EvictOnClose = true }

type SolutionGraph =
    { AnchorPath: string
      Projects: ProjectNode list
      FileOwnership: Map<string, ProjectId>
      ProjectEdges: ProjectEdge list
      Edges: SessionEdge list }

type SolutionSession =
    { Graph: SolutionGraph
      Phase: LifecyclePhase
      Policy: SessionPolicy }

module SolutionSession =
    let create anchorPath graph =
        { Graph = graph
          Phase = Unloaded
          Policy = SessionPolicy.defaultPolicy }

    let withPhase phase (session: SolutionSession) = { session with Phase = phase }

module SolutionGraph =
    let create anchorPath projects fileOwnership edges projectEdges =
        { AnchorPath = anchorPath
          Projects = projects
          FileOwnership = fileOwnership
          ProjectEdges = projectEdges
          Edges = edges }

    let tryFindProject id (graph: SolutionGraph) =
        graph.Projects |> List.tryFind (fun p -> p.Id = id)

    let nodeIds (graph: SolutionGraph) =
        seq {
            for project in graph.Projects do
                yield GraphNodeId.project project.Id

                for cap in project.Capabilities do
                    yield GraphNodeId.capability project.Id cap.Kind
        }
