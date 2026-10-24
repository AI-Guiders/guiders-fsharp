# AIGuiders.Platform.Modeling.CommandPlane

Pure shapes for command surfaces: attach-wizard schemas, value constructors, prefix-arm completion matches, and locale date input algebra.

## What you get

- `AttachSchema.forVerb` — the attach wizard per verb (`Error`, `Issue`, `Document`, `Code`, `Nav`, `Manual`): target `RelationSpec` case + ordered pick steps; `AttachSchemaCatalog` exposes all verbs/steps with stable suggestion ids (`federation.attach.verb`, `federation.attach.step.<id>`)
- `AttachSchema.validateSpecVector` — conformance check: verb → expected target case + expected step ids
- Constructor shapes — `LeafConstructorDef` (segments + wire/display patterns), `CompositeConstructorDef` (slots + separator), `ConstructorRegistry` (pure `Map` registry), `ArgCompletionRow`/`ArgCompletionKind` for step-completion
- Prefix-arm (PAC) shapes — `PrefixArmDisposition` (`NoMatch` / `Ready` / `ArmConstructor`), `PrefixArmMatch` with constructors `noMatch`/`ready`/`armConstructor`, `PrefixArmSite`
- Locale date algebra — `LocaleInputProfile.fromPattern` (derive field order + separators from a culture short-date pattern), `LocaleDateAlgebra.completeness` (`Empty`/`Partial`/`MonthYear`/`CompleteDate`/`CompleteRange`)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.CommandPlane

// The attach wizard for the "code" verb: which steps, in which order
let schema = AttachSchema.forVerb AttachVerb.Code
schema.TargetCase                        // "CodeEdit"
schema.Steps |> List.map (fun s -> s.Id) // ["pick_file"; "pick_member"]

// Steps carry stable suggestion ids for completion menus
AttachSchema.stepSuggestionId "pick_file"
// "federation.attach.step.pick_file"

// Locale date input profile from a culture pattern (pure, no culture I/O)
let profile = LocaleInputProfile.fromPattern "dd.MM.yyyy"
profile.FieldOrder         // [Day; Month; Year]
profile.PrimarySeparator   // '.'
```

## Problem

- Command surfaces (palettes, chat, pickers) keep re-inventing the same scaffolding:
  - "attach this to an error/issue/doc/code" wizards: which steps, which order, what target kind
  - value constructors for structured arguments (segment patterns, composite slots)
  - prefix-armed completion: does this prefix produce a ready wire, or arm a constructor?
  - locale dates: which field order the user types, how complete the value is
- Each surface answers these with private enums and copy-paste steps.

## Solution

- The schemas live once as pure data with stable ids; surfaces render, conformance vectors pin the shape (`validateSpecVector`).
- Constructor and prefix-arm shapes mirror the C# execution plane one-to-one, so F# models and C# runtime agree.
- Battle-tested as the shape layer of the Guiders command plane — [ADR-0038](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0038-prefix-armed-completion.md) · [ADR-0035](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0035-slash-value-constructors.md) · [ADR-0047](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0047-command-for-doi.md).

## When not to use

- UI or completion rendering — these are shapes; menus are drawn by surfaces.
- Producing `RelationSpec` values — attach schemas map onto the Relations vocabulary but do not construct anchors.
- Locale date parsing — `fromPattern` derives the input profile and `completeness` grades parsed tokens; splitting typed text is not here.
- Adaptive topology rules — `AdaptiveRule`-style heuristics are placeholders elsewhere; this package carries no evaluator.

## Learn more

- [ADR-0038 — prefix-armed completion](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0038-prefix-armed-completion.md)
- [ADR-0035 — slash value constructors](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0035-slash-value-constructors.md)
- [ADR-0047 — command for DOI](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0047-command-for-doi.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
