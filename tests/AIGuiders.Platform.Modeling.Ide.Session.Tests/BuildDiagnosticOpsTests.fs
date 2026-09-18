namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Build
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type BuildDiagnosticOpsTests() =

    [<Fact>]
    member _.``Ingest mints DiagnosticRef via DiagnosticIndex not array index stub``() =
        let pid = ProjectId.create @"D:\repo\App.fsproj"
        let graph, ownership = SessionTestFixtures.createGraph "repo" [] (Map [ "src/Foo.fs", pid ]) []
        let runtime = SessionTestFixtures.createRuntime graph ownership [ "src/Foo.fs", "let x = 1" ] Unloaded

        let raw =
            [| { File = "src/Foo.fs"
                 Line = 12
                 Column = 3
                 Code = "CS0246"
                 Message = "type not found" } |]

        let result = BuildDiagnosticOps.ingest raw runtime

        Assert.Equal(0, result.SkippedUnregistered)
        Assert.Equal(1, result.Diagnostics.Length)
        Assert.Equal(1, Map.count result.Runtime.Diagnostics)

        match result.Diagnostics.[0].Spec with
        | RelationSpec.Diag ref ->
            match Map.tryFind ref result.Runtime.Diagnostics with
            | Some record ->
                Assert.Equal("CS0246", record.Code)
                Assert.Equal("type not found", record.Message)
            | None -> Assert.Fail "DiagnosticRef must exist in session index"
        | _ -> Assert.Fail "expected RelationSpec.Diag"

    [<Fact>]
    member _.``Ingest skips diagnostics for unregistered paths``() =
        let pid = ProjectId.create @"D:\repo\App.fsproj"
        let graph, ownership = SessionTestFixtures.createGraph "repo" [] (Map [ "src/Foo.fs", pid ]) []
        let runtime = SessionTestFixtures.createRuntime graph ownership [ "src/Foo.fs", "let x = 1" ] Unloaded

        let raw =
            [| { File = "src/Missing.fs"
                 Line = 1
                 Column = 1
                 Code = "CS0001"
                 Message = "missing file" } |]

        let result = BuildDiagnosticOps.ingest raw runtime

        Assert.Equal(1, result.SkippedUnregistered)
        Assert.Empty(result.Diagnostics)
        Assert.Equal(0, Map.count result.Runtime.Diagnostics)
