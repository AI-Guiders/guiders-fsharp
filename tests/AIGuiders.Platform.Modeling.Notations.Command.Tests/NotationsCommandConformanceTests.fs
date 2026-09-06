module AIGuiders.Platform.Modeling.Notations.Command.Tests.ConformanceTests

open System
open Xunit
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
