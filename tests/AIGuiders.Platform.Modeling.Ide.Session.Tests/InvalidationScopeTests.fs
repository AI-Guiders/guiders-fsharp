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
