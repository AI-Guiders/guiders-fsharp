module AIGuiders.Platform.Modeling.Combinations.Tests.OrderedCombinationTests

open System
open Xunit
open AIGuiders.Platform.Modeling.Combinations

[<Fact>]
let ``fold: single layer is identity`` () =
    Assert.Equal(42, OrderedCombination.fold (+) [ 42 ])

[<Fact>]
let ``fold: combines in order`` () =
    // baseline first, overlays on top
    Assert.Equal<int list>([ 1; 2; 3 ], OrderedCombination.fold (fun acc layer -> acc @ layer) [ [ 1 ]; [ 2 ]; [ 3 ] ])

[<Fact>]
let ``fold: empty layers is an error (C# parity)`` () =
    Assert.Throws<ArgumentException>(fun () -> OrderedCombination.fold (+) [] |> ignore)

[<Fact>]
let ``foldLayers: project then fold`` () =
    let total = OrderedCombination.foldLayers (fun (n: int) -> n * 2) (+) 0 [ 1; 2; 3 ]
    Assert.Equal(12, total)

[<Fact>]
let ``semantics: DU covers documented vocabulary`` () =
    let all = [ FieldOverlay; SectionReplace; ShipFirst; OverlayWins ]
    Assert.Equal(4, all.Length)
    Assert.Equal(ShipFirst, List.item 2 all)