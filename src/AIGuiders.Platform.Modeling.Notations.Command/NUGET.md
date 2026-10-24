# AIGuiders.Platform.Modeling.Notations.Command

The command-line notation family: slash parsing, canonical command paths, typed invocation surfaces, and a four-stage pipeline from raw text to one resolved invocation.

## What you get

- `SlashCommandNotation` — slash-line notation: `tryParseLine` (leading `/` required) and `parseBody` producing `SlashWireBody` (path `Tokens` + `EndsWithSpaceAfterTokens` continuation hint, `JoinedTokens`)
- `InvocationNotation` — canonical command path: `fromPathSegments` (blank segments dropped, single-space join) and case-insensitive `pathsEqual`, over the `NormalizedCommandLine` record
- `InvocationSurfaceId` — the typed surface vocabulary with `toWire`/`tryParse`:
  - `SlashBar` (`slash.bar`), `SlashComposer`, `ConsoleFilter`, `Palette`, `EditorCcl` (`editor-ccl`), `EditorInline`, `CommandBar`, `BindingHotkey`, `MelodyChord`, `McpTool`, `CitizenIntent`, `Custom`
- `InvocationSurfaceSpec` — per-surface contract: `Engage` + `Wire` kind + `RequiresSurfaceProjection` + `Description`; `builtin`/`allBuiltins` for the eleven built-ins
- `InvocationEngageKind` — how invocation started: `Slash`, `Melody`, `Binding`
- `InvocationWireKind` — which parser branch owns the wire: `SlashPath`, `ConsolePath`, `KeySequence`, `None`
- `InvocationStage` — the projection pipeline `Raw → SurfaceProjected → WireParsed → Resolved`, with `advance*` helpers; `CanonicalInvocation` (`CommandId` + `Path` + `Args`) as the resolved end state
- `InvocationLinePhase` — where the user is on the line: `Path`, `Arg`, `Ready`; `ArgMechanic` — arg-tail interaction: `Picker`, `FreeText`, `Optional`, `Constructor`, `TypedInput`
- `SurfaceRegistry` — GDL catalog strings to typed specs: `tryParseId`, `tryParseList`, `specFor`, `validateGdlSurfaces`, plus the federation default surface set
- `AttachInvocation` — the attach command contract: root `federation.attach`, verbs (`error`, `issue`, `document`, `code`, `nav`, `manual`), `tryParsePath`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Notations.Command
open AIGuiders.Platform.Modeling.Notations.Command.Slash

// 1) Tokenize what the user typed on a slash surface
let body = SlashCommandNotation.tryParseLine "/buffer open doc=README.md"
// Some { Tokens = ["buffer"; "open"; "doc=README.md"]; EndsWithSpaceAfterTokens = false }

// 2) Normalize the command path (blank segments dropped, single-space join)
let path = InvocationNotation.fromPathSegments body.Value.Tokens

// 3) One command, many surfaces: get the typed surface contract
let spec = SurfaceRegistry.specFor InvocationSurfaceId.SlashBar
// Ok { Engage = Slash; Wire = SlashPath; RequiresSurfaceProjection = false; … }
```

## Problem

- The same command is invokable from many front-ends — slash bar, palette, console filter, editor line, hotkey, MCP tool — and each one hand-rolls its own line parsing, its own path canonicalization (case, blank segments), and its own list of surfaces as untyped strings.
- Completion UIs also need to know where the user is — still typing the command path, mid-argument, or ready to run — and every surface answers that differently.

## Solution

- One typed pipeline (`Raw → SurfaceProjected → WireParsed → Resolved`) ending in a `CanonicalInvocation`, plus a registry of surface specs so "which surfaces can run this command" is data, not prose.
- The command branch of the Guiders Notations family, running the federation's slash bars, palettes, and command catalogs — [ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md) (§13.1 stages), surfaces per [ADR-0009](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0009-command-surface-pattern.md), engage kinds per [ADR-0015](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0015-invocation-mechanics-slash-melody-binding.md), line phases per [ADR-0043](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0043-invocation-line-phase.md).

## When not to use

- No catalog resolution or execution here: the pipeline ships pure stage labels and advance helpers; matching a path against a command catalog (longest-prefix) and running anything live in the command-plane side.
- No argument-tail parsing: `Args` travels as `NormalizedArguments` from the sibling Argument package — kv/cli/positional readers are not in this package.
- Not a keybinding parser: `BindingHotkey`/`MelodyChord` surfaces carry key-sequence wire, but keyboard normalization itself lives in Notations.Keyboard.

## Learn more

- [ADR-0021 — Notations quarry family (Command branch)](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)
- [ADR-0009 — Command–Surface pattern](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0009-command-surface-pattern.md)
- [ADR-0015 — Invocation mechanics (Slash · Melody · Binding)](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0015-invocation-mechanics-slash-melody-binding.md)
- [ADR-0043 — Invocation line phase and arg mechanics](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0043-invocation-line-phase.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
