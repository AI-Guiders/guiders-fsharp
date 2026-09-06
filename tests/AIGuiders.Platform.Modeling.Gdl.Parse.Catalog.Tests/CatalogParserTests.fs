module AIGuiders.Platform.Modeling.Gdl.Parse.Catalog.Tests.CatalogParserTests

open System
open Xunit
open AIGuiders.Platform.Modeling.Gdl.Authoring
open AIGuiders.Platform.Modeling.Gdl.Command
open AIGuiders.Platform.Modeling.Gdl.Core
open AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

let sample =
    """# agent-edit command catalog (smoke)

catalog agent-edit

defaults
  command.scope = agent-edit
end defaults

commands table
  | command | args      | help        | domain | object | intent |
  | open    | <path>    | Open a doc  |        |        |        |
  | sec.add | <heading> | Add section | sec    | add    | set    |
end commands

phrases table
  | name     | phrase   |
  | open-doc | open {id}|
end phrases

mcp table
  | command | expose |
  | open    | yes    |
end mcp
"""

[<Fact>]
let ``Parses planet, tables and defaults`` () =
    let result = CatalogParser.parse sample

    Assert.Empty(result.Diagnostics)

    let doc = Option.get result.Document
    Assert.Equal("agent-edit", doc.Planet)
    Assert.Equal(2, doc.Commands.Length)
    Assert.Equal("open", doc.Commands.[0].Command)
    Assert.Equal("Add section", doc.Commands.[1].Columns |> Map.find "help")
    Assert.Equal(1, doc.Phrases.Length)
    Assert.Equal(1, doc.Mcp.Length)
    Assert.Equal("yes", doc.Mcp.[0].Expose)
    Assert.Equal(Some "agent-edit", doc.Defaults.CommandScope)

[<Fact>]
let ``Missing header produces MissingCatalogHeader and no document`` () =
    let result = CatalogParser.parse "commands table\nend commands\n"

    Assert.True(result.Document.IsNone)
    Assert.Contains(result.Diagnostics, fun d -> d.Code = MissingCatalogHeader)

[<Fact>]
let ``Duplicate command produces DuplicateRow`` () =
    let text =
        "catalog dup\n\ncommands table\n  | command |\n  | open |\n  | open |\nend commands\n"

    let result = CatalogParser.parse text

    Assert.True(result.Document.IsSome)
    Assert.Contains(result.Diagnostics, fun d -> d.Code = DuplicateRow)

[<Fact>]
let ``Maps document to spine payload with route rows`` () =
    let result = CatalogParser.parse sample
    let doc = Option.get result.Document
    let payload = CatalogMapping.toPayload doc

    Assert.Equal("agent-edit", payload.Planet)
    Assert.Equal(2, Seq.length payload.Routes)

    let routes = Seq.toList payload.Routes
    let first = routes.[0]
    Assert.Equal("open", first.Path)
    Assert.Equal("open", first.CommandId)
    Assert.Equal(CommandArgTailKind.Optional, first.ArgTailKind)
    Assert.Equal("Open a doc", first.Help)
    Assert.Equal("", first.Domain)
    Assert.Equal("", first.Object)
    Assert.Equal("", first.Intent)

    let second = routes.[1]
    Assert.Equal("sec.add", second.CommandId)
    Assert.Equal("sec", second.Domain)
    Assert.Equal("add", second.Object)
    Assert.Equal("set", second.Intent)
    Assert.Equal(CatalogPathRole.Canonical, second.PathRole)

[<Fact>]
let ``Bindings section without keyboard binding grammar is flagged`` () =
    let text =
        "catalog b\n\ncommands table\n  | command |\n  | open |\nend commands\n\nbindings table\n  | gesture | command |\n  | Ctrl+K  | open    |\nend bindings\n"

    let result = CatalogParser.parse text

    Assert.Contains(
        result.Diagnostics,
        fun d -> d.Code = MissingGrammarDeclaration && d.Message.Contains("grammar.keyboard.binding"))

[<Fact>]
let ``Channel without grammar block is flagged by validateChannels`` () =
    let text =
        "catalog ch\n\nchannels\n  slash\n    bar = toolbar-slash\nend channels\n"

    let result = CatalogParser.parse text

    Assert.Contains(
        result.Diagnostics,
        fun d -> d.Code = MissingGrammarDeclaration && d.Message.Contains("slash.bar"))

[<Fact>]
let ``Wire checks use injected parser and slug shortcut passes for key-gesture`` () =
    let text =
        "catalog w\n\ndefaults\n  grammar.keyboard.binding = keyboard-key-gesture\nend defaults\n\nbindings table\n  | gesture  | command |\n  | Ctrl+K   | open    |\nend bindings\n"

    let parseStub (grammarId: string) (wire: string) : bool * string option =
        // Stub "parser": only accepts plain `Ctrl+X` chords; anything else suggests Vim.
        if wire.StartsWith "Ctrl+" then true, None
        else false, Some "Vim"

    let doc = Option.get (CatalogParser.parse text).Document
    let diags = CatalogGrammarValidator.validate (Some parseStub) doc

    Assert.Empty(diags)

    // The same document under a strict stub that rejects everything — wire mismatches surface.
    let strict (_: string) (_: string) : bool * string option = false, Some "Vim"
    let diagsStrict = CatalogGrammarValidator.validate (Some strict) doc

    Assert.Equal(1, diagsStrict.Length) // no chord-root → only the binding row mismatch
    Assert.All(diagsStrict, fun (d: AuthoringDiagnostic) -> Assert.Equal(GrammarWireMismatch, d.Code))

[<Fact>]
let ``Melody slug passes for key-gesture grammar and mismatch surfaces otherwise`` () =
    let text =
        "catalog m\n\ndefaults\n  grammar.keyboard.melody = keyboard-key-gesture\nend defaults\n\nmelodies table\n  | slug      | command |\n  | open-doc  | open    |\nend melodies\n"

    let doc = Option.get (CatalogParser.parse text).Document
    let slugOnly (grammarId: string) (_: string) : bool * string option =
        grammarId.Equals("keyboard-key-gesture", StringComparison.OrdinalIgnoreCase), None

    let diags = CatalogGrammarValidator.validate (Some slugOnly) doc

    Assert.Empty(diags)

    // Slug shortcut bypasses the parser for key-gesture — even a rejecting parser stays silent.
    let reject (_: string) (_: string) : bool * string option = false, None
    let diagsReject = CatalogGrammarValidator.validate (Some reject) doc

    Assert.Empty(diagsReject)

    // A non-key-gesture grammar has no slug shortcut — strict reject surfaces the mismatch.
    let vimText =
        text.Replace("keyboard-key-gesture", "keyboard-vim")

    let docVim = Option.get (CatalogParser.parse vimText).Document
    let diagsVim = CatalogGrammarValidator.validate (Some reject) docVim

    Assert.Equal(1, diagsVim.Length)
    Assert.Equal(GrammarWireMismatch, diagsVim.[0].Code)
