# GUIDERS-FSHARP-ADR-0001: GDL spine ownership (F# monorepo)

| | |
|---|---|
| **Status** | Accepted (v0 bootstrap 2026-09-02) |
| **Tags** | #guiders #fsharp #gdl #federation #spine #ir |
| **Related** | [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) · [GUIDERS-ADR-0048](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0048-authoring-quarry-family.md) · [GUIDERS-ADR-0004](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0004-core-monorepo.md) |

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
| GDL **parse** (wire → payload) | `guiders-platform` | C# `Authoring.*` |
| GDL **emit** (Roslyn `*.g.cs`) | `guiders-platform` / assist | C# |
| Runtime wire | `guiders-platform` | C# `Notations.*` |

**SSOT for `GdlFragment` / `GdlProject`:** `AIGuiders.Gdl.Core` (F#).

### 3. Interop (v0 → v1)

1. **v0:** C# parsers keep local AST (`DeckDocument`); tests map to F# `GdlFragment` manually.
2. **v1:** `Authoring.*` references `AIGuiders.Gdl.Core`; mappers at parse boundary.
3. **Public C# API:** use F# records with `[<CLIMutable>]` or thin DTOs when planets need idiomatic C#.

### 4. Quarry plugin model

New quarry = new case on `GdlFragment` + validation rules + (optional) F# metrics module. Compiler exhaustiveness on `match` enforces updates across validation.

## Consequences

- Federation gains a sanctioned F# hyperlane without forking C# platform.
- IMC.Portal and other planets may reference `AIGuiders.Gdl.*` NuGet packages.
- Learn-by-doing: `GdlTypes.fs` is the canonical F# onboarding artifact for operators.

## Non-goals (v0)

- Rewriting `Authoring.Deck` parser in F#
- Full `PresentationTopology` port (wire string stub on `DeckPreset` only)
- `guiders-fsharp` CI publish to nuget.org (local build + git first)
