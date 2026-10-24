# AIGuiders.Platform.Modeling.Configurations

IR for declarative setup profiles (*.config.gdl) — defaults, sources, contracts, facts — plus a pure predicate that checks required sections in agent notes.

## What you get

- `ConfigDocument` — the setup-profile record:
  - `Defaults` — grammar ids for how operator wire is read
  - `Sources` — `ConfigSourceRow` (id, kind, path, slice, line) with `{personal}` path templates
  - `Contracts` — `ConfigContractRow` (id, `Requires`, `Ensures`)
  - `Facts` — `ConfigFactRow` (contract → `VerifiedBy` evidence)
- `ContractPredicates.evaluateHotL0SectionsPresent` — pure contract check: hot-notes content (scoped by slice, default `above_public_cut`) must contain every L0 section id from the manifest — a JSON `l0` array or an `l0_manifest:` line in the `memory-architecture-v1` section; returns `Satisfied` + `Note` + a coded `Diagnostic`
- `KnowledgeWire` — minimal pure reader for agent-notes TOML: `tryBuildPersonalRootWire` resolves `[knowledge] primary` → the root path in `[knowledge.roots]`; `parseSectionValue` reads flat keys inside one section

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Configurations

let doc : ConfigDocument =
    { Name = "newcomer"
      BasedOnAdr = None
      Defaults = dict [ "notes.slice", "above_public_cut" ]
      Sources =
        [| { Id = "personal-hot"; Kind = "md"
             Path = "{personal}/agent-notes.md"
             Slice = "above_public_cut"; Line = 1 } |]
      Contracts =
        [| { Id = "hot-l0-present"
             Requires = "sources.personal-hot"
             Ensures = "hot_l0_sections_present"; Line = 1 } |]
      Facts = [| { Contract = "hot-l0-present"; VerifiedBy = "l0 manifest"; Line = 1 } |] }

// Pure predicate over already-loaded strings — no file IO here
let wire : KnowledgeWire.PersonalRootWire =
    { TomlPath = "agent-notes-mcp.toml"; PrimaryId = "personal"; PersonalRoot = "D:/agent-notes" }

let result = ContractPredicates.evaluateHotL0SectionsPresent wire notesMarkdown (Some manifestJson) doc
result.Satisfied    // true when every L0 section id from the manifest exists in the hot notes
```

## Problem

- "The environment is set up" is folklore: a checklist someone ran once, no statement of what must hold, no evidence attached.
- Setup docs drift from the machine — nothing re-checks that required sections or files still exist after tools update.

## Solution

- Setup becomes data: named sources with slices, explicit contracts (`requires` / `ensures`), and evidence rows — checkable by a predicate instead of a human re-reading prose.
- The shipped predicate proves the pattern end to end: hot notes must carry every L0 section declared in the manifest, and failures come back as machine codes, not prose.
- Pilot IR of the config GDL family — [ADR-0064](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0064-config-gdl-quarry-family.md).

## When not to use

- Not a TOML parser: `KnowledgeWire` reads flat `key = value` inside one section; nested tables and arrays are out of scope.
- Exactly one predicate ships (`evaluateHotL0SectionsPresent`); there is no general requires/ensures DSL evaluator — other contracts are IR rows only.
- No file IO and no `*.config.gdl` parsing: content arrives as strings, and the GDL text itself is parsed by the GDL parse family (`AIGuiders.Platform.Modeling.Gdl.Parse.Config`).

## Learn more

- [ADR-0064 — Config GDL quarry family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0064-config-gdl-quarry-family.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
