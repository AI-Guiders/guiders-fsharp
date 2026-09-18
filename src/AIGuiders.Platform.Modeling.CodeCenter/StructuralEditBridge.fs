namespace AIGuiders.Platform.Modeling.CodeCenter

open AIGuiders.Platform.Modeling.Core.Identity

/// <summary>C# interop helpers for structural edit construction.</summary>
[<RequireQualifiedAccess>]
module StructuralEditBridge =
    let insertBlock (anchorId: NodeId) (blockKind: string) (body: string) =
        StructuralEdit.InsertBlock(anchorId, blockKind, body)

    let renameMember (nodeId: NodeId) (newName: string) =
        StructuralEdit.RenameMember(nodeId, newName)
