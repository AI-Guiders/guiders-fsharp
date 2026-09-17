module AIGuiders.Platform.Modeling.Documentation.Correspondence.Tests.ReverseAnchorBridgeTests

open Xunit
open AIGuiders.Platform.Modeling.Documentation.Correspondence
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

[<Fact>]
let ``ReverseAnchor with member maps to DocToCode witness`` () =
    let anchor =
        { DocPath = "docs/adr/0063.md"
          DocTitle = "ADR-0063"
          Provenance = Provenance.Bracket
          Kind = Kind.ImplementsObligation
          File = "src/Foo.cs"
          LineStart = Some 10
          LineEnd = None
          MemberKey = Some "Bar"
          Wire = "[F:src/Foo.cs; M:Bar]"
          DocLineHint = None
          Excerpt = None }

    match ReverseAnchorBridge.tryToDocToCodeWitness anchor with
    | Some(RelationSpec.DocToCode(source, CodeTarget.Symbol(_, symbol))) ->
        Assert.Equal("Bar", symbol.Name)
        match source with
        | DocumentPlace.Fragment(path, _) -> Assert.Equal("docs/adr/0063.md", path.Value)
        | _ -> failwith "expected fragment source"
    | _ -> failwith "expected DocToCode witness"

[<Fact>]
let ``ReverseAnchor without member does not emit witness`` () =
    let anchor =
        { DocPath = "docs/adr/0063.md"
          DocTitle = "ADR-0063"
          Provenance = Provenance.DocBody
          Kind = Kind.Related
          File = "src/Foo.cs"
          LineStart = Some 10
          LineEnd = Some 20
          MemberKey = None
          Wire = "[F:src/Foo.cs; L:10; L2:20]"
          DocLineHint = None
          Excerpt = None }

    Assert.True(ReverseAnchorBridge.tryToDocToCodeWitness anchor |> Option.isNone)
