namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type RelationGraphTests() =

    [<Fact>]
    member _.``Requires edge validates as orchestration relation``() =
        let pid = ProjectId.create @"D:\repo\App.fsproj"
        let from = GraphNodeId.capability pid Build
        let to' = GraphNodeId.capability pid CompilerServices

        let edge =
            { From = from
              To = to'
              Kind = SessionEdgeKind.Requires
              Attributes = Map.empty }

        let relation = RelationGraph.fromSessionEdge edge

        match RelationGraph.validateRelation relation with
        | Ok () -> Assert.Equal(RelationType.Requires, relation.Type)
        | Error e -> Assert.Fail e.Message

    [<Fact>]
    member _.``Project edge maps to ProjectRef``() =
        let a = ProjectId.create @"D:\repo\A\A.fsproj"
        let b = ProjectId.create @"D:\repo\B\B.fsproj"
        let relation = RelationGraph.fromProjectEdge { From = a; To = b }

        match RelationGraph.validateRelation relation with
        | Ok () -> Assert.Equal(RelationType.ProjectRef, relation.Type)
        | Error e -> Assert.Fail e.Message

    [<Fact>]
    member _.``Cross-project capability edge fails validation``() =
        let a = ProjectId.create @"D:\repo\A\A.fsproj"
        let b = ProjectId.create @"D:\repo\B\B.fsproj"

        let relation =
            { From = GraphNodeRef.SessionCapability(a, Build)
              Type = RelationType.Requires
              To = GraphNodeRef.SessionCapability(b, CompilerServices)
              Scope = SessionG
              Attributes = RelationAttributes.empty }

        match RelationGraph.validateRelation relation with
        | Ok () -> Assert.Fail "expected sort validation failure"
        | Error e -> Assert.Contains("cross-project", e.Message)
