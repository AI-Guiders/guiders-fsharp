# AIGuiders.Platform.Modeling.Notations.Core

Two bracket-aware string primitives every text notation shares: the KV atom (key + sign + value) and a top-level list split that respects nesting.

## What you get

- `NotationKvPair` — the KV wire atom as a record: `Key`, `Sign`, `Value`
- `NotationKvPair.trySplitFirst segment sign` — split on the first sign only (`Result<NotationKvPair, string>`); the value may itself contain more signs, so `title: Hello: world` stays intact
- `NotationListSplit.splitTopLevel text separator openBracket closeBracket` — split a list on the separator only at bracket depth 0, so `;` inside `[...]` groups does not cut

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Notations

// KV atom: split on the first ':' only — the value keeps later signs
let axis =
    NotationKvPair.trySplitFirst "title: Hello: world" ':'
// Ok { Key = "title"; Sign = ':'; Value = "Hello: world" }

// List split: ';' at depth 0 cuts; ';' inside brackets does not
let parts =
    NotationListSplit.splitTopLevel "a:b; n:[x; y]; c:d" ';' '[' ']'
// ["a:b"; " n:[x; y]"; "c:d"]
```

## Problem

- Small text formats keep re-solving the same two puzzles by hand:
  - `key: value` pairs where the value may contain the sign (`url: https://…`)
  - lists where entries may contain the separator — nested groups like `[a:1; b:[c; d]]`
- Plain `String.Split` breaks on both: it splits on every sign and on every separator, nested or not.

## Solution

- Two named, tested primitives instead of ad-hoc `Split` calls: the KV atom splits on the first sign only, the list split tracks bracket depth.
- The shared atoms of the Guiders notation family (command tails, bracket references, keyboard readers all compose from them) — [ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md), bracket usage in [ADR-0026](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0026-notations-bracket-branch.md); F# port target in [FSHARP-ADR-0003 §4.4](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md).

## When not to use

- No quoting or escaping: only bracket depth protects a separator; if your values can contain `;`, `[`, `]` or the sign without wrapping brackets, this will mis-split.
- Not a bracket normalizer: it never validates or strips the brackets themselves — paired-delimiter grammars (terminals, profiles, nested re-parse) live in the Notations.Bracket package.

## Learn more

- [ADR-0021 — Notations quarry family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)
- [ADR-0026 — Notations.Bracket branch](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0026-notations-bracket-branch.md)
- [FSHARP-ADR-0003 — Model extraction matrix §4.4](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
