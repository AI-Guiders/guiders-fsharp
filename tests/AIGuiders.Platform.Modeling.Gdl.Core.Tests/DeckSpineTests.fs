namespace AIGuiders.Platform.Modeling.Gdl.Core.Tests

open Xunit
open AIGuiders.Platform.Modeling.Gdl.Core
open AIGuiders.Platform.Modeling.Gdl.Validation

module DeckSpineTests =
    [<Fact>]
    let ``dashspec studio deck projects to GdlFragment`` () =
        let project = GdlSpine.dashSpecStudioProject ()

        Assert.Single(project.Documents) |> ignore

        let entry = project.Documents.Head

        match entry.Fragment with
        | GdlFragment.Deck deck ->
            Assert.Equal("dashspec-studio", deck.Planet)
            Assert.Equal("(MFD)(F)", deck.Presets.Head.Topology.Value.SourceWire)
            Assert.Equal("report-preview", deck.Presets.Head.ForwardZoneId.Value)
        | other -> failwith $"Expected Deck fragment, got {other}"

    [<Fact>]
    let ``studio fixture validates without errors`` () =
        let project = GdlSpine.dashSpecStudioProject ()
        let issues = GdlProjectValidation.validate project
        let errors = issues |> List.filter (fun i -> i.Severity = ValidationSeverity.Error)
        Assert.Empty(errors)

    [<Fact>]
    let ``unknown zone yields warning`` () =
        let deck =
            { GdlSpine.dashSpecStudioDeck () with
                Presets =
                    [ { Name = "broken"
                        Topology = None
                        ForwardZoneId = Some "missing-zone"
                        MfdZoneIds = []
                        EicasPolicy = None } ] }

        let project =
            { GdlSpine.dashSpecStudioProject () with
                Documents =
                    [ { Ref = GdlSpine.documentRef "deck/broken.deck.gdl" "deck"
                        Fragment = GdlFragment.Deck deck } ] }

        let warnings =
            GdlProjectValidation.validate project
            |> List.filter (fun i -> i.Code = "GDL_DECK_UNKNOWN_ZONE")

        Assert.NotEmpty(warnings)
