module AIGuiders.Platform.Modeling.Gdl.Parse.Catalog.Tests.CatalogParserTests

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
  | command | args     | help        |
  | open    | <path>   | Open a doc  |
  | sec.add | <heading>| Add section |
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

    let second = routes.[1]
    Assert.Equal("sec.add", second.CommandId)
    Assert.Equal("sec", second.Domain)
    Assert.Equal("add", second.Object)
    Assert.Equal("", second.Intent)
    Assert.Equal(CatalogPathRole.Canonical, second.PathRole)
