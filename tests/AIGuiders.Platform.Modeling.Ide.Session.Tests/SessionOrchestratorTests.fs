namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type SessionOrchestratorTests() =

    [<Fact>]
    member _.``FileChange patch does not evict materialized state``() =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let id = ProjectId.create projectPath

        let project =
            ProjectNode.create
                id
                (DotNet { Language = FSharp })
                projectPath
                (CapabilityCatalog.defaultDotNet ())

        let ownership = Map.ofList [ sourcePath, id ]

        let graph, _ =
            SessionTestFixtures.createGraph @"D:\repo\App.slnx" [ project ] ownership [] []

        let runtime =
            SessionTestFixtures.createRuntime graph ownership [ sourcePath, "let foo = 1" ] DesignTime
            |> fun r ->
                { r with
                    Materialized =
                        MaterializedState.mark (GraphNodeId.capability id CompilerServices) 1L MaterializedState.empty }

        let patch =
            RefactorPlan.planRename runtime.Registry runtime.Contents { OldName = "foo"; NewName = "bar"; Files = [ sourcePath ] }

        match SessionOrchestrator.applyPatch runtime patch { Commit = None } with
        | PatchRejected reasons -> Assert.Fail(String.concat "; " reasons)
        | PatchApplied applied -> Assert.Equal(1, applied.Materialized.Entries.Count)

    [<Fact>]
    member _.``Freeze tree local captures project contents``() =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let id = ProjectId.create projectPath

        let project =
            ProjectNode.create
                id
                (DotNet { Language = FSharp })
                projectPath
                (CapabilityCatalog.defaultDotNet ())

        let ownership = Map.ofList [ sourcePath, id ]

        let graph, _ =
            SessionTestFixtures.createGraph @"D:\repo\App.slnx" [ project ] ownership [] []

        let runtime =
            SessionTestFixtures.createRuntime graph ownership [ sourcePath, "let foo = 1" ] DesignTime

        let frozen, runtime' = SessionOrchestrator.freeze runtime (Local id)
        Assert.Equal(1, frozen.Projects.Length)

        let texts =
            frozen.Projects.[0].Documents
            |> Map.values
            |> Seq.map DocumentText.value
            |> Seq.toList

        Assert.Contains("let foo = 1", texts)
        Assert.Equal(frozen.Revision + 1L, runtime'.Ledger.NextRevision)

    [<Fact>]
    member _.``EnsureCompilerServices marks in-process compiler capability``() =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let id = ProjectId.create projectPath

        let project =
            ProjectNode.create
                id
                (DotNet { Language = FSharp })
                projectPath
                (CapabilityCatalog.defaultDotNet ())

        let ownership = Map.ofList [ sourcePath, id ]

        let graph, _ =
            SessionTestFixtures.createGraph @"D:\repo\App.slnx" [ project ] ownership [] []

        let runtime =
            SessionTestFixtures.createRuntime graph ownership [ sourcePath, "let foo = 1" ] Unloaded

        match DesignTimeCompilerServicesPort.materialize runtime sourcePath with
        | Failed reason -> Assert.Fail(reason)
        | Ensured(mat, applied) ->
            Assert.Equal("fsharp", mat.LanguageId)
            Assert.Equal(InProcess, mat.Topology)
            Assert.Equal(1, applied.Materialized.Entries.Count)
            Assert.Equal(DesignTime, applied.Session.Phase)
            Assert.Equal(1, mat.WorkspaceView.Projects.Length)
