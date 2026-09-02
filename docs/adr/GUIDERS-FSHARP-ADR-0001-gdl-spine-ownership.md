# GUIDERS-FSHARP-ADR-0001: GDL spine ownership (F# monorepo)

| | |
|---|---|
| **Status** | Accepted — v1 platform mirror in progress (2026-09-02) |
| **Tags** | #guiders #fsharp #gdl #federation #spine #ir |
| **Related** | [GUIDERS-FSHARP-ADR-0002](./GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) · [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) · [GUIDERS-ADR-0048](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0048-authoring-quarry-family.md) · [GUIDERS-ADR-0004](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0004-core-monorepo.md) |

## Context

GDL needs a **composed project model** (`GdlProject`, quarry payloads, cross-ref validation). C# 15 union types ship with .NET 11 (preview). F# has production-ready discriminated unions today and fits metrics / SI / rule pipelines planned for IMC.Portal and federation.

`guiders-platform` already owns C# `Authoring.*` parsers for `*.catalog.gdl`, `*.deck.gdl`, …

## Decision

### 1. `guiders-fsharp` sibling monorepo

Pattern: same as **guiders-core** ([0004](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0004-core-monorepo.md)) — not nested inside guiders-platform.

### 2. Ownership split

| Layer | Repo | Language |
|-------|------|----------|
| GDL **spine** (DU, project graph, validation) | `guiders-fsharp` | F# |
| GDL **parse** (wire → payload) | `AIGuiders.Platform.Modeling.Gdl.*` (north star) · `AIGuiders.Gdl.*` (transitional) · flat `Platform.Authoring.*` (transitional C#) | F# |
| Runtime wire parse | `AIGuiders.Platform.Modeling.Notations.*` (north star) · flat `Platform.Notations.*` (transitional C#) | F# |
| GDL **emit** (Roslyn `*.g.cs`) | `AIGuiders.Platform.Execution.Emit.*` | C# |
| Runtime mechanics | `AIGuiders.Platform.Execution.*` | C# |

**SSOT for `GdlFragment` / `GdlProject`:** `AIGuiders.Platform.Modeling.Gdl.Core` (F#; transitional ID `AIGuiders.Gdl.Core` until [ADR-0002](./GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) Phase D).

### 3. Interop (v0 → v1)

1. **v0:** C# parsers keep local AST (`DeckDocument`); tests map to F# `GdlFragment` manually.
2. **v1 (in flight):** F# mirror — `AIGuiders.Gdl.Authoring`, `AIGuiders.Gdl.Presentation`, `AIGuiders.Gdl.Parse.Deck`; parity tests vs platform fixtures.
3. **v2:** `Authoring.*` references F# packages or becomes thin shim; mappers at parse boundary only where C# planets still need DTOs.
4. **Public C# API:** use F# records with `[<CLIMutable>]` or thin DTOs when planets need idiomatic C#.

### 4. Quarry plugin model

New quarry = new case on `GdlFragment` + validation rules + (optional) F# metrics module. Compiler exhaustiveness on `match` enforces updates across validation.

## Consequences

- Federation gains a sanctioned F# hyperlane without forking C# platform.
- IMC.Portal and other planets may reference `AIGuiders.Gdl.*` NuGet packages.
- Learn-by-doing: `GdlTypes.fs` is the canonical F# onboarding artifact for operators.

## Non-goals (v0)

- Catalog / cockpit.logic parsers in F# (deck + topology shipped first)
- `guiders-fsharp` CI publish to nuget.org (local build + git first)
