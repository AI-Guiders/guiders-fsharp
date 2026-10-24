package: AIGuiders.Platform.Modeling.Documentation.Correspondence
tagline: Machine-readable doc-to-code correspondence — typed relation kinds, bracket-wire anchors, doc-to-code witnesses, and the ADR facts-block parser (golden ids, Hoare obligations, verified_by evidence).

## What you get

- Relation vocabulary: `CorrespondenceRelationKind` — `Documents`, `ImplementsObligation`, `Related`, `Constrains`, `Normates`, `VerifiedBy` — with `toWire` / `tryParse`; wire literals in `Kind`, provenance in `Provenance`, ADR lifecycle tags
- Forward map shapes: `AdrReference`, `ForwardDoc`, `ForwardMapResult`, `ExplicitCodeAnchor`
- `BracketWire` — bracket anchors inside doc prose:
  - `build` / `tryParseBracket` — the `F:` / `M:` / `L:` profile (file, optional line span or member key), round-trippable
  - `buildCodeEdit` — canonical `[Kind:CodeEdit; File:…; Member:…; Line:…]` wire
- Doc-to-code witnesses: `DocToCodeWitness`, `CorrespondenceResult`, and `DocToCodeWitnessBridge.tryToRelationSpec` (member-keyed witnesses → relation spec)
- ADR facts (`AdrFacts`): `AdrFactsParser.tryParse` / `containsFactsBlock` over `facts: … end facts` blocks — `GoldenIds`, `HoareObligation`s (id, precondition, transform, postcondition), well-formedness ids, `VerifiedBy` rows
- `HoareObligationCatalog` — fixed pilot catalog (`OB-H1..H3`, `WF-OB1..OB2`) with `isRegistered` / `validateObligation` / `validateWellFormedness`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Documentation.Correspondence

// Typed relation kinds <-> wire tokens
CorrespondenceRelationKind.toWire Normates           // "normates"
CorrespondenceRelationKind.tryParse "verified_by"    // Some VerifiedBy

// Bracket anchors for ADR prose: build once, round-trip later
let anchor = BracketWire.build "src/App.fs" (Some 10) (Some 24) None
BracketWire.tryParseBracket anchor   // Some ("src/App.fs", Some 10, Some 24, None)

// Machine-facing ADR facts block
let adr =
    "# GUIDERS-FSHARP-ADR-0007: title\n\nfacts:\n  golden: GS-1\n  hoare:\n    - OB-H1: { pre } transform { post }\nend facts\n"

match AdrFactsParser.tryParse "docs/adr/GUIDERS-FSHARP-ADR-0007.md" adr with
| Some facts ->
    printfn "%d golden id(s), %d obligation(s)" facts.GoldenIds.Length facts.HoareObligations.Length
| None -> printfn "no facts block"
```

## Problem

- Docs drift from code silently: the ADR says one thing, the code does another, and nothing fails.
- Doc-to-code links live as free-text mentions, so tooling can neither navigate nor verify them — "this ADR is implemented" stays unproven prose.

## Solution

- Correspondence becomes typed data: relation kinds, witness rows, and machine-parsable `facts:` blocks (golden ids, Hoare obligations, evidence) — checks can fail builds instead of disappointing readers.
- Pure model only: workspace scanning, resolve and reverse-scan IO are Execution-side consumers of these shapes.
- Correspondence SSOT of the Guiders federation — extraction per [GUIDERS-FSHARP-ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md) · family map [ADR-0028](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0028-documentation-guild-correspondence-family.md); facts blocks per [GUIDERS-FSHARP-ADR-0006](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0006-adr-lifecycle-verifiable-facts.md); obligation catalog per [GUIDERS-FSHARP-ADR-0007](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0007-open-build-ssot-ftc-correspondence.md).

## When not to use

- No workspace scanning or file IO here — records and pure parsers only; resolve/reverse scan live in Execution consumers.
- `tryParseBracket` extracts `F:` / `M:` / `L:` keys only; `S:` / `K:` keys are recognized vocabulary but not returned.
- The Hoare catalog is a fixed pilot list (five ids from GUIDERS-FSHARP-ADR-0007), not an extensible registry; the witness bridge maps member-keyed witnesses only — line-only anchors map to `None`.

## Learn more

- [GUIDERS-FSHARP-ADR-0003 — Model extraction matrix](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)
- [GUIDERS-FSHARP-ADR-0006 — ADR lifecycle: verifiable facts](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0006-adr-lifecycle-verifiable-facts.md)
- [GUIDERS-FSHARP-ADR-0007 — Open build SSOT: FTC + Correspondence](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0007-open-build-ssot-ftc-correspondence.md)
- [ADR-0028 — Documentation guild: Correspondence family map](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0028-documentation-guild-correspondence-family.md)
