# GUIDERS-FSHARP-ADR-0002: Model guild ownership (GDL + Notations → F#)

| | |
|---|---|
| **Status** | Accepted (2026-09-02) |
| **Tags** | #guiders #fsharp #gdl #notations #ir #model-guild #federation |
| **Related** | [GUIDERS-FSHARP-ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) · [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) · [GUIDERS-ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md) · [GUIDERS-ADR-0058](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md) |

## Context

Federation has two **model-heavy** hyperlanes:

| Hyperlane | Today (C#) | Question |
|-----------|--------------|----------|
| **GDL** (`Authoring.*`) | parse `*.{quarry}.gdl` → declare IR | Who owns parsers + IR SSOT? |
| **Notations** (`Notations.*`) | parse wire → normalized IR | Who owns grammar rules + IR SSOT? |

Both are **algebraic**: discriminated shapes, exhaustive `match`, cross-ref validation, conformance vectors — not UI or runtime mechanics.

[ADR-0001](./GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md) placed GDL **spine** and **parse north star** in `guiders-fsharp`. Deck + `PresentationTopology` mirror shipped (`AIGuiders.Gdl.Authoring`, `AIGuiders.Gdl.Presentation`, `AIGuiders.Gdl.Parse.Deck`).

**Notations** ([0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)) follow the same pattern: wire format(s) → branch Core IR → mechanics consume. Keyboard chords, slash paths, argument tails, bracket profiles — all model-heavy. They belong in the same **model guild** as GDL, not in the execution guild.

`guiders-platform` should remain the **runtime guild**: CommandPlane mechanics, WPF/Studio surfaces, Roslyn emit host, MCP bridges — **consumers** of IR, not definers.

## Decision

### 1. Two guilds, two repos (unchanged monorepo pattern)

```text
guiders-fsharp — MODEL GUILD (F# SSOT)
├── Gdl.*           declare-time: parse, IR, validation, project graph
└── Notations.*     runtime wire: grammar, profiles, normalized IR

guiders-platform — EXECUTION GUILD (C#)
├── CommandPlane.*  catalog/registry, execute, completion glue
├── MCPlane         agent envelope
├── Studio / WPF    product surfaces
└── Emit.*          Roslyn *.g.cs (reads F# IR, writes C# runtime)
```

**Rule:** C# **MUST NOT** maintain a parallel authoritative IR definition once the F# package ships. C# references F# NuGet or generated projections at emit boundaries only.

### 2. GDL model packages (target map)

NuGet prefix: **`AIGuiders.Gdl.*`** in `guiders-fsharp`.

| Package | Owns | Replaces (transitional C#) |
|---------|------|----------------------------|
| `AIGuiders.Gdl.Core` | `GdlProject`, `GdlFragment`, quarry payload spine | — |
| `AIGuiders.Gdl.Authoring` | lexical kit: blocks, tables, import, diagnostics | `Platform.Authoring.Core` |
| `AIGuiders.Gdl.Presentation` | `PresentationTopology`, topology wire parse | `IR.Presentation` + `Notations.Presentation.Topology` (declare) |
| `AIGuiders.Gdl.Command` | catalog / bundle IR | `IR.Command` + `Authoring.Command.*` |
| `AIGuiders.Gdl.Cockpit` | cockpit.logic rule graph IR | proposed `Authoring.Cockpit.Logic` |
| `AIGuiders.Gdl.Display` | display binding IR | proposed `Authoring.Display.Binding` |
| `AIGuiders.Gdl.Expression` | shared `ExprNode` for `when` / conditions | proposed `Authoring.Expression` |
| `AIGuiders.Gdl.Parse.*` | quarry parsers (`Deck`, `Catalog`, …) | `Platform.Authoring.*` parsers |
| `AIGuiders.Gdl.Validation` | cross-quarry rules | partial today |
| `AIGuiders.Gdl.Project` | `*.gdlproj`, import graph | `Authoring.Project` |

**Ship order (normative intent):** deck + topology (**done**) → catalog → display → cockpit.logic → expression → full `GdlProject` loader.

### 3. Notations model packages (target map)

NuGet prefix: **`AIGuiders.Notations.*`** in `guiders-fsharp` (sibling tree to `Gdl.*`, same monorepo).

| Package | Owns | Replaces (transitional C#) |
|---------|------|----------------------------|
| `AIGuiders.Notations.Core` | shared profiles, conformance hooks, branch registry | — |
| `AIGuiders.Notations.Keyboard` | `NormalizedKeySequence`, Vim/KeyGesture grammars | `InputNotation.*` |
| `AIGuiders.Notations.Command` | `NormalizedCommandLine`, slash/console path rules | `CommandPlane.Slash` inline + `Notations.Command.*` |
| `AIGuiders.Notations.Argument` | `NormalizedArguments`, tail profiles | `Notations.Argument.*` |
| `AIGuiders.Notations.Bracket` | `NormalizedBracketWire`, delimiter profiles | `Notations.Bracket.*` |

Each branch: **wire parser(s) + IR types + conformance vectors** in F#. Mechanics in C# call `Notations.*.Parse(wire)` and receive stable IR records/enums.

### 4. IR lives in F# — C# consumes

| IR class | SSOT | C# access |
|----------|------|-----------|
| Declare IR (GDL output) | `AIGuiders.Gdl.*` | ProjectReference / NuGet; optional `[<CLIMutable>]` on hot records |
| Wire IR (Notations output) | `AIGuiders.Notations.*` | same |
| Runtime-only views | `*.g.cs` emit | generated from F# IR snapshots where WPF/registry need it |

**No** long-lived `AIGuiders.Platform.IntermediateRepresentation.*` duplicate. Platform IR packages become **type-forward shims** during migration, then removed.

### 5. Boundary: GDL vs Notations (unchanged semantics)

| | GDL | Notations |
|---|-----|-----------|
| **When** | declare-time (authoring, CI, LSP) | runtime wire (keyboard, slash, console) |
| **Surface** | `*.{quarry}.gdl`, `*.gdlproj` | chord, `/path`, `key=value`, `[…]` |
| **Example** | `topology (MFD)(F)` in deck | `<C-k>`, `/docs adr open` |
| **Repo** | `AIGuiders.Gdl.*` | `AIGuiders.Notations.*` |

Topology appears in **both** only at different layers: deck quarry declares logical hosts (GDL); optional future wire profiles may reference channel tokens at Notations boundary — **distinct IR types**, no merged mega-AST ([0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) §9 non-goal).

### 6. Migration phases

```text
Phase A (now)     F# GDL mirror: deck + PresentationTopology + spine DU
Phase B           F# catalog, display, cockpit.logic parsers + IR packages
Phase C           F# Notations branches (Keyboard → Command → Argument → Bracket)
Phase D           platform Authoring.* / IR.* / Notations.* → thin shim or obsolete
Phase E           authoring-toolchain LSP pins F# packages; conformance vectors run both sides until shim removed
```

**Shim rule:** C# wrapper MAY re-export F# types for one release cycle; MUST NOT fork IR shapes.

### 7. What stays C# (execution guild)

| Stays C# | Why |
|----------|-----|
| `CommandPlane.*` | execute, registry, completion orchestration |
| `MCPlane` | agent pulse / envelope |
| Studio / WPF / product hosts | UI binding |
| Roslyn emit pipeline | generates C# from F# IR |
| Planet-specific adapters | not federation SSOT |

## Consequences

- One **model guild** language (F#) for declare + wire grammars; operators learn DU/`match` once across GDL and Notations.
- `guiders-fsharp` grows beyond `Gdl.*` into `Notations.*` — still one sibling monorepo, not a third repo.
- Platform ADRs ([0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md), [0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md)) remain normative for **semantics**; **implementation SSOT** moves to F# packages listed above.
- Conformance: `docs/conformance/` vectors stay language-neutral JSON; F# and C# shims both run vectors until Phase D completes.

## Non-goals

- Rewriting CommandPlane execution engine in F#
- Rewriting WPF Studio in F#
- Single unified AST across GDL quarries **or** across GDL + Notations (IR stays per branch)
- TS/Kotlin native Notations ports (still per [0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md) — reference quarry on NuGet; native port per stack)
