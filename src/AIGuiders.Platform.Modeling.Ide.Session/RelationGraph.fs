namespace AIGuiders.Platform.Modeling.Ide.Session

open System
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type SessionEdgeKind =
    | Requires
    | Invalidates
    | Feeds

[<Obsolete("Use Relation in SolutionGraph.Relations. Migrate via RelationGraph.fromSessionEdge.", false)>]
type SessionEdge =
    { From: GraphNodeId
      To: GraphNodeId
      Kind: SessionEdgeKind
      Attributes: Map<string, string> }

type NodeSort =
    | SessionProject
    | SessionCapability
    | DocumentFragment
    | AdrObligation
    | SyntaxNode
    | SemanticSymbol
    | Artifact

type GraphNodeRef =
    | SessionProject of ProjectId
    | SessionCapability of ProjectId * CapabilityKind
    | Document of DocumentRef
    | Syntax of DocumentRef * NodeId
    | Semantic of DocumentRef * SymbolRef
    | ArtifactNode of ArtifactRef

type RelationType =
    | ProjectRef
    | Requires
    | Invalidates
    | Feeds
    | Uses
    | Normates
    | ImplementsInterface
    | Extends
    | Instantiates

type RelationScope =
    | SessionG
    | SemanticSubstrate of ProjectId

type RelationAttributes =
    { Tags: string list
      Wire: Map<string, string> }

module RelationAttributes =
    let empty = { Tags = []; Wire = Map.empty }

type Relation =
    { From: GraphNodeRef
      Type: RelationType
      To: GraphNodeRef
      Scope: RelationScope
      Attributes: RelationAttributes }

type SortError = { Message: string }

module RelationGraph =
    let private sortOfNode (node: GraphNodeRef) =
        match node with
        | GraphNodeRef.SessionProject _ -> NodeSort.SessionProject
        | GraphNodeRef.SessionCapability _ -> NodeSort.SessionCapability
        | GraphNodeRef.Document _ -> NodeSort.DocumentFragment
        | GraphNodeRef.Syntax _ -> NodeSort.SyntaxNode
        | GraphNodeRef.Semantic _ -> NodeSort.SemanticSymbol
        | GraphNodeRef.ArtifactNode _ -> NodeSort.Artifact

    let private allowedOrchestration =
        [ RelationType.Requires, NodeSort.SessionCapability, NodeSort.SessionCapability
          RelationType.Invalidates, NodeSort.SessionCapability, NodeSort.SessionCapability
          RelationType.Feeds, NodeSort.SessionCapability, NodeSort.SessionCapability
          RelationType.ProjectRef, NodeSort.SessionProject, NodeSort.SessionProject ]

    let private matchesOrchestration (relationType: RelationType) fromSort toSort =
        allowedOrchestration
        |> List.exists (fun (t, dom, cod) -> t = relationType && dom = fromSort && cod = toSort)

    let private projectOfNode (node: GraphNodeRef) =
        match node with
        | GraphNodeRef.SessionProject pid -> Some pid
        | GraphNodeRef.SessionCapability(pid, _) -> Some pid
        | _ -> None

    let validateRelation (relation: Relation) =
        let fromSort = sortOfNode relation.From
        let toSort = sortOfNode relation.To

        match relation.Type with
        | RelationType.Requires | RelationType.Invalidates | RelationType.Feeds ->
            if not (matchesOrchestration relation.Type fromSort toSort) then
                Error { Message = $"Orchestration {relation.Type}: invalid sort pair {fromSort} -> {toSort}." }
            else
                match projectOfNode relation.From, projectOfNode relation.To with
                | Some fromProject, Some toProject when fromProject <> toProject ->
                    Error { Message = $"Orchestration {relation.Type}: cross-project endpoints are invalid." }
                | _ -> Ok()
        | RelationType.ProjectRef ->
            if fromSort = NodeSort.SessionProject && toSort = NodeSort.SessionProject then
                Ok()
            else
                Error { Message = "ProjectRef requires SessionProject endpoints." }
        | RelationType.Uses | RelationType.Normates ->
            if fromSort = NodeSort.SemanticSymbol && toSort = NodeSort.SemanticSymbol then
                Ok()
            elif fromSort = NodeSort.DocumentFragment && toSort = NodeSort.SemanticSymbol then
                Ok()
            else
                Error { Message = $"Correspondence/dependency {relation.Type}: invalid sort pair {fromSort} -> {toSort}." }
        | RelationType.ImplementsInterface | RelationType.Extends | RelationType.Instantiates ->
            if fromSort = NodeSort.SemanticSymbol && toSort = NodeSort.SemanticSymbol then
                Ok()
            else
                Error { Message = $"TypeSystem {relation.Type}: requires SemanticSymbol -> SemanticSymbol." }

    let fromSessionEdge (edge: SessionEdge) =
        let fromNode =
            match edge.From with
            | GraphNodeId.ProjectNode pid -> GraphNodeRef.SessionProject pid
            | GraphNodeId.CapabilityNode(pid, kind) -> GraphNodeRef.SessionCapability(pid, kind)

        let toNode =
            match edge.To with
            | GraphNodeId.ProjectNode pid -> GraphNodeRef.SessionProject pid
            | GraphNodeId.CapabilityNode(pid, kind) -> GraphNodeRef.SessionCapability(pid, kind)

        let relationType =
            match edge.Kind with
            | SessionEdgeKind.Requires -> RelationType.Requires
            | SessionEdgeKind.Invalidates -> RelationType.Invalidates
            | SessionEdgeKind.Feeds -> RelationType.Feeds

        { From = fromNode
          Type = relationType
          To = toNode
          Scope = SessionG
          Attributes = { Tags = []; Wire = edge.Attributes } }

    let fromProjectEdge (edge: ProjectEdge) =
        { From = GraphNodeRef.SessionProject edge.From
          Type = RelationType.ProjectRef
          To = GraphNodeRef.SessionProject edge.To
          Scope = SessionG
          Attributes = RelationAttributes.empty }

    [<Obsolete("Use SolutionGraph.Relations (Relation list).", false)>]
    let fromLegacy (projectEdges: ProjectEdge list) (sessionEdges: SessionEdge list) =
        [ yield! sessionEdges |> List.map fromSessionEdge
          yield! projectEdges |> List.map fromProjectEdge ]
