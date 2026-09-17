module AIGuiders.Platform.Modeling.Ide.Session.Tests.DocumentRegistryTests

open Xunit
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

[<Fact>]
let ``bootstrap mints DocId and maps path to owner`` () =
    let owner = ProjectId.create @"D:\repo\App.fsproj"
    let path = @"D:\repo\Module.fs"

    let result =
        DocumentRegistryOps.bootstrap [ path, "let x = 1" ] (Map.ofList [ path, owner ]) 0L

    let docId = DocumentRegistryOps.resolvePath (LogicalPath.Create path) result.Registry
    Assert.True docId.IsSome

    match docId with
    | Some id ->
        let meta = result.Registry.[id]
        Assert.Equal(owner, meta.Owner)
        Assert.Equal("let x = 1", DocumentText.value result.Contents.[id])
    | None -> Assert.Fail "expected doc id"

[<Fact>]
let ``resolvePath returns none for unknown path`` () =
    let owner = ProjectId.create @"D:\repo\App.fsproj"

    let result =
        DocumentRegistryOps.bootstrap [ @"D:\repo\A.fs", "a" ] (Map.ofList [ @"D:\repo\A.fs", owner ]) 0L

    Assert.True(
        DocumentRegistryOps.resolvePath (LogicalPath.Create @"D:\repo\Missing.fs") result.Registry
        |> Option.isNone
    )

[<Fact>]
let ``applyPathRename updates registry path`` () =
    let owner = ProjectId.create @"D:\repo\App.fsproj"
    let oldPath = @"D:\repo\Old.fs"
    let newPath = @"D:\repo\New.fs"

    let boot =
        DocumentRegistryOps.bootstrap [ oldPath, "text" ] (Map.ofList [ oldPath, owner ]) 0L

    let registry' = DocumentRegistryOps.applyPathRename oldPath newPath boot.Registry

    Assert.True(
        DocumentRegistryOps.resolvePath (LogicalPath.Create newPath) registry'
        |> Option.isSome
    )

    Assert.True(
        DocumentRegistryOps.resolvePath (LogicalPath.Create oldPath) registry'
        |> Option.isNone
    )
