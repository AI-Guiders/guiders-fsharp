namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

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
    | TypeUses
    | Binds
    | Imports
    | SyntaxDepends
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
    let dependencyKindToRelationType (kind: DependencyRelationKind) =
        match kind with
        | DependencyRelationKind.Uses -> RelationType.Uses
        | DependencyRelationKind.TypeUses -> RelationType.TypeUses
        | DependencyRelationKind.Binds -> RelationType.Binds
        | DependencyRelationKind.Imports -> RelationType.Imports
        | DependencyRelationKind.SyntaxDepends -> RelationType.SyntaxDepends

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

    let private graphNodeRef (node: GraphNodeId) =
        match node with
        | GraphNodeId.ProjectNode pid -> GraphNodeRef.SessionProject pid
        | GraphNodeId.CapabilityNode(pid, kind) -> GraphNodeRef.SessionCapability(pid, kind)

    let projectRef (fromPid: ProjectId) (toPid: ProjectId) =
        { From = GraphNodeRef.SessionProject fromPid
          Type = RelationType.ProjectRef
          To = GraphNodeRef.SessionProject toPid
          Scope = SessionG
          Attributes = RelationAttributes.empty }

    let orchestration
        (relationType: RelationType)
        (fromNode: GraphNodeId)
        (toNode: GraphNodeId)
        (attributes: Map<string, string>)
        =
        let kind =
            match relationType with
            | RelationType.Requires | RelationType.Invalidates | RelationType.Feeds -> relationType
            | _ -> invalidArg (nameof relationType) "orchestration requires Requires|Invalidates|Feeds"

        { From = graphNodeRef fromNode
          Type = kind
          To = graphNodeRef toNode
          Scope = SessionG
          Attributes = { Tags = []; Wire = attributes } }

    let requiresOrchestration (fromNode: GraphNodeId) (toNode: GraphNodeId) =
        orchestration RelationType.Requires fromNode toNode Map.empty

    let invalidatesOrchestration (fromNode: GraphNodeId) (toNode: GraphNodeId) =
        orchestration RelationType.Invalidates fromNode toNode Map.empty

    let feedsOrchestration (fromNode: GraphNodeId) (toNode: GraphNodeId) =
        orchestration RelationType.Feeds fromNode toNode Map.empty

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
        | RelationType.Uses | RelationType.TypeUses | RelationType.Binds ->
            if fromSort = NodeSort.SemanticSymbol && toSort = NodeSort.SemanticSymbol then
                Ok()
            else
                Error { Message = $"Dependency {relation.Type}: requires SemanticSymbol -> SemanticSymbol." }
        | RelationType.Imports ->
            let synSem sort =
                sort = NodeSort.SyntaxNode || sort = NodeSort.SemanticSymbol

            if synSem fromSort && synSem toSort then
                Ok()
            else
                Error { Message = $"Dependency Imports: endpoints must be SyntaxNode or SemanticSymbol." }
        | RelationType.SyntaxDepends ->
            if fromSort = NodeSort.SyntaxNode && toSort = NodeSort.SyntaxNode then
                Ok()
            else
                Error { Message = "Dependency SyntaxDepends: requires SyntaxNode -> SyntaxNode." }
        | RelationType.Normates ->
            if fromSort = NodeSort.DocumentFragment && toSort = NodeSort.SemanticSymbol then
                Ok()
            elif fromSort = NodeSort.SemanticSymbol && toSort = NodeSort.SemanticSymbol then
                Ok()
            else
                Error { Message = $"Correspondence Normates: invalid sort pair {fromSort} -> {toSort}." }
        | RelationType.ImplementsInterface | RelationType.Extends | RelationType.Instantiates ->
            if fromSort = NodeSort.SemanticSymbol && toSort = NodeSort.SemanticSymbol then
                Ok()
            else
                Error { Message = $"TypeSystem {relation.Type}: requires SemanticSymbol -> SemanticSymbol." }
