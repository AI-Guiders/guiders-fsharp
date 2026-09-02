namespace AIGuiders.Gdl.Parse.Deck

open AIGuiders.Gdl.Authoring
open AIGuiders.Gdl.Presentation

type AttentionPreset =
    { Name: string
      Topology: PresentationTopology option
      ForwardZoneId: string option
      MfdZoneIds: string list
      EicasPolicy: string option }

type DeckDocument =
    { Planet: string
      Presets: AttentionPreset list
      ZoneBindings: Map<string, string> }

type DeckParseResult =
    { Document: DeckDocument option
      Diagnostics: AuthoringDiagnostic list }
