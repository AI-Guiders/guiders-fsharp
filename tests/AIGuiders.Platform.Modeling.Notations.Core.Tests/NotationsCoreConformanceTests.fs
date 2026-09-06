module AIGuiders.Platform.Modeling.Notations.Core.Tests.ConformanceTests

open Xunit
open AIGuiders.Platform.Modeling.Notations

[<Fact>]
let ``NotationKvPair: split on first sign only`` () =
    match NotationKvPair.trySplitFirst "a=b=c" '=' with
    | Ok kv ->
        Assert.Equal("a", kv.Key)
        Assert.Equal('=', kv.Sign)
        Assert.Equal("b=c", kv.Value)
    | Error e -> Assert.Fail e

[<Fact>]
let ``NotationKvPair: missing sign is error`` () =
    match NotationKvPair.trySplitFirst "abc" '=' with
    | Error e -> Assert.Contains("Missing KV sign", e)
    | Ok _ -> Assert.Fail "expected error"

[<Fact>]
let ``NotationKvPair: sign at position zero is error`` () =
    match NotationKvPair.trySplitFirst "=x" '=' with
    | Error _ -> Assert.True true
    | Ok _ -> Assert.Fail "expected error"

[<Fact>]
let ``NotationKvPair: empty segment is error`` () =
    match NotationKvPair.trySplitFirst "  " '=' with
    | Error e -> Assert.Equal("Empty segment.", e)
    | Ok _ -> Assert.Fail "expected error"

[<Fact>]
let ``NotationListSplit: top-level split keeps nested brackets`` () =
    let parts = NotationListSplit.splitTopLevel "a,[b,c],d" ',' '[' ']'
    Assert.Equal<string list>([ "a"; "[b,c]"; "d" ], parts)

[<Fact>]
let ``NotationListSplit: close never below zero, trailing part kept`` () =
    let parts = NotationListSplit.splitTopLevel "]a,[b[c,d]e]" ',' '[' ']'
    Assert.Equal<string list>([ "]a"; "[b[c,d]e]" ], parts)

[<Fact>]
let ``NotationListSplit: custom brackets`` () =
    let parts = NotationListSplit.splitTopLevel "(a,b),(c)" ',' '(' ')'
    Assert.Equal<string list>([ "(a,b)"; "(c)" ], parts)