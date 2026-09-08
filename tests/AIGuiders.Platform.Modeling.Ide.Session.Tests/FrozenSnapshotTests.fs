namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

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

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let contents = Map.ofList [ sourcePath, "let foo = 1" ]
        let runtime = SessionOrchestrator.create (SolutionSession.create graph.AnchorPath graph) contents

        let frozen, runtime' = SessionOrchestrator.freeze runtime (Local id)

        Assert.Equal(1, frozen.Projects.Length)
        let leaf = frozen.Projects.[0]
        Assert.Equal(1L, frozen.Revision)
        Assert.NotEmpty(leaf.Capabilities)
        Assert.True(Map.containsKey sourcePath leaf.Contents)
        Assert.Equal("let foo = 1", Map.find sourcePath leaf.Contents)
        Assert.Equal(2L, runtime'.Ledger.NextRevision)

    [<Fact>]
    member _.``Successive freezes get strictly increasing revisions``() =
        let projectPath = @"D:\repo\src\App\App.fsproj"
        let sourcePath = @"D:\repo\src\App\Module.fs"
        let project, id = makeProject projectPath

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ project ]
                (Map.ofList [ sourcePath, id ])
                []
                []

        let contents = Map.ofList [ sourcePath, "let foo = 1" ]
        let runtime = SessionOrchestrator.create (SolutionSession.create graph.AnchorPath graph) contents

        let frozen1, runtime1 = SessionOrchestrator.freeze runtime (Local id)
        let frozen2, runtime2 = SessionOrchestrator.freeze runtime1 (Local id)

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

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ app; lib; core ]
                (Map.ofList [ appFile, appId; libFile, libId; coreFile, coreId ])
                []
                [ ProjectEdge.create appId libId; ProjectEdge.create libId coreId ]

        let contents = Map.ofList [ appFile, "module App"; libFile, "module Lib"; coreFile, "module Core" ]

        let frozen = FrozenSnapshot.freezeTree 7L graph contents (ProjClosure appId)

        let expected = [ appId; libId; coreId ] |> List.sort
        Assert.Equal<ProjectId>(expected, frozen.Projects |> List.map (fun p -> p.ProjectId))
        Assert.Equal(7L, frozen.Revision)
        Assert.All(frozen.Projects, (fun leaf -> Assert.NotEmpty(leaf.Capabilities)))
        let contentsById =
            frozen.Projects |> List.map (fun p -> p.ProjectId, p.Contents) |> Map.ofList
        Assert.True(Map.containsKey appFile (Map.find appId contentsById))
        Assert.True(Map.containsKey libFile (Map.find libId contentsById))
        Assert.True(Map.containsKey coreFile (Map.find coreId contentsById))

    [<Fact>]
    member _.``Solution freeze is a disjoint file-exhaustive composite with no cross-project edges``() =
        let appPath = @"D:\repo\src\App\App.fsproj"
        let libPath = @"D:\repo\src\Lib\Lib.fsproj"
        let appFile = @"D:\repo\src\App\Program.fs"
        let libFile = @"D:\repo\src\Lib\Lib.fs"

        let app, appId = makeProject appPath
        let lib, libId = makeProject libPath

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ app; lib ]
                (Map.ofList [ appFile, appId; libFile, libId ])
                []
                [ ProjectEdge.create appId libId ]

        let contents = Map.ofList [ appFile, "module App"; libFile, "module Lib" ]

        let frozen = FrozenSnapshot.freezeTree 3L graph contents FreezeMode.Solution

        Assert.Equal(2, frozen.Projects.Length)

        let leafPaths = frozen.Projects |> List.collect (fun p -> p.Contents |> Map.toList |> List.map fst)
        Assert.Equal(2, leafPaths.Length)
        Assert.Equal(2, Set.count (Set.ofList leafPaths))
        Assert.Equal(1, graph.ProjectEdges.Length)