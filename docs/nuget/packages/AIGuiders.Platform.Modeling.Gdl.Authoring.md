package: AIGuiders.Platform.Modeling.Gdl.Authoring
tagline: Lexical kit for GDL-style config files — numbered comment-stripped lines, keyword-delimited blocks, a unified import directive, and the host-agnostic authoring project model.

## What you get

- `AuthoringLine` + `AuthoringSource` — raw text to numbered lines: 1-based `LineNumber`, `#` comment stripping (`stripComment`, `fromRawLines`, `fromText` with CRLF normalization)
- `AuthoringSurfaceKind` — `KeyValue` | `Table` | `IndentedTree`; `BlockReader.tryParseOpener` recognizes section openers:
  - `IndentedTree` — `channels`
  - `KeyValue` — `defaults`, `executors`
  - `Table` (also via the ` table` opener suffix) — `variables`, `helps`, `phrases`, `profiles`, `commands`, `bindings`, `melodies`, `mcp`
- `BlockReader.read` — reads a block body up to `end <keyword>`, skipping blank lines; an unclosed block yields `IsClosed = false` plus an `InvalidSyntax` diagnostic
- `AuthoringImportLine.tryParse` — the unified include directive: `import "logical/path"`, `import <wire/library>`, optional `as <alias>`; legacy `!include` parses with `Legacy = true`
- `AuthoringDiagnostic` / `AuthoringDiagnosticCode` — 15 lexing/grammar codes (`MissingDeckHeader`, `GrammarWireMismatch`, `DuplicateRow`, `EntryOutsideWorkspace`, …) with line numbers
- `AuthoringProject` model — `AuthoringDocumentRef` (`LogicalFile` vs `FederationImport`), `ResolvedAuthoringDocument`, `AuthoringProject`, `AuthoringProjectLoadResult`; `PathBoundary.tryToLogical` maps a physical path to a workspace-relative logical path (pure string math, no file IO)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Authoring

// Raw text -> 1-based lines, '#' comments stripped
let lines =
    AuthoringSource.fromText
        "commands table\n  name  phrase\n  build \"Build the project\"  # inline comment\nend commands"

// Section opener: keyword + surface kind
let opener = BlockReader.tryParseOpener "commands table"
// Some { Keyword = "commands"; Kind = Table }

// Block body up to "end commands" (diagnostics collect via ref)
let diagnostics = ref []
let block = BlockReader.read lines 1 "commands" diagnostics
// block.IsClosed = true; block.Body has the two content lines

// Import directive: quoted = logical path, angle brackets = wire library
let import = AuthoringImportLine.tryParse "import <grain/date-filter> as dash-date"
// Some { Kind = WireLibrary; Path = "grain/date-filter"; Alias = Some "dash-date"; Legacy = false }
```

## Problem

- Every hand-edited DSL re-implements the same lexical chores: strip comments, keep line numbers for diagnostics, split the file into `keyword … end keyword` blocks, resolve includes.
- Include syntax drifts per file type (`import` here, `!include` there), and tooling — editors, language servers, build checks — pays that drift back with bespoke handling per grammar.

## Solution

- One lexical kit shared by all GDL grammars: lines and blocks stay dumb, diagnostics keep line numbers, and includes use a single normative `import` directive with two target shapes (logical path vs wire library) per [GUIDERS-ADR-0052](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0052-unified-import-directive.md); the project graph types follow [GUIDERS-ADR-0051](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0051-authoring-project-abstraction.md).
- This is the F# mirror of the C# `Authoring.Core` kit, with parity tracked in [FSHARP-ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md); in this repo it is the lexical foundation of the catalog, deck, config, and IDE-session parsers (`Modeling.Gdl.Parse.*`) and the Gdl language adapter.

## When not to use

- Grammar and IR parsing are not here — the kit stops at lines, openers, blocks, and import directives; quarry-specific parsing lives in the `Gdl.Parse.*` packages.
- It is a deliberate F# mirror of the C# `AIGuiders.Platform.Authoring.Core` — on the C# side use the original package instead.
- No file IO: everything is pure functions over pre-loaded text; loading documents from disk is the host's job (and `PathBoundary` is pure path arithmetic).

## Learn more

- [GUIDERS-ADR-0051 — Authoring project abstraction](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0051-authoring-project-abstraction.md)
- [GUIDERS-ADR-0052 — Unified import directive](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0052-unified-import-directive.md)
- [FSHARP-ADR-0003 — Model extraction matrix](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)
