namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

module GoldenSessions =
    let renameLocalSymbol =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let id = ProjectId.create projectPath

        let project =
            ProjectNode.create
                id
                (DotNet { Language = FSharp })
                projectPath
                (CapabilityCatalog.defaultDotNet ())

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let contents =
            Map.ofList
                [ sourcePath, "module App\n\nlet foo = 1\nlet twice x = x + x\n" ]

        GoldenSession.create "rename-local-symbol" graph contents DesignTime

    let moveTypeToFile =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let targetPath = @"D:\repo\src\App\Foo.fs"
        let id = ProjectId.create projectPath

        let project =
            ProjectNode.create
                id
                (DotNet { Language = FSharp })
                projectPath
                (CapabilityCatalog.defaultDotNet ())

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let contents =
            Map.ofList
                [ sourcePath,
                  "module App\n\ntype Foo = { X: int }\n\nlet useFoo () = Foo { X = 1 }\n" ]

        GoldenSession.create "move-type-to-file" graph contents DesignTime

type ConformanceGoldenSessionTests() =

    [<Fact>]
    member _.``Rename plan satisfies RF6 Hoare on golden session``() =
        let session = GoldenSessions.renameLocalSymbol
        let sourcePath = @"D:\repo\src\App\Module.fs"

        let result =
            Conformance.runRenameGolden
                session
                { OldName = "foo"
                  NewName = "bar"
                  Files = [ sourcePath ] }
                Passed

        match result with
        | Satisfied -> ()
        | Violated vs -> Assert.Fail(String.concat "; " vs)

    [<Fact>]
    member _.``Rename without typecheck violates Q_types``() =
        let session = GoldenSessions.renameLocalSymbol
        let sourcePath = @"D:\repo\src\App\Module.fs"

        let result =
            Conformance.runRenameGolden
                session
                { OldName = "foo"
                  NewName = "bar"
                  Files = [ sourcePath ] }
                NotRun

        match result with
        | Violated vs -> Assert.Contains(vs, fun v -> v.Contains("typecheck"))
        | Satisfied -> Assert.Fail("Expected Q_types violation when typecheck not run.")

    [<Fact>]
    member _.``Move type plan satisfies RF6 and updates omega``() =
        let session = GoldenSessions.moveTypeToFile
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let targetPath = @"D:\repo\src\App\Foo.fs"
        let owner = ProjectId.create @"D:\repo\src\App\App.fsproj"

        let result =
            Conformance.runMoveTypeGolden
                session
                { TypeName = "Foo"
                  SourcePath = sourcePath
                  TargetPath = targetPath
                  Owner = owner
                  UpdatedSourceContents = "module App\n\nlet useFoo () = Foo { X = 1 }\n"
                  ExtractedContents = "module App\n\ntype Foo = { X: int }\n" }
                Passed

        match result with
        | Satisfied ->
            let patch =
                RefactorPlan.planMoveTypeToFile
                    { TypeName = "Foo"
                      SourcePath = sourcePath
                      TargetPath = targetPath
                      Owner = owner
                      UpdatedSourceContents = "module App\n\nlet useFoo () = Foo { X = 1 }\n"
                      ExtractedContents = "module App\n\ntype Foo = { X: int }\n" }

            let graph', _ = SessionPatch.apply session.Graph session.Contents patch
            Assert.True(Map.containsKey targetPath graph'.FileOwnership)

            let validation = GraphValidation.validate graph'
            Assert.True(validation.IsValid, validation.Issues |> List.map (fun i -> i.Message) |> String.concat "; ")
        | Violated vs -> Assert.Fail(String.concat "; " vs)

    [<Fact>]
    member _.``Vendor style without typecheck is rejected for auto-apply``() =
        let decision = StyleConformance.evaluateAutoApply Vendor NotRun

        match decision with
        | Rejected reason -> Assert.Contains("ST5", reason)
        | _ -> Assert.Fail($"Expected Rejected, got {decision}")

    [<Fact>]
    member _.``Vendor style with passed typecheck stays preview-only``() =
        let decision = StyleConformance.evaluateAutoApply Vendor Passed

        match decision with
        | PreviewOnly reason -> Assert.Contains("ST3", reason)
        | _ -> Assert.Fail($"Expected PreviewOnly, got {decision}")

    [<Fact>]
    member _.``Proven style requires passed typecheck for auto-apply``() =
        Assert.Equal(AutoApplyAllowed, StyleConformance.evaluateAutoApply Proven Passed)

        match StyleConformance.evaluateAutoApply Proven NotRun with
        | Rejected reason -> Assert.Contains("ST5", reason)
        | _ -> Assert.Fail("Proven path must reject when typecheck not run.")
