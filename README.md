# Guiders F# (Platform.Modeling)

F# monorepo for **`AIGuiders.Platform.Modeling.*`** — GDL parse, IR, validation, future Notations.

| Repo | NuGet prefix | Role |
|------|--------------|------|
| **guiders-fsharp** | `Platform.Modeling.*` | F# model layer (GDL, Notations) |
| **guiders-dotnet-platform** | `Platform.Execution.*` (target) | C# runtime, emit, surfaces |

Normative naming: [GUIDERS-FSHARP-ADR-0002](docs/adr/GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md) · [GUIDERS-ADR-0059](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0059-gdl-hyperlane.md).

## Packages

| NuGet id (prefix `AIGuiders.Platform.`) | Purpose | Tests |
|------------------------------------------|---------|:-----:|
| `Modeling.Core` | intent envelope shapes — IntentOutcome, RoutedIntent, pulse rules | — |
| `Modeling.Build` | neutral build-result model — diagnostics with anchor wires | — |
| `Modeling.Catalog` | profile-driven catalog index algebra — collision policies, layer merge | — |
| `Modeling.Cockpit.Cds` | CDS routing decision shapes — attention, desk detail, go-map | — |
| `Modeling.Cockpit.DataBus` | event catalog, dispatch policy, projection graph | ✓ |
| `Modeling.Cockpit.Rules` | headless cockpit rule evaluation — traceable rule id | — |
| `Modeling.Combinations` | merge semantics DU, combinator alias, ordered fold laws | ✓ |
| `Modeling.Gdl.Agent` | IR.Agent port — response envelope, detail tier, next hints | ✓ |
| `Modeling.Gdl.Authoring` | lexical kit — F# mirror of Platform Authoring.Core | — |
| `Modeling.Gdl.Command` | IR.Command port — descriptor, arg-tail policy, catalog route entry | ✓ |
| `Modeling.Gdl.Command.Binding` | IR.Binding port — targets, well-known keys, gesture entry | ✓ |
| `Modeling.Gdl.Command.Melody` | IR.Melody port — articulation, line profiles, steps | ✓ |
| `Modeling.Gdl.Core` | spine — GdlFragment DU, payloads from layer IRs, GdlProject | ✓ |
| `Modeling.Gdl.Correspondence` | CRS wire shapes, anchor graph nodes, forward/reverse maps | ✓ |
| `Modeling.Gdl.Expression` | shared GDL expression IR — literals, compares, boolean ops | — |
| `Modeling.Gdl.Language` | IR.Language port — tiers, loci, anchors, edits, sniper scopes | ✓ |
| `Modeling.Gdl.Parse.CockpitLogic` | cockpit rule graph IR — rules, projectors, principles | — |
| `Modeling.Gdl.Parse.Deck` | deck quarry parser — F# mirror of Platform Authoring.Deck | ✓¹ |
| `Modeling.Gdl.Presentation` | presentation IR + TopologyNotation, screen binding | ✓ |
| `Modeling.Gdl.Validation` | cross-quarry project rules | — |
| `Modeling.Ide.Session` | IDE solution session graph IR — projects, lifecycle, graphs | ✓ |
| `Modeling.Ide.Session.Ports.DotNet` | slnx/sln/csproj → SolutionGraph port | ✓ |
| `Modeling.Ide.Session.Ports.Workspace` | md/json/toml/yaml tree → WorkspaceGraph port | ✓ |
| `Modeling.Language` | Language Resolver Center kernel envelopes | ✓ |
| `Modeling.Language.Adapters.Fcs` | F# Compiler Service backend | ✓² |
| `Modeling.Language.Adapters.Gdl` | GDL quarry backend for LRC | — |
| `Modeling.Navigation` | navigation scene IR — anchor, nodes, edges, caps | ✓ |
| `Modeling.Navigation.Policy` | related kinds, presets, merge, kind filter, profile | — |
| `Modeling.Notations.Argument` | argument slot schema, normalized invocation args | ✓ |
| `Modeling.Notations.Bracket` | bracket notation IR — axis shapes, wire normalization | ✓ |
| `Modeling.Notations.Command` | slash body, KV atom, bracket-aware list split | ✓ |
| `Modeling.Notations.Keyboard` | keyboard wire model — key sequences, chord steps | — |
| `Modeling.Paths` | logical repo paths and physical path boundary | — |
| `Modeling.Routing` | route refusal algebra for intent organs | — |

¹ Deck parser tests live in `Modeling.Gdl.Core.Tests` (DeckParserTests).
² FCS smoke test is env-dependent in this workspace — pre-existing, tracked separately.

## Build

```bash
dotnet build AIGuiders.Platform.Modeling.slnx
dotnet test AIGuiders.Platform.Modeling.slnx
```

## License

Software: [MIT](LICENSE) ([OSI text](https://opensource.org/license/MIT)) · Ethical use: [declaration](https://github.com/AI-Guiders/licensing/blob/main/docs/ethical-use.md)

Public federation repo. NuGet prefix `AIGuiders.Platform.Modeling.*` (sibling checkout of `guiders-dotnet-platform` required for LRC adapter builds).

## Learn F#

Read `src/AIGuiders.Platform.Modeling.Gdl.Core/GdlTypes.fs` first — discriminated unions and records used as federation spine.

## ADR

- [GUIDERS-FSHARP-ADR-0001 — GDL spine ownership](docs/adr/GUIDERS-FSHARP-ADR-0001-gdl-spine-ownership.md)
- [GUIDERS-FSHARP-ADR-0002 — Platform.Modeling vs Platform.Execution](docs/adr/GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md)
