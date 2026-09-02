# GUIDERS-FSHARP-ADR-0002: Modeling vs Execution (GDL + Notations → F#)

| | |
|---|---|
| **Status** | Accepted (2026-09-02; `Platform.Modeling` / `Platform.Execution` prefix same day) |
| **Tags** | #guiders #fsharp #gdl #notations #ir #modeling #execution #federation |
| **Related** | [GUIDERS-FSHARP-ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) · [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) · [GUIDERS-ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md) · [GUIDERS-ADR-0058](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md) |

## Context

Federation has two **model-heavy** hyperlanes:

| Hyperlane | Today (C#) | Question |
|-----------|--------------|----------|
| **GDL** (`Authoring.*`) | parse `*.{quarry}.gdl` → declare IR | Who owns parsers + IR SSOT? |
| **Notations** (`Notations.*`) | parse wire → normalized IR | Who owns grammar rules + IR SSOT? |

Both are **algebraic**: discriminated shapes, exhaustive `match`, cross-ref validation, conformance vectors — not UI or runtime mechanics.

[ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) placed GDL **spine** and **parse north star** in `guiders-fsharp`. Deck + `PresentationTopology` mirror shipped as `AIGuiders.Platform.Modeling.Gdl.*` (**Phase A done**).

**Notations** ([0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)) follow the same pattern: wire format(s) → branch Core IR → mechanics consume.

One **.NET** world; layer under the existing **Platform** umbrella:

```text
AIGuiders.Platform.Modeling.*     F#   parse, IR, rules, validation, conformance
AIGuiders.Platform.Execution.*    C#   UI, registry, execute, emit host
```

**Why nested under `Platform` (not top-level `AIGuiders.Modeling.*`):**

- Publish/CI globs already target `AIGuiders.Platform.*`
- `Directory.Packages.props` / CPM patterns stay one family
- Migration from flat `Platform.Authoring.*` / `Platform.CommandPlane.*` is a **segment insert**, not a new root
- CDP (`AIGuiders.Cdp.*`), AgentNotes, MCP tools stay **outside** Platform — correct boundary

Repos (`guiders-fsharp`, `guiders-platform`) are transport. The **NuGet prefix** is normative.

## Decision

### 1. Package prefix (normative)

| Prefix | Language | Owns |
|--------|----------|------|
| **`AIGuiders.Platform.Modeling.*`** | F# | GDL + Notations: grammars, IR SSOT, validation, conformance |
| **`AIGuiders.Platform.Execution.*`** | C# | CommandPlane, Studio, Cockpit runtime, emit — **consumers** of Modeling |

**Dependency rule:** `Platform.Execution.*` → `Platform.Modeling.*`. Never a second authoritative IR in Execution.

**Glob patterns (normative intent):**

```text
AIGuiders.Platform.Modeling.*      F# packages (guiders-fsharp)
AIGuiders.Platform.Execution.*     C# packages (guiders-platform)
AIGuiders.Platform.*               umbrella (publish filters, docs — excludes Cdp/AgentNotes)
```

```text
guiders-fsharp                          guiders-platform
├── Platform.Modeling.Gdl.*             ├── Platform.Execution.CommandPlane.*
└── Platform.Modeling.Notations.*       ├── Platform.Execution.Studio.*
                                        ├── Platform.Execution.Cockpit.*
                                        └── Platform.Execution.Emit.*
```

### 2. GDL Modeling packages (target map)

| Package | Owns | Replaces (transitional C#) |
|---------|------|-------------------------|
| `AIGuiders.Platform.Modeling.Gdl.Core` | `GdlProject`, `GdlFragment`, quarry payload spine | — |
| `AIGuiders.Platform.Modeling.Gdl.Authoring` | lexical kit: blocks, tables, import, diagnostics | `Platform.Authoring.Core` |
| `AIGuiders.Platform.Modeling.Gdl.Presentation` | `PresentationTopology`, topology wire parse | `IR.Presentation` |
| `AIGuiders.Platform.Modeling.Gdl.Command` | catalog / bundle IR | `IR.Command` · `Authoring.Command.*` |
| `AIGuiders.Platform.Modeling.Gdl.Cockpit` | cockpit.logic rule graph IR | proposed `Authoring.Cockpit.Logic` |
| `AIGuiders.Platform.Modeling.Gdl.Display` | display binding IR | proposed `Authoring.Display.Binding` |
| `AIGuiders.Platform.Modeling.Gdl.Expression` | shared `ExprNode` for `when` / conditions | proposed `Authoring.Expression` |
| `AIGuiders.Platform.Modeling.Gdl.Parse.*` | quarry parsers (`Deck`, `Catalog`, …) | `Platform.Authoring.*` |
| `AIGuiders.Platform.Modeling.Gdl.Validation` | cross-quarry rules | — |
| `AIGuiders.Platform.Modeling.Gdl.Project` | `*.gdlproj`, import graph | `Authoring.Project` |

**Ship order:** deck + topology + rename (**done**) → catalog → display → cockpit.logic → expression.

### 3. Notations Modeling packages (target map)

| Package | Owns | Replaces (transitional C#) |
|---------|------|----------------------------|
| `AIGuiders.Platform.Modeling.Notations.Core` | profiles, conformance hooks, branch registry | — |
| `AIGuiders.Platform.Modeling.Notations.Keyboard` | `NormalizedKeySequence`, Vim/KeyGesture grammars | `InputNotation.*` · `Platform.Notations.Keyboard.*` |
| `AIGuiders.Platform.Modeling.Notations.Command` | `NormalizedCommandLine`, slash/console path rules | `Platform.Notations.Command.*` |
| `AIGuiders.Platform.Modeling.Notations.Argument` | `NormalizedArguments`, tail profiles | `Platform.Notations.Argument.*` |
| `AIGuiders.Platform.Modeling.Notations.Bracket` | `NormalizedBracketWire`, delimiter profiles | `Platform.Notations.Bracket.*` |

Mechanics in `Platform.Execution.*` call `Platform.Modeling.Notations.*.Parse(wire)` and receive stable IR.

### 4. Execution packages (target map)

| Package | Owns | Replaces (flat `Platform.*` at 0.31.x) |
|---------|------|----------------------------------------|
| `AIGuiders.Platform.Execution.CommandPlane.*` | catalog/registry, execute, completion | `Platform.CommandPlane.*` |
| `AIGuiders.Platform.Execution.Studio.*` | WPF / product surfaces | `guiders-wpf`, Studio hosts |
| `AIGuiders.Platform.Execution.Cockpit.*` | DataBus, transport, composition runtime | `Platform.Cockpit.*` |
| `AIGuiders.Platform.Execution.Emit.*` | Roslyn `*.g.cs` from Modeling IR | platform emit tools |
| `AIGuiders.Platform.Execution.MCPlane` | agent envelope | `Platform.MCPlane` |
| `AIGuiders.Platform.Execution.Routing` | intent route/execute seam | `Platform.Routing` |

Flat `AIGuiders.Platform.Authoring.*`, `Platform.IntermediateRepresentation.*`, `Platform.Notations.*` (model halves) → **abandon at 0.31.x**; new work ships under `Platform.Modeling.*` only.

### 5. IR lives in Modeling — Execution consumes

| IR class | SSOT | Execution access |
|----------|------|------------------|
| Declare IR (GDL) | `Platform.Modeling.Gdl.*` | PackageReference; optional `[<CLIMutable>]` on hot records |
| Wire IR (Notations) | `Platform.Modeling.Notations.*` | same |
| Runtime-only views | `*.g.cs` emit | generated snapshots where WPF/registry need it |

### 6. NuGet identity migration

**Target IDs** use `AIGuiders.Platform.Modeling.*` / `AIGuiders.Platform.Execution.*`.

| Transitional | Target |
|--------------|--------|
| flat `AIGuiders.Platform.Authoring.*` | `AIGuiders.Platform.Modeling.Gdl.*` (per quarry) |
| `AIGuiders.Platform.IntermediateRepresentation.*` | absorbed into `Platform.Modeling.*` |
| `AIGuiders.Platform.Notations.*` | `AIGuiders.Platform.Modeling.Notations.*` |
| `AIGuiders.Platform.CommandPlane.*` | `AIGuiders.Platform.Execution.CommandPlane.*` |
| `AIGuiders.Platform.Cockpit.*` | `AIGuiders.Platform.Execution.Cockpit.*` |

**Default path:** flat `Platform.*` at **0.31.x** stops receiving releases; new IDs from **1.0** (or next wave). Federation repos update refs in one window. NuGet deprecation/unlist **optional** — downloads are mostly CI restore ([§ nuget reality](https://github.com/NuGet/Home/issues/931)), external adopters negligible.

Namespaces and assembly names **SHOULD** match `PackageId`.

### 7. Boundary: GDL vs Notations (unchanged semantics)

| | GDL | Notations |
|---|-----|-----------|
| **When** | declare-time | runtime wire |
| **Prefix** | `Platform.Modeling.Gdl.*` | `Platform.Modeling.Notations.*` |

No merged mega-AST ([0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) §9).

### 8. Migration phases

```text
Phase A (done)     Platform.Modeling.Gdl.* rename + deck/topology mirror
Phase B           catalog, display, cockpit.logic under Platform.Modeling.Gdl.*
Phase C           Platform.Modeling.Notations.* (F#)
Phase D           Platform.Execution.* for runtime; abandon flat Platform.* 0.31.x
Phase E           LSP / toolchain globs: Platform.Modeling.* + Platform.Execution.*
```

**Shim rule:** C# wrapper MAY re-export Modeling types for one release cycle; MUST NOT fork IR shapes.

## Consequences

- Layer visible in package name **and** under `Platform` glob — publish scripts, CPM, docs need one new segment, not a new root.
- F# in `guiders-fsharp`, C# in `guiders-platform` — unchanged; only `PackageId` / namespace alignment.
- `AIGuiders.Cdp.*`, `AgentNotes.*`, MCP packages unaffected.

## Non-goals

- Top-level `AIGuiders.Modeling.*` (rejected — breaks Platform glob family)
- Rewriting CommandPlane execution engine in F#
- Single unified AST across GDL + Notations
- TS/Kotlin native Notations ports (per [0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md))
