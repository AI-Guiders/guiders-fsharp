# GUIDERS-FSHARP-ADR-0009: Document open → language profile routing

| | |
|---|---|
| **Status** | Accepted · Implemented (Code Center plugin backends) |
| **Date** | 2026-09-21 |
| **Tags** | #guiders #fsharp #codecenter #federation #planet #language-profile |
| **Related** | [GUIDERS-FSHARP-ADR-0004](./GUIDERS-FSHARP-ADR-0004-ide-session-modeling-ownership.md) · [GUIDERS-FSHARP-ADR-0005](./GUIDERS-FSHARP-ADR-0005-federation-reframe-cdp-features.md) · DASHSPEC / ADR-0067 (planet CodeCenter slice) · `IDocumentLanguageProfile` |

## Context

Operators ask: *if the editor does not know the language, how does an AST appear?*

The confusion mixes three layers:

1. **Federation shell** (WPF Code Center, `DocumentSession`) — language-agnostic transport: text buffer, caret, ledger, structural-edit API.
2. **Language profile** (`IDocumentLanguageProfile`) — planet-owned semantics: `Rebuild`, `PlanStructural`, projections.
3. **AST** (`ParseTree`, concept tiers) — ephemeral inside planet `Rebuild`; **not** stored in `DocumentSessionState`.

Arbitrary prose (Russian, English, plain `.txt`) has **no** semantic nodes unless a profile is bound. That is correct behaviour, not a gap.

## Decision

**Language is not inferred from buffer content.** It is **routed at document open** (or session factory call) to a concrete `IDocumentLanguageProfile` implementation supplied by a planet or host adapter.

```text
open document
    → resolve LanguageBinding (see routing table below)
    → create DocumentSession(text, profile)
    → profile.Rebuild(text)           // planet parse; AST lives only here
    → DocumentSnapshot in session     // thin projection for UI
```

Federation never parses DashSpec / GDL / F# itself. It only calls the injected profile.

### Routing inputs (priority order, normative target)

| Signal | Example | Binds to |
|--------|---------|----------|
| Plugin runtime open | `CodeCenterPluginRuntime.OpenDocument` | `DashSpecDocumentLanguageProfile` (`dashspec.block`) |
| Project / document kind in solution graph | `ProjectKind.DashSpec`, `DocumentMeta.LanguageId` | planet profile for that kind |
| File extension + host registry | `.dash`, `.gdl`, `.fs` | registered `IDocumentLanguageProfile` or backend |
| `doc://` logical path convention | `doc://dash/demo` | host maps URI scheme segment → profile |
| Fallback | unknown / plain text | `NeutralDocumentLanguageProfile` (`neutral.plain`) → empty nodes |

**Rule:** first resolved binding wins; federation does not scan text to guess language.

### What lives in session state

`DocumentSessionState` holds `Current: DocumentSnapshot` and `Profile: IDocumentLanguageProfile` only.

- `DocumentSnapshot.Text` — source string (serialization view).
- `DocumentSnapshot.Nodes` — federation outline (`Id`, `Name`, spans, `Parent`), **not** AST.
- AST + concept tiers are built inside planet `Rebuild` and may be discarded after projecting the snapshot.

Example (DashSpec planet):

```text
text
  → SyntaxTree.parse              // DashSpec.Modeling.Parse
  → DashSpecConceptGraphBuilder   // tiers + edges
  → DashSpecProfileRebuild        // snapshot + law diagnostics
  → DocumentSession.Current
```

### Structural edits

`ApplyStructural` calls `profile.PlanStructural` then `Refresh` (= `profile.Rebuild` again).

Planet-owned planners (e.g. `DashSpecStructuralPlanner`) use tier/AST knowledge; generic `StructuralPlanGraph` is the neutral fallback (text splice only, no parse).

## Consequences

### For federation (`guiders-fsharp`)

- `IDocumentLanguageProfile` is the **only** extension point for semantic graph + structural planning.
- `NeutralDocumentLanguageProfile` is the explicit “no language” profile — not an error.
- A central **LanguageProfileRegistry** (URI + extension + `ProjectKind` → profile factory) is the intended SSOT; hosts must not scatter ad-hoc `new XxxSession()` without documenting the binding.

### For planets (e.g. `dash-spec`)

- Own parser, AST, concept graph, laws, structural planner.
- Expose `createDocumentSession` / `XxxFederationDocumentSession` that wires `IDocumentLanguageProfile`.
- `ProfileRef.ProfileId` is stable contract (`dashspec.block`, …) for tests and telemetry.

### For surface (`guiders-wpf`)

- Code Center plugins register projections and commands **for sessions already bound** to a language.
- Opening a document = host chooses session type; plugin does not infer language from text.

## Current implementation notes (2026-09-21)

| Area | State |
|------|--------|
| Profile contract | `IDocumentLanguageProfile` in `DocumentLanguageProfile.fs` |
| Neutral fallback | `NeutralDocumentLanguageProfile` → `emptySnapshot` |
| DashSpec binding | `DashSpecCodeCenterLanguageBackend.CreateSession` via `DashSpec.CodeCenter.Plugin`; no direct host session types |
| Central registry | `ICodeCenterLanguageBackend` + `CodeCenterDocumentSessionFactory.Open` (guiders-wpf); Studio uses `PluginRuntime.OpenDocument` |
| IDE `DocumentRegistry` | owns paths / `DocId`; language routing via `LanguageId` is adjacent but not unified with CodeCenter profile SSOT |

## Non-goals

- Content-based language detection (ML or heuristics on buffer text).
- Storing full AST inside `DocumentSession` (projection-only federation model).
- Federation-owned DashSpec parser.

## Open follow-ups

1. `LanguageProfileRegistry` module + conformance test per `ProfileRef`.
2. Wire `DocumentRegistryOps` / `DocumentMeta.LanguageId` → profile factory for solution-hosted documents.
3. ADR cross-link in dash-spec planet docs (DASHSPEC slice references this federation ADR).
