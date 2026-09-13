# GUIDERS-FSHARP-ADR-0008: Invocation surfaces — F# modeling SSOT

| | |
|---|---|
| **Status** | Accepted (Wave I0 — types + registry; Execution wiring I1+) |
| **Date** | 2026-09-13 |
| **Tags** | #guiders #fsharp #invocation #commandplane #surface #projection |
| **Related** | [0002](./GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) · [GUIDERS-ADR-0009](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0009-command-surface-pattern.md) · [GUIDERS-ADR-0015](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0015-invocation-mechanics-slash-melody-binding.md) · [GUIDERS-ADR-0043](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0043-invocation-line-phase.md) · [GUIDERS-ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md) §13.1 |

## Context

Federation accepts **one command, many invocation surfaces** (ADR-0009). Today:

- `CommandDescriptor.Surfaces` = untyped GDL strings (`slash.bar`, `console.filter`)
- `InvocationEngageKind` = Slash | Melody | Binding only
- Notation `SurfaceId` on keyboard readers = **dialect**, not invoker channel
- Surface projection (MCP tool name, sigil strip) = prose only

Operator normative (2026-09-13): surfaces invoke; Notations encode wire; **same command** across slash/console/binding/MCP.

## Decision

### 1. New package: `AIGuiders.Platform.Modeling.Invocation` (F# SSOT)

Owns typed **invocation surface** algebra — not cockpit UI surface, not notation dialect.

```text
Modeling.Invocation          SurfaceId, SurfaceSpec, WireKind, Stage, CanonicalInvocation, Registry
Modeling.Notations.*         wire → Normalized* IR
Modeling.Gdl.Command         catalog descriptor (Surfaces strings until I1)
Execution.CommandPlane       resolve, execute (consumes Invocation types via thin C# bridge I2+)
```

### 2. Core types (I0)

| Type | Role |
|------|------|
| `InvocationSurfaceId` | Stable invoker channel (`SlashBar`, `ConsoleFilter`, `McpTool`, …) |
| `InvocationWireKind` | Which notation parser applies (`SlashPath`, `ConsolePath`, `KeySequence`, `None`) |
| `InvocationSurfaceSpec` | Id + default Engage + WireKind + surface-projection flag |
| `CanonicalInvocation` | Post-resolve semantic unit (`CommandId` + path + args) |
| `InvocationStage` | Pipeline DU: Raw → SurfaceProjected → WireParsed → Resolved |

`InvocationEngageKind` and `InvocationLinePhase` **remain** in `Modeling.Notations.Command` until I3 move-up (avoid breaking C# interop in I0).

### 3. Naming disambiguation (I5)

| Legacy | Target name |
|--------|-------------|
| `IKeyboardNotationReader.SurfaceId` | `NotationDialectId` |
| Catalog `Surfaces: string[]` | `InvocationSurfaceId[]` (+ GDL validate) |
| Cockpit `*Surface*` | unchanged (different bounded context) |

### 4. Projection model (normative)

```text
CanonicalInvocation  ←  one command semantics
        ↑ resolve (CommandPlane — I2+)
InvocationStage.WireParsed  ←  Notations.*
        ↑ parse
InvocationStage.SurfaceProjected  ←  optional strip (MCP, @intent)
        ↑
InvocationStage.Raw(surface, text)
```

Console vs Slash = **different WireKind**, often **same CanonicalInvocation** after resolve.

MCP = `InvocationSurfaceId.McpTool`, `WireKind.None`, surface projection required — **not** `Notations.Argument.Json` in v1.

### 5. Waves

| Wave | Deliverable |
|------|-------------|
| **I0** ✓ | F# package + registry + conformance tests (this ADR) |
| **I1** | GDL catalog validate `command.surfaces` → typed ids |
| **I2** | C# `CommandDescriptor` shim |
| **I3** | CommandPlane pipeline calls F# `InvocationStage` |
| **I4** | Forge / MCPlane adoption |
| **I5** | Rename notation dialect id; remove string surfaces |

## Consequences

- One place to answer «which surface is this?» without stringly GDL drift.
- F# exhaustive `match` on `InvocationSurfaceId` for planet extenders (`Custom`).
- Execution stays thin — no second IR in C#.

## Non-goals (I0)

- Moving `InvocationEngageKind` out of Notations.Command
- C# CommandPlane refactor
- New `McpEngage` enum value (defer I4)
