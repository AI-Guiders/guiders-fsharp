package: AIGuiders.Platform.Modeling.Gdl.Parse.Deck
tagline: Parser for GDL deck documents: attention presets with typed topology wires, forward/MFD zone wiring and zone-role bindings, plus mapping onto the spine payload.

## What you get

- `DeckParser.parse` — one call from text to a `DeckParseResult` (`Document` option + `Diagnostics`); the source-path argument is currently unused, diagnostics carry line numbers from the text itself
- Document shape (`DeckDocument`): `Planet`, `Presets`, and a `ZoneBindings` map of `zone-id -> role`
- `AttentionPreset` — one named attention layout:
  - `topology (MFD)(F)` — parsed through `TopologyNotation` into a typed `PresentationTopology` (from `AIGuiders.Platform.Modeling.Gdl.Presentation`); invalid wires report `TopologyWireInvalid` and leave `Topology = None`
  - `forward <zone-id>` — which zone owns the forward attention field
  - `mfd <zone> | <zone> ...` — zones bound to the MFD hosts
  - `eicas <policy>` — when alerts may surface
- `zones` block — `zone-id = role` bindings with duplicate detection (`DuplicateRow`); malformed lines report `InvalidSyntax`
- `DeckMapping.toDeckPayload` — document to the spine `DeckPayload` (same field shape, ready for `GdlFragment.Deck` in `AIGuiders.Platform.Modeling.Gdl.Core`)
- Diagnostics are typed `AuthoringDiagnostic` from `AIGuiders.Platform.Modeling.Gdl.Authoring` — `MissingDeckHeader`, `InvalidSyntax`, `TopologyWireInvalid`, `DuplicateRow`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Parse.Deck

let result =
    DeckParser.parse
        """deck studio

preset report-author
  topology (MFD)(F)
  forward report-preview
  mfd spec-tree | resolve
  eicas when alerts
end preset

zones
  report-preview = forward
  spec-tree      = mfd
end zones
"""
        None

let doc = Option.get result.Document       // None + MissingDeckHeader when `deck <name>` is absent
let preset = doc.Presets.[0]               // { Name = "report-author"; Topology = Some (MFD)(F); ... }
let role = doc.ZoneBindings.["spec-tree"]  // "mfd"

let payload = DeckMapping.toDeckPayload doc   // spine DeckPayload
```

## Problem

- Attention layouts ("which zones exist, who is forward, what sits on the side displays") end up hardcoded per surface — the same layout decisions re-implemented in every renderer, impossible to review as a whole.
- Topology as a raw string travels through the pipeline until someone finally guesses what `(MFD)(F)` means — typos surface far from the file that made them.

## Solution

- Layout as a declarative document: presets and zone bindings in text, parsed once into typed records — reviewable in one place, renderable by any host.
- Topology wires parse into typed `PresentationTopology` at read time, so an invalid wire is a positioned diagnostic at the file, not a mystery string downstream.
- Battle-tested as the F# mirror of the Platform deck authoring parser — typed topology per [ADR-0058](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md) · deck authoring per [ADR-0055](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0055-surface-wpf-guild-deck-authoring.md) · GDL per [ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md).

## When not to use

- Rendering or layout math — this package produces the typed document and spine payload only; projecting presets onto grids/screens belongs to the presentation/execution layers.
- Cross-file consistency: preset `forward`/`mfd` zone ids are not checked against the `zones` block (or against any real surface) — dangling zone ids parse cleanly.
- Invalid topologies stay in the document: a bad topology wire adds a `TopologyWireInvalid` diagnostic but the preset remains (with `Topology = None`) rather than failing the parse.

## Learn more

- [ADR-0058 — presentation topology IR](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md)
- [ADR-0055 — deck authoring](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0055-surface-wpf-guild-deck-authoring.md)
- [ADR-0059 — GDL hyperlane](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)
