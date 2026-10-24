package: AIGuiders.Platform.Modeling.Gdl.Core
tagline: Typed spine for GDL document projects — a closed discriminated union over document payloads (catalog, deck, display, cockpit logic) plus the composed project model.

## What you get

- `GdlDocumentRef` — stable reference to one document: `LogicalPath` + `Quarry` (the token before `.gdl` in the file name, e.g. `deck`, `catalog`)
- `GdlFragment` — the closed discriminated union of document payloads:
  - `Catalog of CatalogPayload` — planet + resolved route rows (row IR from `Gdl.Command`)
  - `Deck of DeckPayload` — attention presets + zone bindings from `*.deck.gdl`
  - `Display of DisplayBindingPayload` — screen-binding profile (alias of `Gdl.Presentation.DisplayBindingProfile`)
  - `CockpitLogic of CockpitLogicPayload` — rule-graph IR (alias of `Gdl.Parse.CockpitLogic.CockpitRuleGraph`)
- `DeckPreset` — one attention preset: name, optional `Topology`, `ForwardZoneId`, `MfdZoneIds`, `EicasPolicy`
- `GdlProjectEntry` / `GdlProjectManifest` / `GdlProject` — composed declare-time project: manifest (name, workspace root) + typed document entries
- `GdlSpine` — helpers for tests and C# mappers: `documentRef`, plus sample `dashSpecStudioDeck` / `dashSpecStudioProject`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Core

let project : GdlProject =
    { Manifest = { Name = "studio"; WorkspaceRoot = "." }
      Documents =
        [ { Ref = GdlSpine.documentRef "deck/studio.deck.gdl" "deck"
            Fragment = GdlFragment.Deck(GdlSpine.dashSpecStudioDeck ()) } ] }

let deckCount =
    project.Documents
    |> List.filter (fun entry ->
        match entry.Fragment with
        | GdlFragment.Deck _ -> true
        | _ -> false)
    |> List.length
```

## Problem

- Declarative toolchains end up with several kinds of hand-edited files — command catalogs, screen-layout decks, rule files, binding profiles.
- Each parser naturally produces its own model, so anything that loads a whole workspace (an editor, a build check, a generator) glues those models together by hand — and adding a new file kind ripples through every consumer with no compiler help.

## Solution

- One closed union (`GdlFragment`) over the four typed payloads: consumers pattern-match, and when a document kind is added the compiler flags every match that needs updating.
- A project is just a manifest plus a list of `{ Ref; Fragment }` entries — trivially serializable, walkable, and diffable.
- This is the SSOT for the GDL project model, owned by the F# monorepo per [FSHARP-ADR-0001](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md); the language itself is named and specified in [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md), with the `config` branch extending the same spine ([GUIDERS-ADR-0064](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0064-config-gdl-quarry-family.md)). In this repo it already carries the deck graph consumed by `Modeling.Ide.Session` and the cross-document checks of `Modeling.Gdl.Validation`.

## When not to use

- Not a parser — text-to-payload lives in the sibling `Gdl.Parse.*` packages; here everything is already typed IR.
- The payload set is closed by design: a new document kind means a new version of this package, not a consumer-side extension point.
- Two payloads are type aliases of sibling packages, so referencing this package pulls in `Gdl.Presentation`, `Gdl.Command`, and `Gdl.Parse.CockpitLogic` — if you need only one payload, reference that package directly.

## Learn more

- [FSHARP-ADR-0001 — GDL spine ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md)
- [GUIDERS-ADR-0059 — GDL hyperlane](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)
- [GUIDERS-ADR-0064 — Config GDL quarry family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0064-config-gdl-quarry-family.md)
