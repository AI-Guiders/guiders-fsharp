namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open System
open System.Reflection
open Microsoft.FSharp.Core
open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Documentation.Correspondence
open AIGuiders.Platform.Modeling.Ide.Session
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Navigation
open AIGuiders.Platform.Modeling.Paths
open Xunit

/// Plan §10 Phase 1 closure gate — runtime evidence for shipped checklist rows.
type FederationPhase1ChecklistTests() =
    [<Fact>]
    member _.``Identity kernel exposes carrier wire and GitPin``() =
        Assert.True(typeof<CarrierWire>.IsPublic)
        Assert.True(typeof<GitPin>.IsPublic)
        Assert.True(typeof<ProjectId>.IsPublic)

        let hex = String('a', 64)

        match ParseCommit.parse hex with
        | Ok _ -> ()
        | Error e -> Assert.Fail e

    [<Fact>]
    member _.``ResolveTier has Syntax and Semantic only``() =
        let names = typeof<ResolveTier>.GetEnumNames()
        Assert.Equal(2, names.Length)
        Assert.DoesNotContain("Text", names)

    [<Fact>]
    member _.``RevisionLedger uses TransformClass DU not string theta``() =
        let entryType = typeof<LedgerEntry>
        let theta = entryType.GetProperty("ThetaClass")

        match theta with
        | null -> Assert.Fail "missing ThetaClass"
        | prop -> Assert.Equal(typeof<TransformClass>, prop.PropertyType)

    [<Fact>]
    member _.``Relation homonyms ImplementsInterface and ImplementsObligation are distinct``() =
        Assert.NotEqual(RelationType.ImplementsInterface, RelationType.ImplementsObligation)
        Assert.NotEqual(CorrespondenceRelationKind.ImplementsObligation, CorrespondenceRelationKind.Normates)

        let app = ProjectId.create @"D:\repo\App.csproj"
        let doc = DocumentRef.File(LogicalPath.Create "src/Foo.cs")
        let sym = { Container = []; Name = "Foo"; Arity = None }

        let iface =
            { From = GraphNodeRef.Semantic(doc, sym)
              Type = RelationType.ImplementsInterface
              To = GraphNodeRef.Semantic(doc, { sym with Name = "IBar" })
              Scope = SemanticSubstrate app
              Attributes = RelationAttributes.empty }

        let obligation =
            { From = GraphNodeRef.Semantic(doc, sym)
              Type = RelationType.ImplementsObligation
              To = GraphNodeRef.AdrObligation(AdrObligationId "ADR-0001")
              Scope = SessionG
              Attributes = RelationAttributes.empty }

        match RelationGraph.validateRelation iface, RelationGraph.validateRelation obligation with
        | Ok (), Ok () -> ()
        | Error e, _ | _, Error e -> Assert.Fail e.Message

    [<Fact>]
    member _.``SolutionGraph uses DocumentRegistry not FileOwnership map``() =
        let graphFields =
            typeof<SolutionGraph>.GetProperties(BindingFlags.Public ||| BindingFlags.Instance)
            |> Array.map (fun p -> p.Name)

        Assert.Contains("Relations", graphFields)
        Assert.DoesNotContain("FileOwnership", graphFields)
        Assert.True(typeof<DocumentRegistry>.IsPublic)

    [<Fact>]
    member _.``Navigation scene records are not CLIMutable seam views``() =
        for t in [ typeof<Node>; typeof<Edge>; typeof<SceneCaps>; typeof<Scene> ] do
            let attr = t.GetCustomAttribute(typeof<CLIMutableAttribute>)
            Assert.Null attr

    [<Fact>]
    member _.``Navigation scene uses Relations NavSeed not parallel Anchor``() =
        let seedField = typeof<Scene>.GetProperty("Seed")
        Assert.NotNull seedField
        Assert.Equal(typeof<NavSeed>, seedField.PropertyType)
        Assert.Null(typeof<Scene>.Assembly.GetType("AIGuiders.Platform.Modeling.Navigation.Anchor"))

    [<Fact>]
    member _.``Correspondence wire records are not CLIMutable seam views``() =
        for t in
            [ typeof<ReverseAnchor>
              typeof<ForwardDoc>
              typeof<CorrespondenceResult> ] do
            let attr = t.GetCustomAttribute(typeof<CLIMutableAttribute>)
            Assert.Null attr

    [<Fact>]
    member _.``Dependency and TypeSystem profiles ship Phase 1 kernels``() =
        Assert.NotEmpty TypeSystemProfile.csharpRoslynProfile
        Assert.True(typeof<DependencyRelationKind>.IsPublic)
        Assert.True(typeof<DiagnosticIndex>.IsPublic)

    [<Fact>]
    member _.``SceneProjection remains projection-only over Relation``() =
        let app = ProjectId.create @"D:\repo\App.fsproj"
        let lib = ProjectId.create @"D:\repo\Lib.fsproj"
        let relation = RelationGraph.projectRef app lib
        let edge = SceneProjection.toEdge "n0" "n1" relation
        Assert.Equal("project_ref", edge.Kind)
