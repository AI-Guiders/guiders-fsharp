module AIGuiders.Platform.Modeling.LanguageIntelligence.Relations.Tests.TypeSystemRelationKindTests

open Xunit
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

[<Fact>]
let ``TypeSystemRelationKind disambiguates implements-interface from correspondence`` () =
    match TypeSystemRelationKind.tryParse "implements-interface" with
    | Some TypeSystemRelationKind.ImplementsInterface ->
        Assert.Equal("implements-interface", TypeSystemRelationKind.toWire TypeSystemRelationKind.ImplementsInterface)
    | _ -> failwith "expected implements-interface"

[<Fact>]
let ``TypeSystemRelationKind does not parse correspondence implements wire`` () =
    Assert.True(TypeSystemRelationKind.tryParse "implements" |> Option.isNone)
