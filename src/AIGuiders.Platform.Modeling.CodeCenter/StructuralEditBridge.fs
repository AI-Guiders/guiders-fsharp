namespace AIGuiders.Platform.Modeling.CodeCenter

open AIGuiders.Platform.Modeling.Core.Identity

/// <summary>C# interop helpers for structural edit construction.</summary>
[<RequireQualifiedAccess>]
module StructuralEditBridge =
    let insertBlock (anchorId: NodeId) (kind: GraphNodeKind) (sourceLine: string) =
        StructuralEdit.InsertBlock(anchorId, kind, sourceLine)

    let renameMember (nodeId: NodeId) (newName: string) =
        StructuralEdit.RenameMember(nodeId, newName)
