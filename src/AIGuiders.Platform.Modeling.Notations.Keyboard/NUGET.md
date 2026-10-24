# AIGuiders.Platform.Modeling.Notations.Keyboard

The normalized key-sequence wire model: modifier flags, chord steps with structural equality, and step lists — one comparable IR behind every keybinding notation.

## What you get

- `ChordModifierKeys` — flags enum: `None`, `Control`, `Alt`, `Shift`, `Meta` (combinable)
- `NormalizedSequenceStep` — the abstract step type
- `NormalizedChordStep` — modifiers + `KeySymbol` (e.g. Ctrl+K), with structural `Equals`/`GetHashCode`
- `NormalizedPlainKeyStep` — a bare `KeySymbol` (e.g. `f` after a chord), also structurally comparable
- `NormalizedKeySequence` — an ordered step list (`Steps`), plus the `Empty` singleton

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Notations.Keyboard

// "Ctrl+K, then F" as one sequence of two steps
let ctrlK = NormalizedChordStep(ChordModifierKeys.Control, "k")
let sequence =
    NormalizedKeySequence(
        [| ctrlK :> NormalizedSequenceStep
           NormalizedPlainKeyStep("f") :> NormalizedSequenceStep |])

// Steps compare structurally — safe as dictionary keys across bindings
let isSame = ctrlK.Equals(NormalizedChordStep(ChordModifierKeys.Control, "k"))  // true
```

## Problem

- Every ecosystem spells keybindings its own way: `ctrl+k ctrl+f`, Vim's `<C-k>`, Emacs's `C-k`. Binding registries, chord matchers, and settings UIs that accept more than one notation need a single comparable representation of "which keys, in which order".
- Without one, key equality devolves to string comparison of whatever notation happened to be typed — `Ctrl+K` and `ctrl+k` and `<C-k>` become three different bindings.

## Solution

- A small wire IR: modifiers as flags, two step kinds with structural equality, sequences as step lists — so any notation's output (and any matcher's input) agrees on what a keypress is.
- The keyboard branch of the Guiders Notations family (the F# mirror of its Keyboard Core); notation readers and the normalizer stay execution-side — [ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md), quarry semantics in [ADR-0016](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0016-input-notation-quarry-family.md).

## When not to use

- Wire model only, no readers: parsing Vim/Emacs/KeyGesture text into these steps — and matching live input against sequences — live platform/execution-side; this package will not turn `ctrl+k` into a `NormalizedChordStep`.
- `NormalizedKeySequence` itself has no structural equality — equality lives on the step types; compare sequences element-wise, or wrap them yourself.
- Key symbols are plain strings (`"k"`, `"F4"`): no key-code mapping, layout awareness, or case normalization — agree on a spelling convention with whoever produces your steps.

## Learn more

- [ADR-0021 — Notations quarry family (Keyboard branch)](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)
- [ADR-0016 — InputNotation quarry + package family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0016-input-notation-quarry-family.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
