module AIGuiders.Platform.Modeling.Gdl.Parse.Config.Tests.ConfigParserTests

open Xunit
open AIGuiders.Platform.Modeling.Gdl.Parse.Config

let private pilotSample =
    """
config cdp-newcomer

based on adr:GUIDERS-ADR-0064

import <federation/config/paths-grain>

defaults
  grammar.path.physical  = path-absolute-platform
  grammar.hot.section    = markdown-section-tags
end defaults

contracts table
  | id              | requires            | ensures                 |
  | hot.personal.l0 | primary_is_personal | hot_l0_sections_present |
"""

[<Fact>]
let ``parse pilot config header defaults and contracts`` () =
    let result = ConfigParser.parseText pilotSample

    Assert.Empty(result.Diagnostics)
    Assert.NotNull(result.Document)

    let doc = result.Document.Value
    Assert.Equal("cdp-newcomer", doc.Name)
    Assert.Equal(Some "GUIDERS-ADR-0064", doc.BasedOnAdr)
    Assert.Equal("path-absolute-platform", doc.Defaults.["grammar.path.physical"])
    Assert.Equal("hot.personal.l0", (Assert.Single doc.Contracts).Id)

[<Fact>]
let ``parse reports missing config header`` () =
    let result = ConfigParser.parseText "based on adr:X\n"
    Assert.Null(result.Document)
    Assert.Contains(result.Diagnostics, fun d -> d.Code = "config-missing-header")

[<Fact>]
let ``parse pilot sources and facts tables`` () =
    let sample =
        """
config cdp-newcomer

sources table
  | id           | kind              | path                                      | slice            |
  | personal-hot | markdown_sections | {personal}/agent-notes.md                 | above_public_cut |
  | l0-manifest  | json              | {personal}/knowledge/META/memory-architecture-v1.json | key:l0 |

facts table
  | contract        | verified_by                              |
  | hot.personal.l0 | install-cdp.personal-seed@e1417c4        |
"""

    let result = ConfigParser.parseText sample
    Assert.Empty(result.Diagnostics)

    let doc = result.Document.Value
    Assert.Equal(2, doc.Sources.Length)
    Assert.Equal("personal-hot", doc.Sources.[0].Id)
    Assert.Single(doc.Facts).VerifiedBy.StartsWith("install-cdp.personal-seed@")
    |> Assert.True
