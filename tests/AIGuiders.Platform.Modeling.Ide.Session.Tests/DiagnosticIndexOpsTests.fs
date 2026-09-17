namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type DiagnosticIndexOpsTests() =

    [<Fact>]
    member _.``Ingest mints DiagnosticRef and stores rule Code``() =
        let pid = ProjectId.create @"D:\repo\App.fsproj"
        let graph, ownership = SessionTestFixtures.createGraph "repo" [] (Map [ "src/Foo.fs", pid ]) [] []
        let runtime = SessionTestFixtures.createRuntime graph ownership [ "src/Foo.fs", "let x = 1" ] Unloaded

        let docId =
            runtime.Registry
            |> Map.toList
            |> List.head
            |> fst

        let incoming : DiagnosticIndexOps.IncomingDiagnostic =
            { Code = "CS0246"
              Severity = "error"
              Message = "type not found"
              Doc = docId
              Span = { StartLine = 1; StartCol = 1; EndLine = 1; EndCol = 5 }
              Tags = []
              Language = "fsharp"
              SurfaceVersion = SurfaceVersion 1L }

        let updated = DiagnosticIndexOps.ingest [ incoming ] runtime

        Assert.Equal(1, Map.count updated.Diagnostics)

        match DiagnosticIndexOps.tryFindByCode "CS0246" updated.Diagnostics with
        | Some(_, record) -> Assert.Equal("type not found", record.Message)
        | None -> Assert.Fail "expected ingested diagnostic"

    [<Fact>]
    member _.``PickerChoices lists ingested diagnostics for attach``() =
        let pid = ProjectId.create @"D:\repo\App.fsproj"
        let graph, ownership = SessionTestFixtures.createGraph "repo" [] (Map [ "src/Foo.fs", pid ]) [] []
        let runtime = SessionTestFixtures.createRuntime graph ownership [ "src/Foo.fs", "let x = 1" ] Unloaded

        let docId =
            runtime.Registry
            |> Map.toList
            |> List.head
            |> fst

        let incoming : DiagnosticIndexOps.IncomingDiagnostic =
            { Code = "CS0246"
              Severity = "error"
              Message = "type not found"
              Doc = docId
              Span = { StartLine = 1; StartCol = 1; EndLine = 1; EndCol = 5 }
              Tags = []
              Language = "fsharp"
              SurfaceVersion = SurfaceVersion 1L }

        let updated = DiagnosticIndexOps.ingest [ incoming ] runtime
        let choices = DiagnosticIndexOps.pickerChoices updated.Diagnostics

        Assert.Equal(1, choices.Length)
        Assert.Equal("1", choices.[0].Id)
        Assert.Contains("CS0246", choices.[0].Label)
