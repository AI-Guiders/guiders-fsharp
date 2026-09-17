namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Navigation

/// <summary>Navigation.Scene edges are projections of G — not an edge SSOT (plan §2.5).</summary>
module SceneProjection =
    /// <summary>Wire label for a persisted <see cref="RelationType"/> edge in scene JSON.</summary>
    let relationTypeWire =
        function
        | RelationType.ProjectRef -> "project_ref"
        | RelationType.Requires -> "requires"
        | RelationType.Invalidates -> "invalidates"
        | RelationType.Feeds -> "feeds"
        | RelationType.Uses -> "uses"
        | RelationType.Normates -> "normates"
        | RelationType.ImplementsInterface -> "implements_interface"
        | RelationType.Extends -> "extends"
        | RelationType.Instantiates -> "instantiates"

    /// <summary>Roslyn/workspace related neighbor — provisional until Execution emits Relations.</summary>
    [<Literal>]
    let RelatedToWire = "related_to"

    let toEdge (fromId: string) (toId: string) (relation: Relation) : Edge =
        { FromId = fromId
          ToId = toId
          Kind = relationTypeWire relation.Type
          RelatedKind = None }

    let relatedNeighborEdge (fromId: string) (toId: string) (relatedKind: string) : Edge =
        { FromId = fromId
          ToId = toId
          Kind = RelatedToWire
          RelatedKind = Some relatedKind }
