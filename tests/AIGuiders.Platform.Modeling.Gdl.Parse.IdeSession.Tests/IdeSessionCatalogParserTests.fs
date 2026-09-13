module AIGuiders.Platform.Modeling.Gdl.Parse.IdeSession.Tests.IdeSessionCatalogParserTests

open Xunit
open AIGuiders.Platform.Modeling.Gdl.Parse.IdeSession

let private gatesSample =
    """
catalog ide-session

gates table
  | gate                  | reject-when                              | code                         |
  | graph-valid           | GraphValidation reports issue            | GraphValidation.validate     |
  | types-check           | typecheck failed or not run when required | HoareChecker.checkTypes      |
  | scope-file-change     | SessionPatch.scope wider than FileChange | GraphPatch.scope             |
end gates
"""

[<Fact>]
let ``parse gates table from ide-session catalog`` () =
    let result = IdeSessionCatalogParser.parseText gatesSample "<sample>"

    Assert.Empty(result.Diagnostics)
    Assert.True(result.Catalog.IsSome)

    let catalog = result.Catalog.Value
    Assert.Equal("<sample>", catalog.SourcePath)
    Assert.Equal(3, catalog.Gates.Length)

    let gate = catalog.Gates.[0]
    Assert.Equal("graph-valid", gate.GateId)
    Assert.Equal("GraphValidation reports issue", gate.RejectWhen)
    Assert.Equal("GraphValidation.validate", gate.Code)

[<Fact>]
let ``parse reports missing gates table`` () =
    let result = IdeSessionCatalogParser.parseText "catalog ide-session\n" "<empty>"
    Assert.True(result.Catalog.IsNone)
    Assert.Contains(result.Diagnostics, fun d -> d.Code = "ide-session.gates.missing")

[<Fact>]
let ``parse reports unclosed gates table`` () =
    let result =
        IdeSessionCatalogParser.parseText
            """
gates table
  | gate | reject-when | code |
  | g1   | when        | Code |
"""
            "<unclosed>"

    Assert.True(result.Catalog.IsNone)
    Assert.Contains(result.Diagnostics, fun d -> d.Code = "ide-session.gates.unclosed")

[<Fact>]
let ``parse reports row missing gate column`` () =
    let result =
        IdeSessionCatalogParser.parseText
            """
gates table
  | gate | reject-when | code |
  |      | when        | Code |
  | ok   | cond        | Impl |
end gates
"""
            "<rows>"

    Assert.True(result.Catalog.IsSome)
    Assert.Equal(1, result.Catalog.Value.Gates.Length)
    Assert.Contains(result.Diagnostics, fun d -> d.Code = "ide-session.gates.row")
