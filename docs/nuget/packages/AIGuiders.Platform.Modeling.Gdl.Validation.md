package: AIGuiders.Platform.Modeling.Gdl.Validation
tagline: Cross-document validation for composed GDL projects — duplicate paths, duplicate deck presets, and unknown zone references reported as structured issues.

## What you get

- `ValidationSeverity` — `Error` | `Warning`
- `ValidationIssue` — structured finding: `Code`, `Severity`, `Message`, optional `DocumentPath`
- `GdlProjectValidation.validate` — runs the rule set over a `GdlProject`:
  - `GDL_DUPLICATE_DOCUMENT` (Error) — the same logical document path appears twice
  - `GDL_DECK_DUPLICATE_PRESET` (Error) — a deck declares the same preset name more than once
  - `GDL_DECK_UNKNOWN_ZONE` (Warning) — a preset references a zone id missing from the zones table
- `GdlProjectValidation.isValid` — true when no Error-severity issues were found

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Core
open AIGuiders.Platform.Modeling.Gdl.Validation

// Same logical path loaded twice -> GDL_DUPLICATE_DOCUMENT
let entry =
    { Ref = GdlSpine.documentRef "deck/studio.deck.gdl" "deck"
      Fragment = GdlFragment.Deck(GdlSpine.dashSpecStudioDeck ()) }

let project : GdlProject =
    { Manifest = { Name = "studio"; WorkspaceRoot = "." }
      Documents = [ entry; entry ] }

for issue in GdlProjectValidation.validate project do
    printfn "[%O] %s: %s" issue.Severity issue.Code issue.Message

if not (GdlProjectValidation.isValid project) then
    failwith "project has errors"
```

## Problem

- When a project is assembled from many hand-edited documents, the costly mistakes live between files: the same document pulled in twice, two presets with the same name fighting over one slot, a preset pointing at a zone that was renamed or never existed.
- Per-file parsers cannot see any of this — they only ever hold one document at a time.

## Solution

- Project-level checks over the composed `GdlProject`: every finding is a structured issue with a stable code, a severity, and the offending document path — ready for CI gates, editor squiggles, or plain logging.
- `isValid` answers the gate question directly: warnings do not block, errors do.
- This is the validation layer of the GDL spine owned by the F# monorepo ([FSHARP-ADR-0001](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md)); the document model it checks is `Modeling.Gdl.Core`, and the deck rules mirror the deck grammar specified in [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md).

## When not to use

- Three rules only (duplicate paths, duplicate deck preset names, unknown zone refs) — it does not re-parse text or check grammar; syntax errors belong to the `Gdl.Parse.*` packages.
- Only deck documents are inspected; catalog, display, and cockpit-logic fragments pass through unchecked.
- No warning policy hooks: severity is fixed per rule, and `isValid` intentionally ignores warnings.

## Learn more

- [FSHARP-ADR-0001 — GDL spine ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md)
- [GUIDERS-ADR-0059 — GDL hyperlane](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)
