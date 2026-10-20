package: AIGuiders.Platform.Modeling.Combinations
tagline: Named merge semantics for layered data: collision policies and a pure ordered fold that combines layers deterministically.

## What you get

- `Semantics` — four documented collision policies:
  - `FieldOverlay` — non-null overlay fields win; baseline fills the gaps
  - `SectionReplace` — whole-section replace when the overlay section is present
  - `ShipFirst` — baseline wins on key collision
  - `OverlayWins` — overlay wins on key collision
- `Combinator<'T>` — the combine contract (`'T -> 'T -> 'T`); the policy lives at your call site
- `OrderedCombination.fold` / `foldLayers` — pure ordered fold over layers (baseline = head; at least one layer required)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Combinations

// OverlayWins: user settings override app defaults, by key
let combine : Combinator<Map<string, string>> =
    fun baseline overlay -> Map.fold (fun acc k v -> acc.Add(k, v)) baseline overlay

let effective = OrderedCombination.fold combine [ appDefaults; userSettings ]
```

## Problem

- Merging layered data — app defaults + user settings, template + overrides, environment layers — keeps answering the same questions:
  - which layer wins — per field or per section
  - who overrides whom (loading order)
- Hand-rolled, these live as if-chains at every call site.

## Solution

- Named policies instead of anonymous if-chains: the answers become reviewable in one place.
- A pure ordered fold: layer order is explicit, the result deterministic.
- Battle-tested as the merge core of the Guiders federation — [ADR-0030](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0030-combinations-family.md) · [pattern catalog](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0049-federation-pattern-library.md).

## When not to use

- Deep merge with per-field-path precedence — this is whole-value combination; nested behaviour must live in your combiner, the package has no field-path machinery.
- Collision reporting — the fold is silent; if you need a merge report (which key won, from which layer), roll your own.

## Learn more

- [ADR-0030 — Combinations family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0030-combinations-family.md)
- [Federation pattern catalog](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0049-federation-pattern-library.md)
