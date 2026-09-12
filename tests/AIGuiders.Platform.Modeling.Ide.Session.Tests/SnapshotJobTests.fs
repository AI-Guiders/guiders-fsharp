namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

/// <summary>§04 OD3 — freeze → job handle with atRevision.</summary>
type SnapshotJobTests() =

    [<Fact>]
    member _.``Start job freezes tree and pins atRevision``() =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let id = ProjectId.create projectPath

        let project =
            ProjectNode.create id (DotNet { Language = FSharp }) projectPath (CapabilityCatalog.defaultDotNet ())

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let runtime =
            SessionOrchestrator.create
                (SolutionSession.create graph.AnchorPath graph |> SolutionSession.withPhase DesignTime)
                (Map.ofList [ sourcePath, "let foo = 1" ])

        let job, runtime' = SnapshotJob.start runtime CodeTransform id "rename-local" (Local id)

        Assert.Equal(CodeTransform, job.Kind)
        Assert.Equal(id, job.ProjectId)
        Assert.Equal(job.AtRevision + 1L, runtime'.Ledger.NextRevision)

        match job.Status with
        | JobPending -> ()
        | other -> Assert.Fail($"Expected JobPending, got {other}")
