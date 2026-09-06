namespace AIGuiders.Platform.Modeling.Gdl.Core

open System.Collections.Generic
open AIGuiders.Platform.Modeling.Gdl.Command
open AIGuiders.Platform.Modeling.Gdl.Parse.CockpitLogic
open AIGuiders.Platform.Modeling.Gdl.Presentation

/// Stable reference to one GDL document in a project directory.
/// <c>Quarry</c> is the token before <c>.gdl</c> (e.g. <c>deck</c>, <c>catalog</c>).
type GdlDocumentRef =
    { LogicalPath: string
      Quarry: string }

/// Command catalog quarry payload — planet + resolved route rows (row IR: Gdl.Command).
[<NoComparison>]
type CatalogPayload =
    { Planet: string
      Routes: IReadOnlyList<CatalogRouteEntry> }

/// One attention preset from <c>*.deck.gdl</c>.
type DeckPreset =
    { Name: string
      Topology: PresentationTopology option
      ForwardZoneId: string option
      MfdZoneIds: string list
      EicasPolicy: string option }

/// Deck quarry payload — zones + presets from <c>*.deck.gdl</c> (SSOT; Parse.Deck maps into it).
type DeckPayload =
    { Planet: string
      Presets: DeckPreset list
      ZoneBindings: Map<string, string> }

/// Display quarry payload — profile IR from Gdl.Presentation (HostIndex → screen).
type DisplayBindingPayload = DisplayBindingProfile

/// Cockpit logic quarry payload — rule graph IR from Gdl.Parse.CockpitLogic.
type CockpitLogicPayload = CockpitRuleGraph

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
