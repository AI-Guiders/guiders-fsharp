namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Paths

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
    { Anchor: LogicalPath
      Projects: ProjectNode list
      Relations: Relation list }

type SolutionSession =
    { Graph: SolutionGraph
      Phase: LifecyclePhase
      Policy: SessionPolicy }

module SolutionSession =
    let create (anchor: LogicalPath) graph =
        { Graph = graph
          Phase = Unloaded
          Policy = SessionPolicy.defaultPolicy }

    let withPhase phase (session: SolutionSession) = { session with Phase = phase }

module SolutionGraph =
    let create (anchor: LogicalPath) projects relations =
        { Anchor = anchor
          Projects = projects
          Relations = relations }

    let projectRefEdges (graph: SolutionGraph) =
        graph.Relations |> List.filter (fun r -> r.Type = RelationType.ProjectRef)

    let orchestrationEdges (graph: SolutionGraph) =
        graph.Relations
        |> List.filter (fun r ->
            match r.Type with
            | RelationType.Requires | RelationType.Invalidates | RelationType.Feeds -> true
            | _ -> false)

    let tryFindProject id (graph: SolutionGraph) =
        graph.Projects |> List.tryFind (fun p -> p.Id = id)

    let nodeIds (graph: SolutionGraph) =
        seq {
            for project in graph.Projects do
                yield GraphNodeId.project project.Id

                for cap in project.Capabilities do
                    yield GraphNodeId.capability project.Id cap.Kind
        }
