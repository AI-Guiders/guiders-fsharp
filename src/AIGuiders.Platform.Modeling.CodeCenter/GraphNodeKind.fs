namespace AIGuiders.Platform.Modeling.CodeCenter

/// Language-neutral structural role of a node in the document graph (not planet syntax tokens).
type GraphNodeKind =
    | Module
    | Block
    | EndMarker
    | Synthetic

module GraphNodeKind =
    let isFormField = function
        | Block -> true
        | _ -> false

    let isDiagramBox = function
        | EndMarker -> false
        | _ -> true
