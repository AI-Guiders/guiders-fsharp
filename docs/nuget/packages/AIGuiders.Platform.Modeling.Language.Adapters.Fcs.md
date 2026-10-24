package: AIGuiders.Platform.Modeling.Language.Adapters.Fcs
tagline: F# Compiler Service backend for the Language Resolver Center — F# diagnostics, outline, navigation, cross-project find-usages, completions and rename behind ILanguageBackend.

## What you get

- `FcsLanguageBackend` — an `ILanguageBackend` over `FSharpChecker` (`LanguageId = "fsharp"`, handles `.fs` / `.fsx` / `.fsproj`):
  - `GetDiagnosticsAsync` — parse + type-check diagnostics; `.fsx` resolves standalone from script options; `.fs` without a resolvable fsproj falls back to parse-only diagnostics
  - `GetDocumentSymbolsAsync` — parse-only outline: modules and nested modules (with children), `let` bindings, type definitions; container chains like `App.Math.calculate`
  - `GoToDefinitionAsync` / `FindUsagesAsync` — symbol-use based; find-usages spreads across the F# projects of the solution graph when bound, deduplicated by range + file
  - `GetCompletionsAsync` — declaration list after partial long names / `.`
  - `GetSymbolAtPositionAsync` — kind, name, and qualified name at cursor
  - `RenameSymbolAsync` — workspace rename as file writes: `Apply = false` returns the planned `Changes`, `Apply = true` writes via the patch applier; active patterns and active-pattern cases are rejected with a message
- Execution-bound seams (the modeling layer does no File IO — a host binds implementations at startup):
  - `IFcsProjectOptionsSource` + `FcsProjectOptions` (`bindSource`, `tryGet`, `invalidate`) — fsproj context for semantic checks
  - `IFcsSourceTextSource` + `FcsSourceText`, `IFcsProjectOwnershipSource` + `FcsProjectOwnership` (`resolveFsproj`), `IFcsSolutionGraphSource` + `FcsSolutionGraph`
  - `IFcsSessionPatchApplier` + `FcsSessionPatchApply` — disk-backed patch apply
- `FcsSessionPatchBridge` — pure mapping between rename edits and the `Ide.Session` `SessionPatch` (`renameFileChangesToPatch`, `patchToRenameFileChanges`, `tryApplyPatch`)
- `FcsProbeWire` + `FcsProbeWireMapping.toFcsOptions` — pure probe wire to `FSharpProjectOptions`, guarded by `FcsProjectOptionsGuards.requireFrameworkReferences` (rejects options missing `System.Runtime`)

## Quick start

```fsharp
open System.Threading
open AIGuiders.Platform.Modeling.Language
open AIGuiders.Platform.Modeling.Language.Adapters.Fcs

let backend = FcsLanguageBackend() :> ILanguageBackend

// .fsx scripts resolve standalone — no host binding needed
let request : LanguageRequest =
    { FilePath = "script.fsx"
      Line = 1
      Column = 1
      SourceText = "let x = 1 + \"oops\""
      SolutionOrProjectPath = ""
      SessionDefaultLanguageId = LanguageIds.Fsharp }

let result = backend.GetDiagnosticsAsync(request, CancellationToken.None).Result
```

## Problem

- You want F# language smarts outside an IDE — an agent tool, a review bot, a doc pipeline — and wiring `FSharpChecker` by hand means re-solving the same puzzles every time:
  - script (`.fsx`) vs project (`.fsproj`) options resolution
  - extracting symbol uses for go-to-definition and find-usages
  - a rename that touches multiple files without corrupting edits
- Doing it inline also drags disk and solution-graph IO into what should be a pure, testable core.

## Solution

- One backend that owns the FCS mechanics and speaks the kernel envelopes; everything disk-shaped (options, source text, ownership, solution graph, patch apply) goes through bindable seams, so the adapter stays usable pure and the host binds real IO at startup.
- Renames are computed as per-file writes from deduplicated symbol uses, applied through the session-patch bridge.
- This is the F# slot of the federation Language Resolver Center, run by its CDP host — [ADR-0061](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md), on the solution/session SSOT of [GUIDERS-FSHARP-ADR-0004](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md).

## When not to use

- This mirror speaks F# only — C#, TypeScript and friends need their own backends; the shared contract lives in `AIGuiders.Platform.Modeling.Language`.
- Expecting project-grade results without binding the host side: with no `IFcsProjectOptionsSource`/ownership bound, `.fs` files degrade to parse-only diagnostics and completions/find-usages/rename return empty (`.fsx` still resolves standalone).
- Renaming active patterns (and their cases) — rejected by design; and there is no formatting, no code fixes — the verb set is diagnostics, outline, navigation, usages, completions, rename.

## Learn more

- [ADR-0061 — Language Resolver Center](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0061-language-resolver-center.md)
- [GUIDERS-FSHARP-ADR-0004 — IDE Session modeling ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md)
