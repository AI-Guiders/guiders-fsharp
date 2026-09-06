module AIGuiders.Platform.Modeling.Conformance.Tests.SpecTests

open Xunit
open AIGuiders.Platform.Modeling.Conformance

[<Fact>]
let ``Navigation expectation: kinds/excluded/max-count checks`` () =
    let expect =
        { NodeCount = 3
          Kinds = [ "type" ]
          ExcludedKinds = [ "todo" ]
          MaxKindCounts = [ "type", 2 ] }

    // ok case
    Assert.Equal<string list>([], NavigationExpectation.checkKinds [ "type"; "type"; "method" ] expect)

    // missing kind
    let missing = NavigationExpectation.checkKinds [ "method" ] expect
    Assert.Contains("expected kind \"type\" missing.", missing |> List.head)

    // excluded kind present
    let excluded = NavigationExpectation.checkKinds [ "type"; "todo" ] expect
    Assert.Contains("excluded kind \"todo\" present.", excluded |> List.head |> fun s -> s)

[<Fact>]
let ``Navigation expectation: max kind count exceeded`` () =
    let expect =
        { NodeCount = 0; Kinds = []; ExcludedKinds = []; MaxKindCounts = [ "type", 1 ] }
    let errors = NavigationExpectation.checkKinds [ "type"; "type"; "type" ] expect
    Assert.NotEmpty errors
    Assert.Contains("exceeds max 1", List.head errors)

[<Fact>]
let ``Policy shapes: slash row + binding row round-trip`` () =
    let row = { Path = "build"; CommandId = "cmd.build" }
    let binding = { Key = "ctrl+b"; Gesture = "Ctrl+B" }
    Assert.Equal("build", row.Path)
    Assert.Equal("Ctrl+B", binding.Gesture)