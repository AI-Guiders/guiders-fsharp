module AIGuiders.Platform.Modeling.Documentation.Correspondence.Tests.HoareCatalogTests

open Xunit
open AIGuiders.Platform.Modeling.Documentation.Correspondence

[<Fact>]
let ``registeredIds includes OB-H1 OB-H2 OB-H3 from ADR-0007`` () =
    let ids = HoareObligationCatalog.registeredIds () |> Set.ofArray
    Assert.True(ids.Contains "OB-H1")
    Assert.True(ids.Contains "OB-H2")
    Assert.True(ids.Contains "OB-H3")
    Assert.Equal(3, ids.Count)

[<Fact>]
let ``registeredWfIds includes WF-OB1 WF-OB2 from ADR-0007`` () =
    let ids = HoareObligationCatalog.registeredWfIds () |> Set.ofArray
    Assert.True(ids.Contains "WF-OB1")
    Assert.True(ids.Contains "WF-OB2")
    Assert.Equal(2, ids.Count)

[<Fact>]
let ``isRegistered is case insensitive`` () =
    Assert.True(HoareObligationCatalog.isRegistered "ob-h1")
    Assert.True(HoareObligationCatalog.isRegisteredWf "wf-ob2")
    Assert.False(HoareObligationCatalog.isRegistered "OB-H99")
    Assert.False(HoareObligationCatalog.isRegisteredWf "WF-OB99")

[<Fact>]
let ``validateObligation accepts known catalog ids with obligation shape`` () =
    Assert.True(HoareObligationCatalog.validateObligation "OB-H1")
    Assert.True(HoareObligationCatalog.validateObligation "OB-H3")
    Assert.False(HoareObligationCatalog.validateObligation "OB-H99")
    Assert.False(HoareObligationCatalog.validateObligation "")

[<Fact>]
let ``validateWellFormedness accepts known catalog ids with wf shape`` () =
    Assert.True(HoareObligationCatalog.validateWellFormedness "WF-OB1")
    Assert.True(HoareObligationCatalog.validateWellFormedness "WF-OB2")
    Assert.False(HoareObligationCatalog.validateWellFormedness "WF-OB9")
    Assert.False(HoareObligationCatalog.validateWellFormedness "WF-OTHER")

[<Fact>]
let ``catalog entries cite GUIDERS-FSHARP-ADR-0007`` () =
    for entry in HoareObligationCatalog.entries () do
        Assert.Equal("GUIDERS-FSHARP-ADR-0007", entry.AdrSource)

    for entry in HoareObligationCatalog.wellFormednessEntries () do
        Assert.Equal("GUIDERS-FSHARP-ADR-0007", entry.AdrSource)
