namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open AIGuiders.Platform.Modeling.Paths
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// Shared graph/runtime bootstrap for session tests (DocumentRegistry canon).
module SessionTestFixtures =
    let requiresOrchestration (fromNode: GraphNodeId) (toNode: GraphNodeId) =
        RelationGraph.fromSessionEdge
            { From = fromNode
              To = toNode
              Kind = SessionEdgeKind.Requires
              Attributes = Map.empty }

    let projectRef (fromPid: ProjectId) (toPid: ProjectId) =
        RelationGraph.fromProjectEdge (ProjectEdge.create fromPid toPid)

    let createGraph (anchorPath: string) projects ownership (relations: Relation list) =
        SolutionGraph.create (LogicalPath.Create anchorPath) projects relations,
        ownership

    let bootstrap (ownership: Map<string, ProjectId>) (pathContents: (string * string) list) =
        DocumentRegistryOps.bootstrap pathContents ownership 0L

    let createRuntime graph ownership pathContents phase =
        let session = SolutionSession.create graph.Anchor graph |> SolutionSession.withPhase phase

        SessionOrchestrator.create session pathContents ownership

    let registryForOwnership (ownership: Map<string, ProjectId>) =
        bootstrap ownership (ownership |> Map.toList |> List.map (fun (p, _) -> p, ""))
        |> fun b -> b.Registry

    let contentsByPath (registry: DocumentRegistry) (contents: Map<DocId, DocumentText>) =
        registry
        |> Map.toList
        |> List.choose (fun (docId, meta) ->
            Map.tryFind docId contents
            |> Option.map (fun (DocumentText text) -> meta.Path.Value, text))
        |> Map.ofList
