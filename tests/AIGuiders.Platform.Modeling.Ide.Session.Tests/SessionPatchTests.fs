namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Paths
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

type SessionPatchTests() =

    [<Fact>]
    member _.``Rename patch scope is FileChange``() =
        let owner = ProjectId.create @"D:\repo\App.fsproj"
        let boot = SessionTestFixtures.bootstrap (Map.ofList [ "a.fs", owner ]) [ "a.fs", "let foo = 1" ]

        let patch =
            RefactorPlan.planRename
                boot.Registry
                boot.Contents
                { OldName = "foo"; NewName = "bar"; Files = [ "a.fs" ] }

        Assert.Equal(FileChange, SessionPatch.scope patch)

    [<Fact>]
    member _.``Move type patch scope is ProjectFileCrud``() =
        let owner = ProjectId.create @"D:\repo\App.fsproj"

        let patch =
            RefactorPlan.planMoveTypeToFile
                { TypeName = "Foo"
                  SourcePath = @"D:\repo\Module.fs"
                  TargetPath = @"D:\repo\Foo.fs"
                  Owner = owner
                  UpdatedSourceContents = "let x = 1"
                  ExtractedContents = "type Foo = unit" }

        Assert.Equal(ProjectFileCrud, SessionPatch.scope patch)

    [<Fact>]
    member _.``Move path transfers registry path and contents``() =
        let owner = ProjectId.create @"D:\repo\App.fsproj"
        let oldPath = @"D:\repo\Module.fs"
        let newPath = @"D:\repo\Renamed.fs"

        let graph, ownership =
            SessionTestFixtures.createGraph @"D:\repo\App.slnx" [] (Map.ofList [ oldPath, owner ]) [] []

        let boot = SessionTestFixtures.bootstrap ownership [ oldPath, "module App" ]
        let patch = RefactorPlan.planMovePath { From = oldPath; To = newPath }

        let _, registry', contents', _ =
            SessionPatch.apply graph boot.Registry boot.Contents 0L patch

        Assert.True(
            DocumentRegistryOps.resolvePath (LogicalPath.Create oldPath) registry'
            |> Option.isNone
        )

        match DocumentRegistryOps.resolvePath (LogicalPath.Create newPath) registry' with
        | None -> Assert.Fail("Expected renamed document in registry")
        | Some docId ->
            match Map.tryFind docId contents' with
            | Some (DocumentText text) -> Assert.Equal("module App", text)
            | None -> Assert.Fail("Expected contents at renamed doc id")

    [<Fact>]
    member _.``Project metadata patch scope is ProjectCrud``() =
        let owner = ProjectId.create @"D:\repo\App.fsproj"

        let project =
            ProjectNode.create owner (DotNet { Language = FSharp }) (ProjectId.value owner) (CapabilityCatalog.defaultDotNet ())

        let updated = { project with AbsolutePath = @"D:\repo\App.v2.fsproj" }

        let patch =
            { SessionPatch.empty with
                Graph = { GraphStructurePatch.empty with ProjectMetadataUpdates = [ updated ] } }

        Assert.Equal(ProjectCrud, SessionPatch.scope patch)

    [<Fact>]
    member _.``Solution project removal scope is SolutionProjectCrud``() =
        let owner = ProjectId.create @"D:\repo\App.fsproj"

        let patch =
            { SessionPatch.empty with
                Graph = { GraphStructurePatch.empty with ProjectsRemoved = [ owner ] } }

        Assert.Equal(SolutionProjectCrud, SessionPatch.scope patch)
