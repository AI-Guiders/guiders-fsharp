# AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

Typed relation vocabulary for language intelligence — resolve tiers, dependency and type-system edge kinds, document geometry, and the RelationSpec witness exchanged before any resolve or materialize.

## What you get

- `ResolveTier` — `Syntax = 1` / `Semantic = 2`; how deep a stored resolution goes (no text tier, by design)
- Edge-kind vocabularies with stable wire strings (`toWire` / `tryParse` round-trips):
  - `DependencyRelationKind` — `Uses`, `TypeUses`, `Binds`, `Imports`, `SyntaxDepends`
  - `TypeSystemRelationKind` — `ImplementsInterface`, `Extends`, `Instantiates`
- Document geometry (`RelationGeometry.fs`): `TextSpan`, `LineRange` (buffer offsets `[Start, End)` with `create` / `length` / `contains`), `LineRangeHint`, `SymbolRef` (container / name / arity), `DocumentPlace` (`Fragment` / `Obligation` / `Package`), `DocumentRef` (`DocId` / `File`), `NavSeed`, `ResourcePath`, `SurfaceVersion`, `DocumentText`, `DocumentMeta` + `DocumentRegistry`, `DiagnosticRecord` + `DiagnosticIndex`, `ResolveCtx`, `AddressRef`
- `Locus` — where intelligence attaches: `Syntax` (doc + node + span) or `Semantic` (doc + symbol + span)
- `RelationSpec` — the typed witness produced before anything is resolved or materialized: `CodeEdit`, `DocToCode`, `Diag`, `Address`, `Nav`, `Resource`, with `CodeTarget` = `Symbol` / `TreeNode`
- Pure data: depends only on identity ids (`Modeling.Core`) and `LogicalPath` (`Modeling.Paths`) — no parsers, no IO

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

// Edge kind wire round-trip
let edge = DependencyRelationKind.tryParse "type-uses"   // Some TypeUses
let wire = DependencyRelationKind.toWire TypeUses         // "type-uses"

// Buffer-offset geometry
let range = LineRange.create 10 20
LineRange.contains 15 range                                // true

// Where a stored resolution landed
let tier = ResolveTier.Semantic
```

## Problem

- Edit-intelligence features — attach metadata to code, sniper-style edits, doc-to-code links, "jump from this diagnostic" — each invent their own locators: line/column tuples, path strings, path + member pairs.
- They drift, and nothing records how deep a locator actually resolved: a syntax-tree node and a checked symbol are different durability classes, but ad-hoc locators cannot say which one you hold.

## Solution

- One closed, typed vocabulary: `Locus` + `ResolveTier` for places and their depth, edge kinds with stable wire strings, and `RelationSpec` as the single witness exchanged at the host boundary before resolve/materialize — bracket wires and legacy locator strings parse into it, nothing attaches to code without it.
- It is the F# SSOT of the federation's language-intelligence relation surface, superseding the public Anchor entity for attach/persist paths — [ADR-0025](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0025-language-intelligence-boundary.md) · [ADR-0063](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md).

## When not to use

- Expecting engines from the vocabulary: nothing here resolves a `RelationSpec`, computes symbol uses, or touches files — resolvers and hosts live in consuming packages.
- Plain-text addressing by design: `ResolveTier` is `Syntax`/`Semantic` only — if your locators are raw text spans with no syntax or semantic backing, this tier model deliberately has no slot for them.
- LSP-adjacent but not LSP: spans are house-shaped (`TextSpan`, buffer-offset `LineRange`), not UTF-16 line/character conventions, and there is no JSON-RPC mapping.

## Learn more

- [ADR-0025 — LanguageIntelligence boundary](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0025-language-intelligence-boundary.md)
- [ADR-0063 — Anchors federation reincarnation](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
