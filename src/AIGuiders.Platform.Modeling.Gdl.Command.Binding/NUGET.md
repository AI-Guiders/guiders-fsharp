# AIGuiders.Platform.Modeling.Gdl.Command.Binding

Keybinding catalog rows: binding keys, gesture wires, and target kinds (command, chord root, surface opener) for keyboard catalogs.

## What you get

- `BindingDescriptor` — one catalog row: `BindingKey` + `GestureWire` + `TargetKind`
- `BindingTargetKind` — what the row points at:
  - `Command` — binding key is a command id
  - `ChordRoot` — the well-known `cascade_chord` key, upgraded automatically by `fromFlatEntry`
  - `SurfaceOpener` — opens a UI surface
- `BindingDescriptor.fromFlatEntry` — flat catalog entry → descriptor (validates non-empty key/wire, upgrades `cascade_chord` to `ChordRoot`)
- `BindingDescriptor.commandId` — `Some id` for command-target rows, `None` otherwise
- `BindingEntry` — descriptor plus optional `NormalizedKeySequence` (from `Notations.Keyboard`)
- `BindingWellKnownKeys.CascadeChord` — the shared `"cascade_chord"` literal
- `BindingDocumentFormat` — TOML / JSON markers for binding documents

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Command.Binding

// Flat catalog row → descriptor
let run = BindingDescriptor.fromFlatEntry "build.run" "ctrl+alt+r"
run.TargetKind                          // Command
run.CommandId                           // Some "build.run"  (via commandId)

// The well-known chord root is upgraded automatically
let chord = BindingDescriptor.fromFlatEntry BindingWellKnownKeys.CascadeChord "ctrl+x"
chord.TargetKind                        // ChordRoot
BindingDescriptor.commandId chord       // None
```

## Problem

- Keybinding tables drift from the commands they invoke:
  - rows are loose key/value pairs; nothing says whether the value is a command id, a chord root, or a UI opener
  - every consumer re-decides which keys are special, so chord roots get bound like plain commands
- Gesture spellings arrive as raw strings with no place to carry a normalized form.

## Solution

- One descriptor row names the target kind explicitly; the one special key (`cascade_chord`) is upgraded at construction, not per consumer.
- The wire string travels with an optional normalized gesture, so normalization happens once.
- Battle-tested as the binding spine of the Guiders invocation mechanics — [ADR-0015](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0015-invocation-mechanics-slash-melody-binding.md).

## When not to use

- Gesture normalization/parsing — that is `Notations.Keyboard` (this package only carries the wire and the optional normalized result).
- Chord trees and melody lines — `Gdl.Command.Melody` describes sequential play lines.
- Binding file parsing — this is the row IR; TOML/JSON readers live with the catalogs.

## Learn more

- [ADR-0015 — invocation mechanics: slash, melody, binding](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0015-invocation-mechanics-slash-melody-binding.md)
- [ADR-0016 — input notation quarry family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0016-input-notation-quarry-family.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
