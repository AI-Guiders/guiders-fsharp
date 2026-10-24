package: AIGuiders.Platform.Modeling.Catalog
tagline: Layered catalog index: project descriptors into a keyed index and merge layers with explicit collision policies (ship-first or overlay-wins).

## What you get

- `CatalogIndex<'Key, 'Entry>` — keyed, read-only-ish index: `Entries`, `Keys`, `TryGet` (byref), `Merge`/`MergeShipFirst`/`MergeOverlayWins`, `Empty`, `FromMap`, `FromDescriptors`
- `ICatalogProfile<'Descriptor, 'Key, 'Entry>` — how a descriptor family projects into the index:
  - `Project` — descriptor → (key, entry) pairs (one descriptor may land under several keys)
  - `NormalizeKey` — key canonicalization (case folding, trimming)
  - `LayerCollisionPolicy` / `MergeCollisionPolicy` — what wins inside a layer and between layers
- `CatalogIndexCollisionPolicy` — `ShipFirst` (first writer wins) / `OverlayWins` (last writer wins)
- `CatalogLayerComposer.compose` — fold a layer sequence into one index, applying the merge policy layer by layer

## Quick start

```fsharp
open System.Collections.Generic
open AIGuiders.Platform.Modeling.Catalog

// Ship layer and user overlay as plain maps
let ship    = CatalogIndex.FromMap(dict ["build.run", "ship:build"], EqualityComparer<string>.Default)
let overlay = CatalogIndex.FromMap(dict ["build.run", "user:build"], EqualityComparer<string>.Default)

// User settings override ship defaults:
let merged = ship.MergeOverlayWins overlay
let mutable entry = ""
merged.TryGet("build.run", &entry)   // true; entry = "user:build"

// Ship-first instead: first writer wins
let pinned = ship.MergeShipFirst overlay
pinned.TryGet("build.run", &entry)   // true; entry stays "ship:build"
```

## Problem

- Registries that ship defaults and accept user additions (commands, keybindings, snippets) all solve the same merge:
  - which layer wins on a key collision — the shipped one or the user's?
  - inside one layer, do duplicates collapse to the first or the last?
  - are keys compared raw or normalized?
- The answers are usually implicit in dictionary insertion order.

## Solution

- Collisions become a named policy on the index and the profile — `ShipFirst` vs `OverlayWins`, per layer and per merge, explicit at the call site.
- Key normalization is part of the profile, so `"Build.Run"` and `"build.run"` are decided once.
- Battle-tested as the index behind the Guiders command catalogs — [ADR-0039](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0039-command-catalog-family.md).

## When not to use

- Ordering or ranking (precedence by position, scores) — the index knows equality, not order; sort after projection.
- Deep entry merging — a collision replaces the whole entry; partial field overlaying is `Modeling.Combinations`' job.
- Persistence — in-memory only; serialize the descriptor layers, not the index.

## Learn more

- [ADR-0039 — command catalog family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0039-command-catalog-family.md)
