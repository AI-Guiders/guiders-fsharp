package: AIGuiders.Platform.Modeling.Language
tagline: Language Resolver Center kernel — LSP-shaped request/result envelopes, language id resolution, and the ILanguageBackend seam for multi-language tooling.

## What you get

- Kernel envelopes, all `[<CLIMutable>]` records in one namespace:
  - `SourceSpan` — path + line/column range (1-based)
  - `Severity` — `Error` / `Warning` / `Info` / `Hint`
  - `LanguageDiagnostic`, `LanguageSymbol` (hierarchical, with `Children`), `LanguageNavigation`, `LanguageReference`, `LanguageCompletion`
  - Result payloads: `DiagnosticsResult`, `DocumentSymbolsResult`, `CompletionsResult`, `FindUsagesResult`, `SymbolAtPositionResult`, `RenameSymbolResult` (+ `RenameFileChange`)
  - `LanguageRequest` — one request shape: file path, line/column, source text, solution/project path, session language id
- `ILanguageBackend` — the backend seam: `LanguageId`, `CanHandle(path, hint)`, and async verbs `GetDiagnosticsAsync`, `GetDocumentSymbolsAsync`, `GoToDefinitionAsync`, `FindUsagesAsync`, `GetCompletionsAsync`, `GetSymbolAtPositionAsync`, `RenameSymbolAsync`
- `LanguageIds` — well-known language id literals: `Csharp`, `Fsharp`, `Typescript`, `Gdl`, `PowerShell`, `Python`, `Delphi`, `Dashspec`, `Sql` (+ `SqlPostgres`, `SqlMssql`, `SqlSqlite`), `Latex`
- `LanguagePathRules.resolveLanguageId` — extension/filename-based resolution: `.cs`, `.fs`/`.fsx`, `.ts`/`.tsx`/`.js`/`.jsx`, `.ps1`/`.psm1`, `.py`, `.pas`/`.dpr`, `.gdl`, `.sql`, the TeX family (`.tex`/`.sty`/`.cls`/`.bib`/…), project files (`.sln`/`.slnx`/`.csproj`/`.fsproj`/`.gdlproj`), and `tsconfig.json` / `pyproject.toml`
- `DashSpecPathRules` — `.ds` extension check (`isDashSpecPath`)
- `ProjectHint` / `RenameSymbolRequest` envelopes, and `ILanguageActivationCatalog` — optional host-side activation seam
- `TextEdit` + `BufferEditOutcome` (with `BufferEditOutcome.fromText`) — LSP-shaped single-edit and buffer command result

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Language

// Which language owns this file?
let languageId = LanguagePathRules.resolveLanguageId "src/App/Program.fs"
// Some "fsharp" — .fs/.fsx map to LanguageIds.Fsharp

// The single request shape every backend verb consumes
let request : LanguageRequest =
    { FilePath = "src/App/Program.fs"
      Line = 3
      Column = 7
      SourceText = "let answer = 42"
      SolutionOrProjectPath = "App.sln"
      SessionDefaultLanguageId = LanguageIds.Fsharp }
```

## Problem

- Tools that host many languages — language servers, review bots, agent IDEs, doc generators — keep asking the same two questions:
  - which language owns this file (extension sniffing, again, per tool)
  - what shape do the answers take (diagnostics, outline, go-to-definition, find-usages, completions, rename)
- Without a shared contract each backend grows its own tuples or JSON, and dispatch becomes a flat `if language` switch with per-language result shapes.

## Solution

- One typed contract for the verbs and one rule-set for language id resolution, in a leaf package with no parser dependencies.
- Adapters project into these envelopes only — no backend AST leaks into the kernel; dispatch/registry stays with the host.
- This is the kernel of the federation's Language Resolver Center, dogfooded by its CDP host and F#/GDL/SQL adapters — [ADR-0061](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md), seam defined in [GUIDERS-FSHARP-ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md).

## When not to use

- Expecting language work from this package — it is envelopes, resolution rules, and the seam; parsing/checking/navigation live in adapter packages (for F#, see `AIGuiders.Platform.Modeling.Language.Adapters.Fcs`).
- Needing the LSP wire protocol — shapes are LSP-like but there is no JSON-RPC, no position-encoding negotiation; transport is yours.
- Needing a backend registry or multiplexer — the kernel defines the contract only; hosting and routing across backends belong to the host process.

## Learn more

- [ADR-0061 — Language Resolver Center](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md)
- [ADR-0067 — Language Profile federation model](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0067-language-profile-federation-model.md)
- [GUIDERS-FSHARP-ADR-0003 — Model extraction matrix](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)
