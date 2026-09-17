module AIGuiders.Platform.Modeling.Notations.Bracket.Tests.LegacyBracketSpanTests

open Xunit
open AIGuiders.Platform.Modeling.Notations.Bracket

[<Fact>]
let ``BracketAxisFamily includes Json = 5`` () =
    Assert.Equal(0, int BracketAxisFamily.None)
    Assert.Equal(5, int BracketAxisFamily.Json)

[<Fact>]
let ``BracketAnchorSpan supports nested anchors`` () =
    let inner = { BracketAnchorSpan.empty with MemberKey = Some "inner" }
    let outer = { BracketAnchorSpan.empty with File = Some "a.fs"; NestedAnchor = Some inner }
    Assert.Equal(Some inner, outer.NestedAnchor)
