namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type SessionPatchTests() =

    [<Fact>]
    member _.``Rename patch scope is FileChange``() =
        let patch =
            RefactorPlan.planRename
                (Map.ofList [ "a.fs", "let foo = 1" ])
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
    member _.``Move path transfers omega and contents``() =
        let owner = ProjectId.create @"D:\repo\App.fsproj"
        let oldPath = @"D:\repo\Module.fs"
        let newPath = @"D:\repo\Renamed.fs"

        let graph =
            SolutionGraph.create
                @"D:\repo\App.slnx"
                []
                (Map.ofList [ oldPath, owner ])
                []
                []

        let contents = Map.ofList [ oldPath, "module App" ]
        let patch = RefactorPlan.planMovePath { From = oldPath; To = newPath }

        let graph', contents' = SessionPatch.apply graph contents patch

        Assert.False(Map.containsKey oldPath graph'.FileOwnership)
        Assert.Equal(Some owner, Map.tryFind newPath graph'.FileOwnership)
        Assert.Equal("module App", Map.find newPath contents')
