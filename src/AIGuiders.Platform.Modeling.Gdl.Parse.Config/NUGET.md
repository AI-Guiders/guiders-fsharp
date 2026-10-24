# AIGuiders.Platform.Modeling.Gdl.Parse.Config

Parser for GDL config documents: header, ADR provenance, defaults and contracts/sources/facts pipe tables into a typed document with line-coded diagnostics.

## What you get

- `ConfigParser.parseText` — one call from text to a `ConfigParseResult` (`Document` option + `Diagnostics` array); `ConfigParser.parse` accepts pre-read `AuthoringLine` lists
- Document sections recognized:
  - `config <name>` header — missing header means `Document = None` plus a `config-missing-header` diagnostic
  - `based on adr:<id>` — ADR provenance kept as an option on the document
  - `import <...>` lines — accepted and skipped
  - `defaults ... end defaults` — plain `key = value` pairs into a case-insensitive dictionary; unclosed blocks report `config-unclosed-defaults`
  - `sources table` — rows of `id / kind / path / slice` (`ConfigSourceRow`)
  - `contracts table` — rows of `id / requires / ensures` (`ConfigContractRow`); rows without `id` report `config-contract-missing-id` / `config-source-missing-id`
  - `facts table` — rows of `contract / verified_by` (`ConfigFactRow`); only rows with both columns land in the document
- The typed document (`ConfigDocument`, rows `ConfigContractRow` / `ConfigSourceRow` / `ConfigFactRow`) lives in `AIGuiders.Platform.Modeling.Configurations` — this parser feeds that IR
- `ConfigParseDiagnostic` — `{ Code; Message; Line }` with stable string codes (`config-missing-header`, `config-unclosed-defaults`, `config-invalid-default`, ...), easy to assert on in CI

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Parse.Config

let result =
    ConfigParser.parseText """
config newcomer

based on adr:GUIDERS-ADR-0064

defaults
  grammar.hot.section = markdown-section-tags
end defaults

contracts table
  | id              | requires            | ensures                 |
  | hot.personal.l0 | primary_is_personal | hot_l0_sections_present |
"""

let doc = result.Document.Value      // None + `config-missing-header` diagnostic when the header is absent
let contract = doc.Contracts.[0]     // { Id = "hot.personal.l0"; Requires = "primary_is_personal"; ... }
let provenance = doc.BasedOnAdr      // Some "GUIDERS-ADR-0064"
```

## Problem

- Environment and tool configs drift from their documentation: which file a wiring came from, which ADR declared it, and what must hold after install are facts that live in prose, not in the config itself.
- Flat key-value formats (TOML, INI) parse fine but carry no postconditions — a broken install passes the parse and fails operationally, with nothing to check against.

## Solution

- The config document becomes typed data with provenance: named contracts (`requires` / `ensures`), evidence rows (`contract / verified_by`), and sources with slices — parseable by tooling instead of re-readable only by humans.
- The parser is a dependency-free single file producing stable diagnostic codes, so CI can gate on `*.config.gdl` files the same way it gates on code.
- Battle-tested as the F# SSOT mirror of the config-quarry SAT pilot, following [ADR-0064](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0064-config-gdl-quarry-family.md) and the GDL hyperlane ([ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)).

## When not to use

- You need contract evaluation, not just parsing — predicate checking lives in `AIGuiders.Platform.Modeling.Configurations` (`ContractPredicates`); this package only turns text into the document.
- Import following: `import` lines are skipped, not resolved — the parser never touches the filesystem.
- Strict schemas: unknown `... table` blocks are skipped silently and defaults accept any key — if you need unknown-section rejection or value typing, bring your own validation on top of the IR.

## Learn more

- [ADR-0064 — config GDL quarry (declare · wire · contract · Sat)](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0064-config-gdl-quarry-family.md)
- [ADR-0059 — GDL hyperlane](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
