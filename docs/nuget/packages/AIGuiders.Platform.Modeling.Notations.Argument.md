package: AIGuiders.Platform.Modeling.Notations.Argument
tagline: A notation-neutral schema for command argument tails: slot definitions, reader ids, and one normalized result type that every argument syntax lands in.

## What you get

- `ArgumentSlot` — one accepted slot: `Name`, `Kind`, `LongOption`, `ShortOption`
- `ArgumentSlotKind` — what a slot carries:
  - `Flag` — present/absent switch
  - `Value` — named value
  - `Positional` — position-significant token
- `ArgumentNotationProfile` — a command's declared tail: `ReaderId` + `Slots`, with `IsEmpty` and a static `Merge` (non-empty incoming fields win, empty incoming keeps existing)
- `ArgumentReaders` — the canonical reader id literals:
  - `Kv` = `"kv"`, `Cli` = `"cli"`, `Positional` = `"positional"`, `Delimited` = `"delimited"`, `Colon` = `"colon"`, `Raw` = `"raw"`
- `NormalizedArguments` — the one normalized result: `Raw` text, `Slots` dictionary, `ReaderId`; constructors `FromRaw` / `FromSlots`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Notations.Argument

// Declare the argument tail a command accepts — syntax-neutral
let profile =
    ArgumentNotationProfile(
        ArgumentReaders.Kv,
        [| ArgumentSlot("file", ArgumentSlotKind.Positional)
           ArgumentSlot("force", ArgumentSlotKind.Flag) |])

// Whatever syntax the user typed, your reader normalizes it to one shape
let args = NormalizedArguments.FromRaw("README.md --force", ArgumentReaders.Cli)
```

## Problem

- Command argument tails come in many dialects: space-separated tokens, `key=value` pairs, bare flags, positional arguments — and every plugin or UI hand-rolls its own stringly-typed parsing of them.
- Consumers (completion lists, validation, docs) have no shared way to ask "what arguments does this command accept, and what did the user actually pass?"

## Solution

- One neutral slot schema to declare what a command accepts, and one normalized result type to carry what the user passed — many syntaxes, one IR, reader ids to tag which syntax produced it.
- The argument branch of the Guiders Notations family, used across its command surfaces — [ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md).

## When not to use

- No parsing here: the package is schema + result IR only — turning `key=value` or CLI text into `NormalizedArguments` is your reader's job (the platform ships profile-driven readers separately).
- `ArgumentNotationProfile.Merge` is coarse: a non-empty incoming slot list replaces the whole list; slots are never merged one by one.
- C#-wire interop style: mutable, null-tolerant classes — not immutable F# records, and nothing validates slot names or option spellings.

## Learn more

- [ADR-0021 — Notations quarry family (Argument branch)](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)
