namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Paths
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type DependencyRelationTests() =

    [<Fact>]
    member _.``Dependency wire round-trips through RelationType``() =
        for kind in
            [ DependencyRelationKind.Uses
              DependencyRelationKind.TypeUses
              DependencyRelationKind.Binds
              DependencyRelationKind.Imports
              DependencyRelationKind.SyntaxDepends ] do
            match DependencyRelationKind.tryParse(DependencyRelationKind.toWire kind) with
            | Some parsed ->
                Assert.Equal(kind, parsed)
                let mapped = RelationGraph.dependencyKindToRelationType kind
                Assert.NotEqual(RelationType.ProjectRef, mapped)
            | None -> Assert.Fail $"failed to parse {DependencyRelationKind.toWire kind}"

    [<Fact>]
    member _.``SyntaxDepends validates syntax node pair``() =
        let doc = DocumentRef.File(LogicalPath.Create "src/Foo.cs")
        let fromNode = NodeId.mint (NumericId.ofCounter 1L)
        let toNode = NodeId.mint (NumericId.ofCounter 2L)

        let relation =
            { From = GraphNodeRef.Syntax(doc, fromNode)
              Type = RelationType.SyntaxDepends
              To = GraphNodeRef.Syntax(doc, toNode)
              Scope = SemanticSubstrate(ProjectId.create @"D:\repo\App.csproj")
              Attributes = RelationAttributes.empty }

        match RelationGraph.validateRelation relation with
        | Ok () -> ()
        | Error e -> Assert.Fail e.Message

    [<Fact>]
    member _.``Imports allows syntax to semantic``() =
        let doc = DocumentRef.File(LogicalPath.Create "src/Foo.cs")
        let syn = NodeId.mint (NumericId.ofCounter 1L)
        let sym = { Container = []; Name = "Bar"; Arity = None }

        let relation =
            { From = GraphNodeRef.Syntax(doc, syn)
              Type = RelationType.Imports
              To = GraphNodeRef.Semantic(doc, sym)
              Scope = SemanticSubstrate(ProjectId.create @"D:\repo\App.csproj")
              Attributes = RelationAttributes.empty }

        match RelationGraph.validateRelation relation with
        | Ok () -> ()
        | Error e -> Assert.Fail e.Message

type TypeSystemProfileTests() =

    [<Fact>]
    member _.``CSharp Roslyn profile covers TypeSystem wires``() =
        for wire in [ "implements-interface"; "extends"; "instantiates" ] do
            match TypeSystemProfile.tryFind wire with
            | Some row ->
                Assert.Equal(NodeSort.SemanticSymbol, row.Dom)
                Assert.Equal(NodeSort.SemanticSymbol, row.Cod)
            | None -> Assert.Fail $"missing profile row for {wire}"
