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

        let ownership = Map.ofList [ sourcePath, id ]

        let graph, _ =
            SessionTestFixtures.createGraph @"D:\repo\App.slnx" [ project ] ownership [] 

        let runtime =
            SessionTestFixtures.createRuntime graph ownership [ sourcePath, "let foo = 1" ] DesignTime

        let job, runtime' = SnapshotJob.start runtime CodeTransform id "rename-local" (Local id)

        Assert.Equal(CodeTransform, job.Kind)
        Assert.Equal(id, job.ProjectId)
        Assert.Equal(job.AtRevision + 1L, runtime'.Ledger.NextRevision)

        match job.Status with
        | JobPending -> ()
        | other -> Assert.Fail($"Expected JobPending, got {other}")
