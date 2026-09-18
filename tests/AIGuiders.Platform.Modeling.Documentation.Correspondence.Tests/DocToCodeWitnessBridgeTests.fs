module AIGuiders.Platform.Modeling.Documentation.Correspondence.Tests.DocToCodeWitnessBridgeTests

open Xunit
open AIGuiders.Platform.Modeling.Documentation.Correspondence
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

[<Fact>]
let ``DocToCodeWitness with member maps to DocToCode relation spec`` () =
    let witness =
        { DocPath = "docs/adr/0063.md"
          DocTitle = "ADR-0063"
          Provenance = Provenance.Bracket
          Kind = CorrespondenceRelationKind.ImplementsObligation
          File = "src/Foo.cs"
          LineStart = Some 10
          LineEnd = None
          MemberKey = Some "Bar"
          Wire = "[F:src/Foo.cs; M:Bar]"
          DocLineHint = None
          Excerpt = None }

    match DocToCodeWitnessBridge.tryToRelationSpec witness with
    | Some(RelationSpec.DocToCode(source, CodeTarget.Symbol(_, symbol))) ->
        Assert.Equal("Bar", symbol.Name)
        match source with
        | DocumentPlace.Fragment(path, _) -> Assert.Equal("docs/adr/0063.md", path.Value)
        | _ -> failwith "expected fragment source"
    | _ -> failwith "expected DocToCode witness"

[<Fact>]
let ``DocToCodeWitness without member does not emit relation spec`` () =
    let witness =
        { DocPath = "docs/adr/0063.md"
          DocTitle = "ADR-0063"
          Provenance = Provenance.DocBody
          Kind = CorrespondenceRelationKind.Related
          File = "src/Foo.cs"
          LineStart = Some 10
          LineEnd = Some 20
          MemberKey = None
          Wire = "[F:src/Foo.cs; L:10; L2:20]"
          DocLineHint = None
          Excerpt = None }

    Assert.True(DocToCodeWitnessBridge.tryToRelationSpec witness |> Option.isNone)
