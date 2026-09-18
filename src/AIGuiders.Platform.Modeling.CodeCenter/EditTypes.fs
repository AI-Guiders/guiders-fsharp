namespace AIGuiders.Platform.Modeling.CodeCenter

open AIGuiders.Platform.Modeling.Core.Identity

type EditScope =
    | Point
    | Region
    | Document

type MechanicalEdit =
    { Scope: EditScope
      RemovedSpan: int * int
      InsertedText: string }

type StructuralEdit =
    | RenameMember of nodeId: NodeId * newName: string
    | InsertBlock of anchorId: NodeId * kind: GraphNodeKind * sourceLine: string
    | MoveMember of nodeId: NodeId * targetParentId: NodeId * index: int
    | Extract of nodeId: NodeId * extractedName: string

type InverseQuality =
    | Exact
    | Partial
    | Unspecified

module StructuralEdit =
    let kind =
        function
        | RenameMember _ -> "RenameMember"
        | InsertBlock _ -> "InsertBlock"
        | MoveMember _ -> "MoveMember"
        | Extract _ -> "Extract"

module MechanicalEdit =
    let kind (edit: MechanicalEdit) =
        match edit.Scope with
        | Point -> "MechanicalPoint"
        | Region -> "MechanicalRegion"
        | Document -> "MechanicalDocument"
