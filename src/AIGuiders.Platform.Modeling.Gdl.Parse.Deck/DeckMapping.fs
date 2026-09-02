namespace AIGuiders.Platform.Modeling.Gdl.Parse.Deck

open AIGuiders.Platform.Modeling.Gdl.Core

[<RequireQualifiedAccess>]
module DeckMapping =
    let toDeckPayload (document: DeckDocument) : DeckPayload =
        { Planet = document.Planet
          Presets =
            document.Presets
            |> List.map (fun preset ->
                { Name = preset.Name
                  Topology = preset.Topology
                  ForwardZoneId = preset.ForwardZoneId
                  MfdZoneIds = preset.MfdZoneIds
                  EicasPolicy = preset.EicasPolicy })
          ZoneBindings = document.ZoneBindings }
