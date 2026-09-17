namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Paths
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type SceneProjectionTests() =

    [<Fact>]
    member _.``Relation edge kind projects from RelationType``() =
        let doc = DocumentRef.File(LogicalPath.Create "src/Foo.cs")
        let relation =
            { From = GraphNodeRef.Semantic(doc, { Container = []; Name = "Dog"; Arity = None })
              Type = RelationType.ImplementsInterface
              To = GraphNodeRef.Semantic(doc, { Container = []; Name = "Animal"; Arity = None })
              Scope = SemanticSubstrate(ProjectId.create @"D:\repo\App.fsproj")
              Attributes = RelationAttributes.empty }

        let edge = SceneProjection.toEdge "n0" "n1" relation
        Assert.Equal("implements_interface", edge.Kind)
        Assert.Null(edge.RelatedKind)

    [<Fact>]
    member _.``Related neighbor edge keeps RelatedKind projection``() =
        let edge = SceneProjection.relatedNeighborEdge "n0" "n1" "test_counterpart"
        Assert.Equal(SceneProjection.RelatedToWire, edge.Kind)
        Assert.Equal(Some "test_counterpart", edge.RelatedKind)
