# AIGuiders.Platform.Modeling.Language.Adapters.Gdl

GDL deck-file adapter for the Language Resolver Center — parser-backed diagnostics and outline behind ILanguageBackend.

## What you get

- `GdlLanguageBackend` — an `ILanguageBackend` with `LanguageId = "gdl"`; `CanHandle` accepts `.gdl` and `.gdlproj`:
  - `GetDiagnosticsAsync` — parses `*.deck.gdl` with the GDL deck parser and surfaces each `AuthoringDiagnostic` as an Error-severity `LanguageDiagnostic` (code, message, line/column)
  - `GetDocumentSymbolsAsync` — outline tree for deck files: a `deck` root (named by planet) with attention `preset` children and bound `zone` children
  - `GoToDefinitionAsync`, `FindUsagesAsync`, `GetCompletionsAsync`, `GetSymbolAtPositionAsync`, `RenameSymbolAsync` — registered for contract completeness and intentionally return empty/default results (syntax tier only today)
- Zero IO: the backend works purely from `LanguageRequest.SourceText`

## Quick start

```fsharp
open System.Threading
open AIGuiders.Platform.Modeling.Language
open AIGuiders.Platform.Modeling.Language.Adapters.Gdl

let backend = GdlLanguageBackend() :> ILanguageBackend

let request : LanguageRequest =
    { FilePath = "cockpit.deck.gdl"
      Line = 1
      Column = 1
      SourceText = deckText
      SolutionOrProjectPath = ""
      SessionDefaultLanguageId = LanguageIds.Gdl }

let outline = backend.GetDocumentSymbolsAsync(request, CancellationToken.None).Result
let checks = backend.GetDiagnosticsAsync(request, CancellationToken.None).Result
```

## Problem

- A multi-language workspace wants one verbs surface for every file kind, but structured DSL files are usually left out: they either get no language service at all, or each tool wires a bespoke one-off parser call with its own result shape.
- Deck documents are machine-checked config — without an adapter, typos in them surface only when the deck is loaded, not while editing.

## Solution

- A thin mirror: the deck parser (`Platform.Modeling.Gdl.Parse.Deck`) and its authoring diagnostics project into the shared LRC envelopes, so `.deck.gdl` files answer the same diagnostics/outline calls as any other language in the workspace.
- Parsing stays in the GDL packages; this adapter adds no grammar of its own.
- It fills the GDL slot of the federation Language Resolver Center — [ADR-0061](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md); the deck language itself is specified by [ADR-0059 — GDL hyperlane](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md).

## When not to use

- This mirror does real work only for `*.deck.gdl` — other `.gdl` files (catalog and the rest of the quarry family) pass `CanHandle` but get empty diagnostics and a bare file outline.
- No semantic tier: go-to-definition, find-usages, completions and rename are deliberate stubs returning empty results.
- Coarse positions: all deck diagnostics map to Error severity and preset/zone symbols carry line-level pseudo-spans, not precise token ranges.

## Learn more

- [ADR-0061 — Language Resolver Center](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md)
- [ADR-0059 — GDL hyperlane](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
