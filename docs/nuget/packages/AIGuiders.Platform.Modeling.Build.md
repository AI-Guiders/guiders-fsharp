package: AIGuiders.Platform.Modeling.Build
tagline: Neutral build-result model — toolchain diagnostics shaped into referenceable witnesses, plus the freeze-tree request/outcome for isolated build worktrees.

## What you get

- `RawDiagnostic` — a build diagnostic exactly as the toolchain reports it at the execution boundary: `File`, `Line`, `Column`, `Code`, `Message`
- `BuildDiagnostic` — the shaped form: raw fields plus `Spec: RelationSpec`, a `Diag` witness minted when the diagnostic is ingested — the diagnostic becomes a referenceable entity, not an array-index stub
- `FreezeRequest` — freeze a revision tree: `EntryPath` (required), optional `FocusPath` (nullability mirrors C# `string?`)
- `FrozenTree` — the freeze outcome the consumer owns:
  - `Ok` / `Error` — outcome
  - `PlanId` — identity of the frozen copy; release = discard by plan id
  - `GitRoot` / `WorkRoot` / `PlanScope` — where the frozen tree lives and what it covers
  - `BaseTreeSha` + `OverlayPathCount` — base revision plus how many dirty (work-in-progress) paths are overlaid on it
- `Freeze.validate` — pure request validation (`null` = valid)
- `Freeze.pulse` — one status line for a tree: `frozen · <planId> · overlay=<n> · base=<sha7>` or the failure

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Build

let req : FreezeRequest = { EntryPath = "src/App/App.fsproj"; FocusPath = null }
let error = Freeze.validate req        // null = well-formed

let tree : FrozenTree =
    { Ok = true; Error = null; PlanId = "plan-42"
      GitRoot = @"D:\repo"; WorkRoot = @"D:\repo\.frozen\plan-42"
      PlanScope = "src"; BaseTreeSha = "1a2b3c4d"; OverlayPathCount = 3 }

Freeze.pulse tree   // "frozen · plan-42 · overlay=3 · base=1a2b3c4"
```

## Problem

- Build results come back as loose strings — file, line, code, message — that carry no identity. Tests, gates and navigation cannot point at *this* diagnostic without inventing positional stubs that rot on the next rebuild.
- Building in a shared checkout means contention: two builds or one build plus one experiment fight over the same `bin`/`obj`, and "clean first" becomes the workflow.

## Solution

- Diagnostics get a minted witness: `RawDiagnostic` is shaped into `BuildDiagnostic` with a `RelationSpec.Diag` reference (the ingest lives in `AIGuiders.Platform.Modeling.Ide.Session` `BuildDiagnosticOps`).
- `FrozenTree` makes isolation a value: a worktree frozen at HEAD with the dirty paths overlaid — the consumer is the sole owner of its `WorkRoot`, so `bin`/`obj` contention disappears by construction (parity with the C# `WorktreePlanRunner.FrozenTree` execution binding).
- Both are the model layer of the open-build SSOT bet — [FSHARP ADR-0007](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0007-open-build-ssot-ftc-correspondence.md).

## When not to use

- Running builds — no compiler or MSBuild invocation here; runners are Execution. This package is the result/plan shape only.
- Diagnostic analysis — severity mapping and filtering happen at ingest (Ide.Session), not here; `BuildDiagnostic` just carries the raw fields plus the witness.
- It does not create worktrees — `FrozenTree` describes the outcome; git worktree management lives in the execution runner.

## Learn more

- [FSHARP ADR-0007 — Open build SSOT: FTC + Correspondence](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0007-open-build-ssot-ftc-correspondence.md)
