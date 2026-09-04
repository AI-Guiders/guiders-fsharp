module AIGuiders.Platform.Modeling.Notations.Command.Tests.ConformanceTests

open System
open Xunit
open AIGuiders.Platform.Modeling.Notations
open AIGuiders.Platform.Modeling.Notations.Command
open AIGuiders.Platform.Modeling.Notations.Command.Slash

[<Fact>]
let ``SlashWireBody: parse body keeps trailing-space flag and token list`` () =
    let body = SlashCommandNotation.parseBody "cmd sub arg "
    Assert.Equal<string list>([ "cmd"; "sub"; "arg" ], List.ofSeq body.Tokens)
    Assert.True body.EndsWithSpaceAfterTokens
    Assert.Equal("cmd sub arg", body.JoinedTokens)

[<Fact>]
let ``SlashWireBody: no trailing space by default`` () =
    let body = SlashCommandNotation.parseBody "deploy --force"
    Assert.False body.EndsWithSpaceAfterTokens
    Assert.Equal(2, body.Tokens.Count)

[<Fact>]
let ``TryParseLine: slash line parses after gate`` () =
    match SlashCommandNotation.tryParseLine "/deploy --force " with
    | Some body -> Assert.Equal(2, body.Tokens.Count)
    | None -> Assert.Fail "expected Some"

[<Fact>]
let ``TryParseLine: rejects non-slash, empty and bare-slash lines`` () =
    Assert.True (SlashCommandNotation.tryParseLine "deploy").IsNone
    Assert.True (SlashCommandNotation.tryParseLine "/").IsNone
    Assert.True (SlashCommandNotation.tryParseLine "/   ").IsNone
    Assert.True (SlashCommandNotation.tryParseLine "").IsNone

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
