namespace AIGuiders.Gdl.Core

open AIGuiders.Gdl.Presentation

/// Stable reference to one GDL document in a project directory.
/// <c>Quarry</c> is the token before <c>.gdl</c> (e.g. <c>deck</c>, <c>catalog</c>).
type GdlDocumentRef =
    { LogicalPath: string
      Quarry: string }

/// Command catalog quarry payload (v0 stub — grows with Authoring.Command.Catalog IR).
[<NoComparison>]
type CatalogPayload = { Planet: string }

/// One attention preset from <c>*.deck.gdl</c>.
type DeckPreset =
    { Name: string
      Topology: PresentationTopology option
      ForwardZoneId: string option
      MfdZoneIds: string list
      EicasPolicy: string option }

/// Deck quarry payload — zones + presets from <c>*.deck.gdl</c>.
type DeckPayload =
    { Planet: string
      Presets: DeckPreset list
      ZoneBindings: Map<string, string> }

/// Physical screen binding quarry (proposed <c>*.display.gdl</c>).
type DisplayBindingPayload =
    { ProfileName: string
      Bindings: (int * string) list }

/// Cockpit annunciation quarry (proposed <c>*.cockpit.logic.gdl</c>).
type CockpitLogicPayload = { Planet: string }

/// Closed set of GDL quarry payloads — federation spine discriminated union.
type GdlFragment =
    | Catalog of CatalogPayload
    | Deck of DeckPayload
    | Display of DisplayBindingPayload
    | CockpitLogic of CockpitLogicPayload

type GdlProjectEntry =
    { Ref: GdlDocumentRef
      Fragment: GdlFragment }

type GdlProjectManifest =
    { Name: string
      WorkspaceRoot: string }

/// Composed declare-time project: manifest + typed document fragments.
type GdlProject =
    { Manifest: GdlProjectManifest
      Documents: GdlProjectEntry list }

/// Helpers for tests and future C# Authoring mappers.
[<RequireQualifiedAccess>]
module GdlSpine =
    let documentRef logicalPath quarry =
        { LogicalPath = logicalPath
          Quarry = quarry }

    let dashSpecStudioDeck () : DeckPayload =
        match TopologyNotation.parse "(MFD)(F)" with
        | { Topology = Some topology } ->
            { Planet = "dashspec-studio"
              Presets =
                [ { Name = "report-author"
                    Topology = Some topology
                    ForwardZoneId = Some "report-preview"
                    MfdZoneIds = [ "spec-tree"; "resolve" ]
                    EicasPolicy = Some "when alerts" } ]
              ZoneBindings =
                Map.ofList
                    [ "report-preview", "forward"
                      "repl", "forward"
                      "spec-tree", "mfd" ] }
        | _ -> failwith "dashspec-studio topology wire must parse"

    let dashSpecStudioProject () : GdlProject =
        let deck = dashSpecStudioDeck ()
        { Manifest =
            { Name = "dashspec-studio"
              WorkspaceRoot = "." }
          Documents =
            [ { Ref = documentRef "deck/dashspec-studio.deck.gdl" "deck"
                Fragment = GdlFragment.Deck deck } ] }
