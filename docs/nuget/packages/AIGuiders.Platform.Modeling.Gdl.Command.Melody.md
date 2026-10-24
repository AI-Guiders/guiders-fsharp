package: AIGuiders.Platform.Modeling.Gdl.Command.Melody
tagline: Melody lines: sequential keyboard play sequences (by-note or by-chord steps) that invoke a command after a chord root is engaged.

## What you get

- `MelodyStep` — one step: `Articulation` (`ByNote` — single key, e.g. `b`; `ByChord` — modifier+key as one step, e.g. `Ctrl+R`), `Wire`, optional `ReaderId` for parametric tails
- `MelodyLineProfile` — the line's capture policy:
  - `PureByNote` — every step is a single key (the CIDE/Glass default)
  - `PureByChord` — every step is a simultaneous chord
  - `Mixed` — steps may mix both, declared explicitly
- `MelodyLine` — slug + profile + steps (+ optional help): the play line for one melody alias
- `MelodyDescriptor` — the catalog projection: `CommandId`, `Slug`, `Profile`, `Steps`, `Help`
- `MelodyDescriptor.toLine` — descriptor → play line
- `MelodyDescriptor.fromSlug` — slug-only descriptor with the `PureByNote` default

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Command.Melody

// Catalog row: command "git.commit" playable as the melody "gc"
let descriptor =
    MelodyDescriptor.fromSlug "git.commit" "gc" (Some "Commit staged changes")

let line = MelodyDescriptor.toLine descriptor
line.Profile     // PureByNote
line.Steps       // empty — slug-only descriptor, steps attach as you author them
```

## Problem

- Sequential key sequences ("type a chord root, then play `g c`") are usually ad-hoc strings:
  - nothing says whether `b` means the key `b` or a chord shorthand
  - mixed lines (some steps single keys, some chords) are indistinguishable from sloppy ones
  - the alias that invokes a command and the command itself get conflated into one string
- So two input layers disagree about what a line means.

## Solution

- A line states its capture policy up front (`PureByNote` / `PureByChord` / `Mixed`); each step states its own articulation.
- The slug aliases a real command id — invocation and play are separate rows.
- Battle-tested as the melody mechanic of the Guiders invocation surfaces — [ADR-0015 §7](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0015-invocation-mechanics-slash-melody-binding.md).

## When not to use

- Keystroke capture — this package describes lines; the input layer records them.
- Parametric argument parsing — `ReaderId` names a tail parser; the parsers live execution-side (`Notations.Argument` profiles).
- Static keybindings — that is `Gdl.Command.Binding`.

## Learn more

- [ADR-0015 — invocation mechanics: slash, melody, binding](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0015-invocation-mechanics-slash-melody-binding.md)
