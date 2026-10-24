# AIGuiders.Platform.Modeling.Navigation.Policy

Navigation policy — canonical related-kind tokens, named presets, preset/request merge, kind filter and profiles with caps.

## What you get

- `RelatedKinds` — six canonical related-kind tokens:
  - `partial_peer` — file-level counterpart (e.g. code-behind)
  - `project_peer` — sibling file in the same project
  - `xaml_codebehind_pair` — XAML paired with its code-behind
  - `test_counterpart` — code paired with its test
  - `same_namespace` — files sharing a namespace
  - `same_directory` — files sharing a directory
  - plus `tryCanonicalKind` (case-insensitive token → canonical name) and `all`
- `Presets` — five named filter presets:
  - `peers_only` — only partial/project peers
  - `no_namespace_noise` — everything except `same_namespace`/`same_directory`
  - `tests_and_peers` — peers plus test counterparts
  - `structure_only` — peers, XAML pairs, same-directory
  - `explore_default` — everything except `project_peer`
  - with `tryGet` (unknown preset → `None`), `allowsKind`, and `names`
- `PresetMerge.merge` — named preset + request-time include/exclude overrides: non-empty request include wins, excludes are unioned and deduped by canonical kind, unknown preset → error string
- `KindFilter` — whitelist/blacklist filter with `create`, `allows`, `effectiveInclude`, `effectiveExclude` (unknown tokens dropped, not fatal)
- `KindCaps.defaultRelated` — default per-kind caps: `same_directory` 4, `same_namespace` 4, `project_peer` 3
- `NavigationProfile` + `Profile` — preset + limits as one value: ready-made `exploreDefault` and `peersOnly`, request-time `fromExplore`, and `toCaps` (profile → `SceneCaps` with default kind caps merged in)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Navigation.Policy

// Does a kind survive a named preset?
Presets.allowsKind (Some "no_namespace_noise") RelatedKinds.SameNamespace   // false

// Preset + request overrides, merged (excludes union when both sides have them)
let includeKinds, excludeKinds, err =
    PresetMerge.merge (Some "tests_and_peers") None (Some [ "same_directory" ])

// Explore request -> profile -> scene caps
let caps =
    Profile.fromExplore (Some "peers_only") (Some 32) None None
    |> Profile.toCaps
```

## Problem

- "Show me what's related to this file" pulls neighbors of very different value: a test counterpart is gold, fifty same-namespace files are noise. Every navigation tool re-answers:
  - which neighbor kinds count, and which are noise
  - how many of each kind before the scene drowns
  - what happens when the caller narrows the filter at request time on top of the default
- Hand-rolled, these answers are scattered if-chains — invisible policy, drifting per host.

## Solution

- Policy as named, pure data: presets are an include/exclude catalog, merge rules are one function, caps are one list — reviewable in one place, deterministic everywhere.
- The F# SSOT of the navigation policy layer ([platform ADR-0033](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0033-navigation-family-semantic-scenes.md)), consumed by the C# navigation family (`AIGuiders.Platform.Navigation.Code`, `AIGuiders.Platform.Conformance.Navigation`); extraction mapped in [FSHARP ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md).

## When not to use

- Scene construction and wire parsing — this package decides *what passes*, never *how to walk a graph*; the scene IR is `AIGuiders.Platform.Modeling.Navigation`.
- Enforcement — caps and filters are data plus predicates; truncating an actual node/edge list is the scene builder's job (Execution).
- It is an F# mirror with C# parity (the C# `AIGuiders.Platform.Navigation.Policy` twin carries the same semantics) — it is not a new policy language, and it holds no per-project configuration of its own.

## Learn more

- [ADR-0033 — Navigation family: bounded semantic scenes](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0033-navigation-family-semantic-scenes.md)
- [FSHARP ADR-0003 — Model extraction matrix](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
