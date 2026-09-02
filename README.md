# Guiders F# (Platform.Modeling)

F# monorepo for **`AIGuiders.Platform.Modeling.*`** — GDL parse, IR, validation, future Notations.

| Repo | NuGet prefix | Role |
|------|--------------|------|
| **guiders-fsharp** | `Platform.Modeling.*` | F# model layer (GDL, Notations) |
| **guiders-dotnet-platform** | `Platform.Execution.*` (target) | C# runtime, emit, surfaces |

Normative naming: [GUIDERS-FSHARP-ADR-0002](docs/adr/GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) · [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md).

## Packages

| NuGet id | Purpose |
|----------|---------|
| `AIGuiders.Platform.Modeling.Gdl.Core` | `GdlFragment`, quarry payloads, `GdlProject` |
| `AIGuiders.Platform.Modeling.Gdl.Authoring` | lexical kit (blocks, import, diagnostics) |
| `AIGuiders.Platform.Modeling.Gdl.Presentation` | `PresentationTopology` IR + wire parse |
| `AIGuiders.Platform.Modeling.Gdl.Parse.Deck` | `*.deck.gdl` parser |
| `AIGuiders.Platform.Modeling.Gdl.Validation` | cross-quarry project rules |

## Build

```bash
dotnet build AIGuiders.Platform.Modeling.slnx
dotnet test AIGuiders.Platform.Modeling.slnx
```

## Learn F#

Read `src/AIGuiders.Platform.Modeling.Gdl.Core/GdlTypes.fs` first — discriminated unions and records used as federation spine.

## ADR

- [GUIDERS-FSHARP-ADR-0001 — GDL spine ownership](docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md)
- [GUIDERS-FSHARP-ADR-0002 — Platform.Modeling vs Platform.Execution](docs/adr/GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md)
