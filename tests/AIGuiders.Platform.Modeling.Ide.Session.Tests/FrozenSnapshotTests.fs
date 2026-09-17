namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// <summary>§11 sanity + §9.10 — freeze_tree канон: κ_π captured, r монотонен (ledger reserve), композит = ⊕ без глобальных capability-рёбер.</summary>
type FrozenSnapshotTests() =

    let makeProject (path: string) =
        let id = ProjectId.create path
        ProjectNode.create id (DotNet { Language = FSharp }) path (CapabilityCatalog.defaultDotNet ()), id

    [<Fact>]
    member _.``Local freeze captures capabilities ownership contents and advances ledger``() =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let project, id = makeProject projectPath
        let ownership = Map.ofList [ sourcePath, id ]

        let graph, _ =
            SessionTestFixtures.createGraph @"D:\repo\App.slnx" [ project ] ownership [] []

        let runtime =
            SessionTestFixtures.createRuntime graph ownership [ sourcePath, "let foo = 1" ] Unloaded

        let frozen, runtime' = SessionOrchestrator.freeze runtime (Local id)

        Assert.Equal(1, frozen.Projects.Length)
        let leaf = frozen.Projects.[0]
        Assert.Equal(1L, frozen.Revision)
        Assert.NotEmpty(leaf.Capabilities)

        let texts =
            leaf.Documents |> Map.values |> Seq.map (fun (DocumentText t) -> t) |> Seq.toList

        Assert.Contains("let foo = 1", texts)
        Assert.Equal(2L, runtime'.Ledger.NextRevision)

    [<Fact>]
    member _.``Successive freezes get strictly increasing revisions``() =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let project, id = makeProject projectPath
        let ownership = Map.ofList [ sourcePath, id ]

        let graph, _ =
            SessionTestFixtures.createGraph @"D:\repo\App.slnx" [ project ] ownership [] []

        let runtime =
            SessionTestFixtures.createRuntime graph ownership [ sourcePath, "let foo = 1" ] Unloaded

        let frozen1, runtime1 = SessionOrchestrator.freeze runtime (Local id)
        let frozen2, _ = SessionOrchestrator.freeze runtime1 (Local id)

        Assert.True(frozen1.Revision < frozen2.Revision)

    [<Fact>]
    member _.``ProjClosure composes the dependency subtree``() =
        let appPath = @"D:\repo\src\App\App.fsproj"
        let libPath = @"D:\repo\src\Lib\Lib.fsproj"
        let corePath = @"D:\repo\src\Core\Core.fsproj"
        let appFile = @"D:\repo\src\App\Program.fs"
        let libFile = @"D:\repo\src\Lib\Lib.fs"
        let coreFile = @"D:\repo\src\Core\Core.fs"

        let app, appId = makeProject appPath
        let lib, libId = makeProject libPath
        let core, coreId = makeProject corePath

        let ownership =
            Map.ofList [ appFile, appId; libFile, libId; coreFile, coreId ]

        let graph, _ =
            SessionTestFixtures.createGraph
                @"D:\repo\App.slnx"
                [ app; lib; core ]
                ownership
                []
                [ ProjectEdge.create appId libId; ProjectEdge.create libId coreId ]

        let boot = SessionTestFixtures.bootstrap ownership [ appFile, "module App"; libFile, "module Lib"; coreFile, "module Core" ]

        let frozen =
            FrozenSnapshot.freezeTree 7L graph boot.Registry boot.Contents (ProjClosure appId)

        let expected = [ appId; libId; coreId ] |> List.sort
        Assert.Equal<ProjectId>(expected, frozen.Projects |> List.map (fun p -> p.ProjectId))
        Assert.Equal(7L, frozen.Revision)
        Assert.All(frozen.Projects, (fun leaf -> Assert.NotEmpty(leaf.Capabilities)))

        let docCounts =
            frozen.Projects |> List.map (fun p -> p.ProjectId, Map.count p.Documents) |> Map.ofList

        Assert.Equal(1, Map.find appId docCounts)
        Assert.Equal(1, Map.find libId docCounts)
        Assert.Equal(1, Map.find coreId docCounts)

    [<Fact>]
    member _.``Solution freeze is a disjoint file-exhaustive composite with no cross-project edges``() =
        let appPath = @"D:\repo\src\App\App.fsproj"
        let libPath = @"D:\repo\src\Lib\Lib.fsproj"
        let appFile = @"D:\repo\src\App\Program.fs"
        let libFile = @"D:\repo\src\Lib\Lib.fs"

        let app, appId = makeProject appPath
        let lib, libId = makeProject libPath

        let ownership = Map.ofList [ appFile, appId; libFile, libId ]

        let graph, _ =
            SessionTestFixtures.createGraph
                @"D:\repo\App.slnx"
                [ app; lib ]
                ownership
                []
                [ ProjectEdge.create appId libId ]

        let boot = SessionTestFixtures.bootstrap ownership [ appFile, "module App"; libFile, "module Lib" ]

        let frozen = FrozenSnapshot.freezeTree 3L graph boot.Registry boot.Contents FreezeMode.Solution

        Assert.Equal(2, frozen.Projects.Length)

        let docCount =
            frozen.Projects |> List.sumBy (fun p -> Map.count p.Documents)

        Assert.Equal(2, docCount)
        Assert.Equal(1, graph.ProjectEdges.Length)
