namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type InvalidationScopeTests() =

    [<Fact>]
    member _.``Promotes from FileChange to ProjectFileCrud``() =
        Assert.True(InvalidationScope.promotes FileChange ProjectFileCrud)
        Assert.False(InvalidationScope.promotes ProjectFileCrud FileChange)

    [<Fact>]
    member _.``Max picks coarsest scope``() =
        Assert.Equal(ProjectCrud, InvalidationScope.max FileChange ProjectCrud)

type GraphValidationWfTests() =

    [<Fact>]
    member _.``WF7 rejects cross project capability edge``() =
        let fs =
            ProjectNode.create
                (ProjectId.create @"D:\repo\src\App\App.fsproj")
                (DotNet { Language = FSharp })
                @"D:\repo\src\App\App.fsproj"
                (CapabilityCatalog.defaultDotNet ())

        let cs =
            ProjectNode.create
                (ProjectId.create @"D:\repo\src\Lib\Lib.csproj")
                (DotNet { Language = CSharp })
                @"D:\repo\src\Lib\Lib.csproj"
                (CapabilityCatalog.defaultDotNet ())

        let fromCap = GraphNodeId.capability fs.Id CompilerServices
        let toCap = GraphNodeId.capability cs.Id Build

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ fs; cs ]
                Map.empty
                [ { From = fromCap; To = toCap; Kind = Requires; Attributes = Map.empty } ]
                []

        let result = GraphValidation.validate graph
        Assert.False(result.IsValid)
        Assert.Contains(result.Issues, fun i -> i.Message.Contains("WF7"))

    [<Fact>]
    member _.``WF8 rejects project edge cycle``() =
        let a = ProjectId.create @"D:\repo\A\A.fsproj"
        let b = ProjectId.create @"D:\repo\B\B.fsproj"

        let pa =
            ProjectNode.create a (DotNet { Language = FSharp }) (ProjectId.value a) (CapabilityCatalog.defaultDotNet ())

        let pb =
            ProjectNode.create b (DotNet { Language = FSharp }) (ProjectId.value b) (CapabilityCatalog.defaultDotNet ())

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ pa; pb ]
                Map.empty
                []
                [ { From = a; To = b }
                  { From = b; To = a } ]

        let result = GraphValidation.validate graph
        Assert.False(result.IsValid)
        Assert.Contains(result.Issues, fun i -> i.Message.Contains("WF8"))

module InvalidationTestFixtures =
    let twoProjectGraph () =
        let appPath = @"D:\repo\src\App\App.fsproj"
        let libPath = @"D:\repo\src\Lib\Lib.fsproj"
        let appId = ProjectId.create appPath
        let libId = ProjectId.create libPath
        let appSource = @"D:\repo\src\App\Module.fs"
        let libSource = @"D:\repo\src\Lib\Lib.fs"

        let appProject =
            ProjectNode.create appId (DotNet { Language = FSharp }) appPath (CapabilityCatalog.defaultDotNet ())

        let libProject =
            ProjectNode.create libId (DotNet { Language = FSharp }) libPath (CapabilityCatalog.defaultDotNet ())

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                [ appProject; libProject ]
                (Map.ofList [ appSource, appId; libSource, libId ])
                []
                []

        graph, appId, libId, appSource, libSource

type MaterializedInvalidationTests() =

    [<Fact>]
    member _.``ProjectFileCrud marks compiler stale without evicting other projects``() =
        let graph, appId, libId, appSource, _ = InvalidationTestFixtures.twoProjectGraph ()

        let state =
            MaterializedState.empty
            |> MaterializedState.mark (GraphNodeId.capability appId CompilerServices) 1L
            |> MaterializedState.mark (GraphNodeId.capability libId Build) 1L

        let patch =
            { SessionPatch.empty with
                FileSystem =
                    { FileSystemPatch.empty with
                        Writes = [ (appSource, "let x = 1") ] } }

        let result =
            MaterializedState.Invalidation.forScope (ProjectFileCrud) (graph) (patch) (state)

        Assert.Equal(2, result.Entries.Count)

        match Map.tryFind (GraphNodeId.capability appId CompilerServices) result.Entries with
        | None -> Assert.Fail("Expected compiler-services entry to remain")
        | Some entry -> Assert.True(entry.Stale)

        match Map.tryFind (GraphNodeId.capability libId Build) result.Entries with
        | None -> Assert.Fail("Expected lib build entry to remain")
        | Some entry -> Assert.False(entry.Stale)

    [<Fact>]
    member _.``ProjectCrud evicts only affected project subtree``() =
        let graph, appId, libId, _, _ = InvalidationTestFixtures.twoProjectGraph ()

        let state =
            MaterializedState.empty
            |> MaterializedState.mark (GraphNodeId.capability appId CompilerServices) 1L
            |> MaterializedState.mark (GraphNodeId.capability libId Build) 1L

        let updatedApp =
            graph.Projects
            |> List.find (fun p -> p.Id = appId)
            |> fun p -> { p with AbsolutePath = @"D:\repo\src\App\App.v2.fsproj" }

        let patch =
            { SessionPatch.empty with
                Graph = { GraphStructurePatch.empty with ProjectMetadataUpdates = [ updatedApp ] } }

        let result =
            MaterializedState.Invalidation.forScope (ProjectCrud) (graph) (patch) (state)

        Assert.False(Map.containsKey (GraphNodeId.capability appId CompilerServices) result.Entries)
        Assert.True(Map.containsKey (GraphNodeId.capability libId Build) result.Entries)

    [<Fact>]
    member _.``SolutionProjectCrud evicts removed project only``() =
        let graph, appId, libId, _, _ = InvalidationTestFixtures.twoProjectGraph ()

        let state =
            MaterializedState.empty
            |> MaterializedState.mark (GraphNodeId.capability appId CompilerServices) 1L
            |> MaterializedState.mark (GraphNodeId.capability libId Build) 1L

        let patch =
            { SessionPatch.empty with
                Graph = { GraphStructurePatch.empty with ProjectsRemoved = [ appId ] } }

        let result =
            MaterializedState.Invalidation.forScope (SolutionProjectCrud) (graph) (patch) (state)

        Assert.False(Map.containsKey (GraphNodeId.capability appId CompilerServices) result.Entries)
        Assert.True(Map.containsKey (GraphNodeId.capability libId Build) result.Entries)
