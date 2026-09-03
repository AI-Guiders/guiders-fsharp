namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

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

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let contents = Map.ofList [ sourcePath, "let foo = 1" ]

        let runtime =
            SessionOrchestrator.create
                (SolutionSession.create graph.AnchorPath graph |> SolutionSession.withPhase DesignTime)
                contents
                |> fun r ->
                    { r with
                        Materialized =
                            MaterializedState.mark (GraphNodeId.capability id CompilerServices) 1L MaterializedState.empty }

        let patch =
            RefactorPlan.planRename contents { OldName = "foo"; NewName = "bar"; Files = [ sourcePath ] }

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

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let contents = Map.ofList [ sourcePath, "let foo = 1" ]

        let runtime =
            SessionOrchestrator.create
                (SolutionSession.create graph.AnchorPath graph |> SolutionSession.withPhase DesignTime)
                contents

        let frozen = SessionOrchestrator.freeze runtime (Local id)
        Assert.Equal(1, frozen.Projects.Length)
        Assert.True(Map.containsKey sourcePath frozen.Projects.[0].Contents)

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

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let runtime =
            SessionOrchestrator.create
                (SolutionSession.create graph.AnchorPath graph)
                (Map.ofList [ sourcePath, "let foo = 1" ])

        match SessionOrchestrator.ensureCompilerServices runtime sourcePath with
        | Failed reason -> Assert.Fail(reason)
        | Ensured(mat, applied) ->
            Assert.Equal("fsharp", mat.LanguageId)
            Assert.Equal(InProcess, mat.Topology)
            Assert.Equal(1, applied.Materialized.Entries.Count)
            Assert.Equal(DesignTime, applied.Session.Phase)
