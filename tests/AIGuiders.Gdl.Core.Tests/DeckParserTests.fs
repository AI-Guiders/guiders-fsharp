namespace AIGuiders.Gdl.Core.Tests

open System
open System.IO
open Xunit
open AIGuiders.Gdl.Core
open AIGuiders.Gdl.Parse.Deck
open AIGuiders.Gdl.Presentation

module DeckParserTests =
    let private loadFixture name =
        let path =
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "Authoring", name)

        File.ReadAllText(path)

    [<Fact>]
    let ``parse dashspec studio fixture has typed topology`` () =
        let text = loadFixture "dashspec-studio.deck.gdl"
        let result = DeckParser.parse text (Some "dashspec-studio.deck.gdl")

        Assert.Empty(result.Diagnostics)
        Assert.True(result.Document.IsSome)

        let document = result.Document.Value
        Assert.Equal("dashspec-studio", document.Planet)

        let preset = Assert.Single(document.Presets)
        Assert.Equal("report-author", preset.Name)
        Assert.True(preset.Topology.IsSome)

        let topology = preset.Topology.Value
        Assert.Equal("(MFD)(F)", topology.SourceWire)
        Assert.Equal(TopologyArrangement.MultiHost, topology.Arrangement)
        Assert.Equal(2, topology.HostCount)
        Assert.Equal(AttentionDisplayRole.Mfd, topology.Hosts.[0].Role)
        Assert.Equal(AttentionDisplayRole.Forward, topology.Hosts.[1].Role)
        Assert.Equal(Some "report-preview", preset.ForwardZoneId)
        Assert.Equal<string list>([ "spec-tree"; "resolve" ], preset.MfdZoneIds)
        Assert.Equal(Some "when alerts", preset.EicasPolicy)

        Assert.Equal("forward", document.ZoneBindings.["report-preview"])
        Assert.Equal("forward", document.ZoneBindings.["repl"])
        Assert.Equal("mfd", document.ZoneBindings.["spec-tree"])

    [<Fact>]
    let ``parsed deck maps to GdlFragment spine`` () =
        let text = loadFixture "dashspec-studio.deck.gdl"
        let result = DeckParser.parse text None
        let deck = DeckMapping.toDeckPayload result.Document.Value

        match GdlFragment.Deck deck with
        | GdlFragment.Deck payload ->
            Assert.Equal("dashspec-studio", payload.Planet)
            Assert.Equal("(MFD)(F)", payload.Presets.Head.Topology.Value.SourceWire)
        | other -> failwith $"Expected Deck fragment, got {other}"
