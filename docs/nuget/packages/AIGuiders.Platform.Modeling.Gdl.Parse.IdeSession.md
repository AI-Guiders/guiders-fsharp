package: AIGuiders.Platform.Modeling.Gdl.Parse.IdeSession
tagline: Parser for the gates table of ide-session GDL catalogs: gate ids, reject conditions and verifier references into a typed gate catalog with stable diagnostics.

## What you get

- `IdeSessionCatalogParser.parseText` — one call from catalog text plus a source path to an `IdeSessionCatalogParseResult` (`Catalog` option + `Diagnostics` array); `parse` accepts pre-read `AuthoringLine` lists
- Finds the single `gates table` block anywhere in a `*.catalog.gdl` document and parses its pipe-table rows:
  - `gate` — stable gate id (required per row; rows without it report `ide-session.gates.row`)
  - `reject-when` — human-readable condition under which the gate rejects
  - `code` — verifier reference (which check proves/disproves the condition)
- The typed result (`IdeSessionGateCatalog { SourcePath; Gates }`, rows `IdeSessionGateRow { GateId; RejectWhen; Code }`) comes from `AIGuiders.Platform.Modeling.IdeSession.GateCatalog`
- Stable diagnostic codes for CI assertions:
  - `ide-session.gates.missing` — no `gates table` block in the file
  - `ide-session.gates.unclosed` — block not terminated by `end gates`
  - `ide-session.gates.row` — a row missing the `gate` column
  - `ide-session.gates.empty` — table present but no usable rows

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Parse.IdeSession

let result =
    IdeSessionCatalogParser.parseText
        """
catalog ide-session

gates table
  | gate          | reject-when                   | code                     |
  | types-check   | typecheck failed or not run   | HoareChecker.checkTypes  |
  | scope-leak    | patch scope wider than change | GraphPatch.scope         |
end gates
"""
        "ide-session.catalog.gdl"

let catalog = result.Catalog.Value        // None + `ide-session.gates.missing` when the table is absent
let gate = catalog.Gates.[0]              // { GateId = "types-check"; RejectWhen = ...; Code = ... }
```

## Problem

- Editor/IDE session pipelines need go/no-go gates ("reject when typecheck failed", "reject when a change outscopes its patch") — but those rules live as scattered checks in code, with no single list of what gates exist, what they reject on, or what verifies them.
- When a gate list does exist as text, it is usually a markdown table nobody parses: no line-precise errors, no stable codes, nothing for CI to assert against.

## Solution

- The gate list becomes a typed table inside the session's catalog document: one parser call yields the gate rows plus stable diagnostic codes, so gate definitions can be added or reviewed like code.
- Gate conditions stay declarative strings (`reject-when` prose, `code` verifier reference) — hosts resolve them to real checks; the parser stays dependency-free.
- Battle-tested as the F# SSOT mirror of the ide-session gate catalog per [FSHARP ADR-0004](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) under the session orchestrator model of [ADR-0062](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md).

## When not to use

- Full catalog parsing — this parser deliberately reads only the `gates table` and ignores every other catalog section (commands, channels, defaults, ...); take `AIGuiders.Platform.Modeling.Gdl.Parse.Catalog` for the rest of the document.
- Gate enforcement — `reject-when` and `code` are plain strings; nothing here compiles or evaluates the conditions.
- A gates-free catalog is a hard failure by design: without a `gates table` the result is `Catalog = None` plus a diagnostic, even when the rest of the catalog file is valid.

## Learn more

- [FSHARP ADR-0004 — ide session modeling ownership](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md)
- [ADR-0062 — ide solution session orchestrator](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0062-ide-solution-session-orchestrator.md)
