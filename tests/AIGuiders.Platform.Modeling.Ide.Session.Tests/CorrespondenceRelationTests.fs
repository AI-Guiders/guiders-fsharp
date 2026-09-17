namespace AIGuiders.Platform.Modeling.Ide.Session.Tests

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.Documentation.Correspondence
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths
open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type CorrespondenceRelationTests() =

    [<Fact>]
    member _.``Correspondence wire kinds map to RelationType``() =
        for kind in
            [ CorrespondenceRelationKind.Documents
              CorrespondenceRelationKind.ImplementsObligation
              CorrespondenceRelationKind.Related
              CorrespondenceRelationKind.Constrains
              CorrespondenceRelationKind.Normates
              CorrespondenceRelationKind.VerifiedBy ] do
            let relationType = CorrespondenceRelationGraph.kindToRelationType kind
            Assert.Equal(CorrespondenceRelationKind.toWire kind, CorrespondenceRelationGraph.relationTypeWire relationType)

    [<Fact>]
    member _.``ImplementsObligation validates semantic to adr obligation``() =
        let codeDoc = DocumentRef.File(LogicalPath.Create "src/Foo.cs")
        let symbol = { Container = []; Name = "Bar"; Arity = None }
        let obligation = AdrObligationId "GUIDERS-ADR-0063"

        let relation =
            { From = GraphNodeRef.Semantic(codeDoc, symbol)
              Type = RelationType.ImplementsObligation
              To = GraphNodeRef.AdrObligation obligation
              Scope = SessionG
              Attributes = RelationAttributes.empty }

        match RelationGraph.validateRelation relation with
        | Ok () -> ()
        | Error e -> Assert.Fail e.Message

    [<Fact>]
    member _.``Documents validates document fragment pair``() =
        let fromDoc = DocumentRef.File(LogicalPath.Create "docs/a.md")
        let toDoc = DocumentRef.File(LogicalPath.Create "docs/b.md")

        let relation =
            { From = GraphNodeRef.Document fromDoc
              Type = RelationType.Documents
              To = GraphNodeRef.Document toDoc
              Scope = SessionG
              Attributes = RelationAttributes.empty }

        match RelationGraph.validateRelation relation with
        | Ok () -> ()
        | Error e -> Assert.Fail e.Message

    [<Fact>]
    member _.``ReverseAnchor materializes validated doc to code relation``() =
        let anchor =
            { DocPath = "docs/adr/0063.md"
              DocTitle = "ADR-0063"
              Provenance = Provenance.Bracket
              Kind = Kind.Normates
              File = "src/Foo.cs"
              LineStart = None
              LineEnd = None
              MemberKey = Some "Bar"
              Wire = "[F:src/Foo.cs; M:Bar]"
              DocLineHint = None
              Excerpt = None }

        match CorrespondenceMaterialize.tryMaterializeReverseAnchor anchor with
        | Some relation ->
            Assert.Equal(RelationType.Normates, relation.Type)
            match relation.From, relation.To with
            | GraphNodeRef.Document _, GraphNodeRef.Semantic _ -> ()
            | _ -> Assert.Fail "expected document to semantic edge"
        | None -> Assert.Fail "expected materialized relation"

    [<Fact>]
    member _.``buildUses validates semantic pair``() =
        let doc = DocumentRef.File(LogicalPath.Create "src/App.cs")
        let consumer = { Container = []; Name = "Consumer"; Arity = None }
        let helper = { Container = []; Name = "Helper"; Arity = None }
        let project = ProjectId.create @"D:\repo\App.csproj"

        let relation = CorrespondenceMaterialize.buildUses doc consumer helper project

        match RelationGraph.validateRelation relation with
        | Ok () -> Assert.Equal(RelationType.Uses, relation.Type)
        | Error e -> Assert.Fail e.Message
