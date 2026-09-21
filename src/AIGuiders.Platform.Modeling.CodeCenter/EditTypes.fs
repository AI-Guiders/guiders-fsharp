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
    | InsertBlock of anchorId: NodeId * sourceLine: string
    | RemoveBlock of nodeId: NodeId
    | MoveMember of nodeId: NodeId * targetParentId: NodeId * index: int
    | Extract of nodeId: NodeId * extractedName: string

type InverseQuality =
    | Exact
    | Partial
    | Unspecified

/// Coarse projection family (C# ProjectionKind parity). Plugin identity is PluginId string.
type DocumentProjectionKind =
    | Text = 0
    | Diagram = 1
    | Tree = 2
    | Form = 3
    | Preview = 4

type ProjectionDescriptor =
    { Kind: DocumentProjectionKind
      PluginId: string
      NodeId: string option
      Dialect: string option }

module StructuralEdit =
    let kind =
        function
        | RenameMember _ -> "RenameMember"
        | InsertBlock _ -> "InsertBlock"
        | RemoveBlock _ -> "RemoveBlock"
        | MoveMember _ -> "MoveMember"
        | Extract _ -> "Extract"

module MechanicalEdit =
    let kind (edit: MechanicalEdit) =
        match edit.Scope with
        | Point -> "MechanicalPoint"
        | Region -> "MechanicalRegion"
        | Document -> "MechanicalDocument"

module ProjectionDescriptor =
    let text =
        { Kind = DocumentProjectionKind.Text
          PluginId = "core.text"
          NodeId = None
          Dialect = None }

    let treeOutline =
        { Kind = DocumentProjectionKind.Tree
          PluginId = "tree.outline"
          NodeId = None
          Dialect = None }

    /// Default session advertisement; host intersects with installed plugins.
    let defaultAvailable () : ProjectionDescriptor list =
        [ text; treeOutline ]
