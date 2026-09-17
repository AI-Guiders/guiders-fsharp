namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.Paths
open Xunit

type DependencyRelationOpsTests() =
    [<Fact>]
    member _.``mergeRelations appends validated Uses without duplicates`` () =
        let project = ProjectId.create @"D:\repo\App.csproj"
        let uses =
            CorrespondenceMaterialize.buildUsesFromTypeNames @"D:\repo\src\App.cs" "Consumer" "Helper" project

        let anchor = LogicalPath.Create @"D:\repo\App.slnx"
        let graph = SolutionGraph.create anchor [] []
        let merged = SolutionGraph.mergeRelations [ uses ] graph

        Assert.Equal(1, merged.Relations.Length)

        let again = SolutionGraph.mergeRelations [ uses ] merged
        Assert.Equal(1, again.Relations.Length)

    [<Fact>]
    member _.``ingest updates runtime session graph`` () =
        let project = ProjectId.create @"D:\repo\App.csproj"
        let uses =
            CorrespondenceMaterialize.buildUsesFromTypeNames @"D:\repo\src\App.cs" "Consumer" "Helper" project

        let anchor = LogicalPath.Create @"D:\repo\App.slnx"
        let graph = SolutionGraph.create anchor [] []
        let session = SolutionSession.create anchor graph

        let runtime =
            SessionBootstrap.createFromRegistry session Map.empty Map.empty 0L

        let updated = DependencyRelationOps.ingest [ uses ] runtime

        Assert.Equal(1, updated.Session.Graph.Relations.Length)
