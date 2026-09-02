# Guiders F# (GDL spine)

F# sibling monorepo for **model-heavy** federation hyperlanes — starting with **GDL** (Guiders Declarative Language) spine types and validation.

| Repo | Role |
|------|------|
| **guiders-platform** (C#) | `Authoring.*` parsers, `Notations.*`, emit, surfaces |
| **guiders-fsharp** (F#) | `GdlFragment` DU, `GdlProject`, validation, future metrics/SI |

Normative naming: [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md) (`*.{quarry}.gdl`).

## Packages (v0)

| NuGet id | Purpose |
|----------|---------|
| `AIGuiders.Gdl.Core` | `GdlFragment`, quarry payloads, `GdlProject` |
| `AIGuiders.Gdl.Validation` | project-level rules |

## Build

```bash
dotnet build
dotnet test
```

## Learn F# here

Read `src/AIGuiders.Gdl.Core/GdlTypes.fs` first — discriminated unions and records used as federation spine. Ask questions in chat against this repo; no separate textbook pass required.

## ADR

- [GUIDERS-FSHARP-ADR-0001 — GDL spine ownership](docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md)
