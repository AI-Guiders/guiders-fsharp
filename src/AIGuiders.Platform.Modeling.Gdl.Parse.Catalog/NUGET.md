# AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

Parser and validator for GDL command catalog documents: sections, tables, channels and defaults into a typed document with line diagnostics, plus mapping onto the spine payload.

## What you get

- `CatalogParser.parse` — one call from text to a `CatalogParseResult` (`Document` + `Diagnostics`), covering the whole `.catalog.gdl` shape:
  - `catalog <name>` header and `import <...>` lines (collected, not resolved)
  - `defaults` block — `variable.kind`, `command.scope`, `command.surfaces`, `grammar.keyboard.binding`, `grammar.keyboard.melody`, `binding.chord-root`, `command.flavor`
  - `channels` tree with inner `grammar command=/argument=` blocks
  - pipe tables: `helps`, `phrases`, `profiles`, `commands`, `bindings`, `melodies`, `mcp`
  - `executors` key-value section
- Typed document model — `CatalogDocument` plus row records: `CatalogDefaults`, `CatalogChannel`, `CatalogVariable`, `CatalogHelp`, `CatalogPhrase`, `CatalogProfile` / `CatalogProfileEntry`, `CatalogCommandRow`, `CatalogBindingRow`, `CatalogMelodyRow`, `CatalogMcpRow`; `CatalogDocument.empty` as the neutral starting point
- `CatalogGrammarValidator` — post-parse rules with pluggable predicates:
  - `validate` — bindings/melodies require their `grammar.keyboard.*` default; gesture and melody wires checked against the declared grammar via an injectable `WireParse` (`grammarId -> wire -> ok, looksLike`) — pass `None` to skip wire checks and stay parser-free
  - `validateChannels` — every non-palette channel must declare a `grammar` block with both command and argument
  - `validateInvocationSurfaces` — defaults and per-command `surfaces` columns checked against `SurfaceRegistry` (from `AIGuiders.Platform.Modeling.Notations.Command`)
- `CatalogMapping.toRouteEntry` / `toPayload` — document to spine `CatalogPayload { Planet; Routes }`; Domain/Object/Intent come only from explicit `domain`/`object`/`intent` columns, never inferred from the command id shape
- Diagnostics are typed `AuthoringDiagnostic` (code + message + line) from `AIGuiders.Platform.Modeling.Gdl.Authoring` — `MissingCatalogHeader`, `InvalidSyntax`, `DuplicateRow`, `MissingGrammarDeclaration`, `GrammarWireMismatch`, `UnknownInvocationSurface`, ...

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

let result =
    CatalogParser.parse """catalog agent-edit

defaults
  command.scope = agent-edit
end defaults

commands table
  | command | args   | help       | domain | object | intent |
  | open    | <path> | Open a doc |        |        |        |
  | sec.add |        | Add section| sec    | add    | set    |
end commands
"""

let doc = Option.get result.Document          // None + MissingCatalogHeader when the header is absent
let routes = doc.Commands                     // [ { Command = "open"; Columns = ... }; ... ]

let payload = CatalogMapping.toPayload doc    // spine CatalogPayload { Planet = "agent-edit"; Routes }
```

## Problem

- A command catalog file mixes shapes: key-value defaults, a nested channels tree, and half a dozen markdown-style pipe tables. Every tool that reads it re-implements the same scraping — and silently disagrees about unknown lines, duplicate rows, or which column means what.
- Catalog errors surface late: a typo'd gesture or an undeclared keyboard grammar only blows up when the catalog is already shipped and consumed.

## Solution

- One typed parser for the whole document: sections and tables become records, unknown or malformed lines become positioned diagnostics instead of silent drops.
- Grammar rules split from wire parsers: the package owns the rules (which sections require which defaults) and takes the wire parser as a pluggable predicate, so validation stays dependency-free.
- Battle-tested as the F# mirror of the Platform `Authoring.Command.Catalog` parser feeding the GDL spine — [ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) · [ADR-0039](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0039-command-catalog-family.md) · [FSHARP ADR-0001](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md). Domain/Object/Intent columns follow the [slash catalog DOI model](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0154-slash-catalog-domain-object-intent.md).

## When not to use

- You need the full C# authoring emit path: this mirror deliberately leaves out profile bundle resolution (`ICatalogBundleLibrary` / `CatalogProfileResolver`), command-profile cross-validation, import-alias resolution, and `CatalogPhraseSlotIndex` codegen inputs.
- Wire syntax checks without a parser: with `keyboardParse = None` the gesture/melody wire rules are skipped — bring your own `WireParse` or accept unchecked wires.
- Import following: `import <...>` lines are collected as strings; loading and merging imported files is out of scope.

## Learn more

- [ADR-0059 — GDL hyperlane](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)
- [ADR-0039 — command catalog family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0039-command-catalog-family.md)
- [FSHARP ADR-0001 — GDL spine ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md)
- [Slash catalog Domain/Object/Intent (DOI)](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0154-slash-catalog-domain-object-intent.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
