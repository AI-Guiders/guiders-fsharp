# GUIDERS-FSHARP-ADR-0002: Modeling vs Execution (GDL + Notations → F#)

| | |
|---|---|
| **Status** | Accepted (2026-09-02; package prefix amended same day) |
| **Tags** | #guiders #fsharp #gdl #notations #ir #modeling #execution #federation |
| **Related** | [GUIDERS-FSHARP-ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) · [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) · [GUIDERS-ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md) · [GUIDERS-ADR-0058](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md) |

## Context

Federation has two **model-heavy** hyperlanes:

| Hyperlane | Today (C#) | Question |
|-----------|--------------|----------|
| **GDL** (`Authoring.*`) | parse `*.{quarry}.gdl` → declare IR | Who owns parsers + IR SSOT? |
| **Notations** (`Notations.*`) | parse wire → normalized IR | Who owns grammar rules + IR SSOT? |

Both are **algebraic**: discriminated shapes, exhaustive `match`, cross-ref validation, conformance vectors — not UI or runtime mechanics.

[ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) placed GDL **spine** and **parse north star** in `guiders-fsharp`. Deck + `PresentationTopology` mirror shipped under transitional IDs (`AIGuiders.Gdl.*`).

**Notations** ([0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)) follow the same pattern: wire format(s) → branch Core IR → mechanics consume. Keyboard chords, slash paths, argument tails, bracket profiles — all model-heavy.

One **.NET** world; two **layers** by responsibility, not by accident of language:

```text
AIGuiders.Modeling.*    F#   parse, IR, rules, validation, conformance
AIGuiders.Execution.*   C#   UI, registry, execute, emit host, MCP glue
```

Repos (`guiders-fsharp`, `guiders-platform`) are transport. The **NuGet prefix** is the normative axis.

## Decision

### 1. Package prefix (normative)

| Prefix | Language | Owns |
|--------|----------|------|
| **`AIGuiders.Modeling.*`** | F# | GDL + Notations: grammars, IR SSOT, validation, conformance |
| **`AIGuiders.Execution.*`** | C# | CommandPlane, Studio, emit, planet adapters — **consumers** of Modeling |

**Dependency rule:** `Execution` → `Modeling`. Never a second authoritative IR in `Execution`.

```text
guiders-fsharp (Modeling monorepo)          guiders-platform (Execution monorepo)
├── AIGuiders.Modeling.Gdl.*                ├── AIGuiders.Execution.CommandPlane.*
└── AIGuiders.Modeling.Notations.*          ├── AIGuiders.Execution.Studio.*
                                            └── AIGuiders.Execution.Emit.*
```

### 2. GDL Modeling packages (target map)

| Package | Owns | Replaces (transitional) |
|---------|------|-------------------------|
| `AIGuiders.Modeling.Gdl.Core` | `GdlProject`, `GdlFragment`, quarry payload spine | `AIGuiders.Gdl.Core` |
| `AIGuiders.Modeling.Gdl.Authoring` | lexical kit: blocks, tables, import, diagnostics | `AIGuiders.Gdl.Authoring` · `Platform.Authoring.Core` |
| `AIGuiders.Modeling.Gdl.Presentation` | `PresentationTopology`, topology wire parse | `AIGuiders.Gdl.Presentation` · `IR.Presentation` |
| `AIGuiders.Modeling.Gdl.Command` | catalog / bundle IR | `IR.Command` · `Authoring.Command.*` |
| `AIGuiders.Modeling.Gdl.Cockpit` | cockpit.logic rule graph IR | proposed `Authoring.Cockpit.Logic` |
| `AIGuiders.Modeling.Gdl.Display` | display binding IR | proposed `Authoring.Display.Binding` |
| `AIGuiders.Modeling.Gdl.Expression` | shared `ExprNode` for `when` / conditions | proposed `Authoring.Expression` |
| `AIGuiders.Modeling.Gdl.Parse.*` | quarry parsers (`Deck`, `Catalog`, …) | `AIGuiders.Gdl.Parse.*` · `Platform.Authoring.*` |
| `AIGuiders.Modeling.Gdl.Validation` | cross-quarry rules | `AIGuiders.Gdl.Validation` |
| `AIGuiders.Modeling.Gdl.Project` | `*.gdlproj`, import graph | `Authoring.Project` |

**Ship order:** deck + topology (**done**, transitional IDs) → catalog → display → cockpit.logic → expression → full `GdlProject` loader → **rename to `Modeling.*` prefix** (§6).

### 3. Notations Modeling packages (target map)

| Package | Owns | Replaces (transitional C#) |
|---------|------|----------------------------|
| `AIGuiders.Modeling.Notations.Core` | profiles, conformance hooks, branch registry | — |
| `AIGuiders.Modeling.Notations.Keyboard` | `NormalizedKeySequence`, Vim/KeyGesture grammars | `InputNotation.*` |
| `AIGuiders.Modeling.Notations.Command` | `NormalizedCommandLine`, slash/console path rules | `CommandPlane.Slash` inline · `Notations.Command.*` |
| `AIGuiders.Modeling.Notations.Argument` | `NormalizedArguments`, tail profiles | `Notations.Argument.*` |
| `AIGuiders.Modeling.Notations.Bracket` | `NormalizedBracketWire`, delimiter profiles | `Notations.Bracket.*` |

Mechanics in `AIGuiders.Execution.*` call `Modeling.Notations.*.Parse(wire)` and receive stable IR.

### 4. Execution packages (target map)

| Package | Owns | Replaces (transitional) |
|---------|------|-------------------------|
| `AIGuiders.Execution.CommandPlane.*` | catalog/registry, execute, completion | `AIGuiders.Platform.CommandPlane.*` |
| `AIGuiders.Execution.Studio.*` | WPF / product surfaces | `AIGuiders.Platform.Studio.*` |
| `AIGuiders.Execution.Emit.*` | Roslyn `*.g.cs` from Modeling IR | platform emit tools |
| `AIGuiders.Execution.MCPlane` | agent envelope | `MCPlane` |

`AIGuiders.Platform.*` and `AIGuiders.Platform.IntermediateRepresentation.*` → shim → obsolete (no long-lived IR duplicate).

### 5. IR lives in Modeling — Execution consumes

| IR class | SSOT | Execution access |
|----------|------|------------------|
| Declare IR (GDL) | `AIGuiders.Modeling.Gdl.*` | PackageReference; optional `[<CLIMutable>]` on hot records |
| Wire IR (Notations) | `AIGuiders.Modeling.Notations.*` | same |
| Runtime-only views | `*.g.cs` emit | generated snapshots where WPF/registry need it |

### 6. NuGet identity migration (transitional `AIGuiders.Gdl.*`)

Early bootstrap packages used **`AIGuiders.Gdl.*`** before the Modeling prefix was chosen. **Target ID is always `AIGuiders.Modeling.Gdl.*`.**

| Transitional (rename from) | Target |
|----------------------------|--------|
| `AIGuiders.Gdl.Core` | `AIGuiders.Modeling.Gdl.Core` |
| `AIGuiders.Gdl.Authoring` | `AIGuiders.Modeling.Gdl.Authoring` |
| `AIGuiders.Gdl.Presentation` | `AIGuiders.Modeling.Gdl.Presentation` |
| `AIGuiders.Gdl.Parse.Deck` | `AIGuiders.Modeling.Gdl.Parse.Deck` |
| `AIGuiders.Gdl.Validation` | `AIGuiders.Modeling.Gdl.Validation` |

**Default path (no public consumers yet):** rename `PackageId` / namespace in-repo; old IDs were never published — **no** NuGet deprecation or unlist step.

**If an old ID was published** and something still references it: prefer **new package IDs + update federation references** in one window. Deprecation (`<PackageDeprecation>`) and unlist are **optional** polish for external adopters, not federation requirements — abandoned IDs can simply stop receiving releases while planets move to `Modeling.*` / `Execution.*`.

Platform `AIGuiders.Platform.*` → `AIGuiders.Execution.*` follows the same rule: rename when ready; formal deprecate only if we care about third-party discoverability.

Namespaces and assembly names **SHOULD** match `PackageId` (`AIGuiders.Modeling.Gdl.Core`, etc.).

### 7. Boundary: GDL vs Notations (unchanged semantics)

| | GDL | Notations |
|---|-----|-----------|
| **When** | declare-time (authoring, CI, LSP) | runtime wire (keyboard, slash, console) |
| **Surface** | `*.{quarry}.gdl`, `*.gdlproj` | chord, `/path`, `key=value`, `[…]` |
| **Prefix** | `AIGuiders.Modeling.Gdl.*` | `AIGuiders.Modeling.Notations.*` |

No merged mega-AST across branches ([0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) §9).

### 8. Migration phases

```text
Phase A (now)     F# GDL mirror under transitional AIGuiders.Gdl.* IDs
Phase B           catalog, display, cockpit.logic Modeling packages
Phase C           Notations Modeling branches
Phase D           Rename → AIGuiders.Modeling.* / Execution.*; update federation refs (deprecate old NuGet IDs only if needed)
Phase E           Platform shims removed; LSP pins Modeling packages
```

**Shim rule:** C# wrapper MAY re-export Modeling types for one release cycle; MUST NOT fork IR shapes.

## Consequences

- Layer is visible in the **package name** — no guessing whether a dependency is model or runtime.
- One F# dialect across GDL + Notations; one C# dialect across Execution — same .NET, clear boundary.
- Renaming cost is **front-loaded** in Phase D; semantics ADRs ([0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md), [0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)) stay valid; only implementation IDs change.

## Non-goals

- Rewriting CommandPlane execution engine in F#
- Rewriting WPF Studio in F#
- Single unified AST across GDL quarries **or** across GDL + Notations
- TS/Kotlin native Notations ports (per [0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md))
