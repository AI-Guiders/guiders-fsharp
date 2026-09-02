namespace AIGuiders.Platform.Modeling.Ide.Session

/// <summary>Stable project identity within a session graph.</summary>
[<Struct; StructuralEquality; StructuralComparison>]
type ProjectId = ProjectId of string

module ProjectId =
    let value (ProjectId id) = id

    let create path = ProjectId(System.IO.Path.GetFullPath path)
