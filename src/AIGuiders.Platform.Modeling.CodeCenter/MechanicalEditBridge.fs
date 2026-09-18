namespace AIGuiders.Platform.Modeling.CodeCenter

/// <summary>C# interop helpers for mechanical edit construction.</summary>
[<RequireQualifiedAccess>]
module MechanicalEditBridge =
    let point (offset: int) (insertedText: string) =
        { Scope = EditScope.Point
          RemovedSpan = (offset, 0)
          InsertedText = insertedText }

    let region (offset: int) (removedLength: int) (insertedText: string) =
        { Scope = EditScope.Region
          RemovedSpan = (offset, removedLength)
          InsertedText = insertedText }

    let document (offset: int) (removedLength: int) (insertedText: string) =
        { Scope = EditScope.Document
          RemovedSpan = (offset, removedLength)
          InsertedText = insertedText }
