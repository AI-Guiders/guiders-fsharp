package: AIGuiders.Platform.Modeling.Conformance
tagline: Wire shapes for machine-readable conformance specs — navigation scene vectors and policy combination vectors — plus a pure expectation checker for observed scene shapes.

## What you get

- Navigation spec shapes:
  - `NavigationSpecDocument` — kind / surface / version + `Vectors`
  - `NavigationSpecVector` — id + `WireJson` + `ProfileJson` + `ExpectJson`
  - `NavigationProfileWire` — preset and result budgets (`MaxRelated`, `MaxNodes`, `MaxEdges`, `WithUsages`)
- Policy spec shapes:
  - `PolicySpecDocument` — policy id + `Semantics` + `Vectors`
  - `PolicySpecVector` — baseline JSON + overlay JSON + expected JSON
- `NavigationExpectation` + `NavigationExpectation.checkKinds` — pure checker: expected kinds present, excluded kinds absent, per-kind max counts respected
- Slash/keybinding wire rows: `SlashPathRow`, `BindingEntryRow`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Conformance

let expect : NavigationExpectation =
    { NodeCount = 3
      Kinds = [ "document"; "symbol" ]
      ExcludedKinds = [ "diagnostic" ]
      MaxKindCounts = [ "document", 2 ] }

NavigationExpectation.checkKinds [ "document"; "document"; "symbol" ] expect
// [] — no violations
NavigationExpectation.checkKinds [ "document"; "diagnostic" ] expect
// ["excluded kind \"diagnostic\" present."]
```

## Problem

- Behavior that must match across ports (a .NET reference harness and a JS port, say) is tested by prose and hope: each repo re-declares its own fixtures and drift goes unnoticed.
- Scene-shaped outputs (code maps, navigation graphs) need shape assertions — which kinds may appear, which must not, how many of each — not just golden-file diffs.

## Solution

- Spec documents become data: vectors pair inputs (wire + profile, or baseline + overlay) with expected outcomes, so every port consumes the same fixtures.
- The pure expectation algebra turns scene-shape checks into one call, reusable by any harness.
- Wire SSOT for the federation's navigation and policy conformance specs — extraction per [GUIDERS-FSHARP-ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md); the vectors pattern per [ADR-0033](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0033-navigation-family-semantic-scenes.md) and [ADR-0032](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0032-conformance-obligations-policy-specs.md).

## When not to use

- No runner here: schema validation, deserialization and executing vectors stay with the consuming harness (Execution side).
- `checkKinds` validates the kind set only; `NodeCount` is carried on the expectation but not checked by it.
- Two families only (navigation scenes, combination policies) — this is not a general-purpose spec framework.

## Learn more

- [GUIDERS-FSHARP-ADR-0003 — Model extraction matrix](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)
- [ADR-0033 — Navigation family: semantic scenes](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0033-navigation-family-semantic-scenes.md)
- [ADR-0032 — Conformance obligations: policy specs](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0032-conformance-obligations-policy-specs.md)
