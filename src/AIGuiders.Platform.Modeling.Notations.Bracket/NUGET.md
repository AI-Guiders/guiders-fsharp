# AIGuiders.Platform.Modeling.Notations.Bracket

Bracket reference IR: parameterized profiles for paired-delimiter notations, axis value plans, and bridges that turn a bracket wire into a code-edit, navigation, or diagnostic target.

## What you get

- `BracketNotationProfile` — the grammar parameters as a record: `StartTerminal`/`EndTerminal`, `ListSeparator`, `KvSign`, `AxisShape`, `StripOuterTerminals`, `RespectBracketDepthOnListSplit`, `NestedAxisKeys`
- `BracketAxisShape` — how the inner text tokenizes:
  - `KeyValue` — split into `Key:Value` axes
  - `Opaque` — keep the inner text whole
- `NormalizedBracketWire` + `BracketAxis` — the normalized result: `ProfileId`, axes (`Key`, `Sign`, `Value`, `ValueWireClass`, optional `Nested` wire), and the `Raw` source
- `BracketProfiles` — built-in profiles: `CdpSquareKeyValue` (`[k:v; …]`, nested `Anchor`), `AngleOpaque` (`<…>`), `DocSymbol`, `ForgeFrg` (compound path + kv tail)
- `BracketAxisValuePlans` — what each axis value means per profile (`CdpCode`, `ForgeFrgCompound`, `DocSymbol`), over `BracketAxisValueClasses`: `Opaque`, `CommandPath`, `Kv`, `LineRange`, `NestedBracket`
- `BracketAxisValuePlan` / `BracketAxisAliasMap` — axis-key→value-class tables and alias maps for your own profiles
- `CodeEditWireEncoding` — line/scope/type/text hints (`@line:`, `@scope:`, `@type:`, `@text:`) appended to and decoded from symbol containers
- `XmlWireEncoding` — XML element targets packed into symbol containers via the `__xml__` marker
- `BracketRelationWire.tryParseRelationSpec` — map a normalized wire by its `Kind` axis to a `RelationSpec`: `CodeEdit` (member, element, or scope/line/text targets), `Diag` (numeric diagnostic id), `Nav` (file/line/column/command/member)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Notations.Bracket

// A profile-driven bracket reference, e.g. a code anchor in chat text
let profile = BracketProfiles.CdpSquareKeyValue  // [Key:value; …] with nested Anchor axes

// What each axis value means for this profile (short + long aliases)
let plan = BracketAxisValuePlans.CdpCode         // F/File → command.path, L/Line → line.range, …

// The normalized wire your parser produces and your resolver consumes
let wire : NormalizedBracketWire =
    { ProfileId = profile.Id
      Axes = [| BracketAxis("F", ':', "src/App.fs", BracketAxisValueClasses.CommandPath) |]
      Raw = "[F:src/App.fs]" }
```

## Problem

- Bracket-delimited references — `[F:src/App.fs; L:10-20]` style anchors in chats, issues, CI logs — look simple but every tool invents its own grammar: square or angle terminals, `:` or `=` signs, `;` or `,` separators, nesting or none.
- Hard-coded parsers per tool mean an anchor written for one surface cannot be understood by another, and "what does `L:` mean" is buried in code.

## Solution

- Profiles parameterize the grammar instead of hard-coding it; value plans declare axis semantics in one reviewable table; wire encodings bridge the normalized axes to concrete code-edit, navigation, and diagnostic targets.
- Shipped as the bracket branch of the Guiders Notations family — editor anchors and Forge compound references run on it — [ADR-0026](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0026-notations-bracket-branch.md), family contract in [ADR-0021](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md).

## When not to use

- Not a text parser: bracket text into `NormalizedBracketWire` is done by profile-driven parsers on the platform side; this package ships the IR, built-in profiles, and downstream encodings.
- The relation bridge drags in three sibling packages (`Modeling.Core`, `Modeling.Paths`, `Modeling.LanguageIntelligence.Relations`); if you only need bracket-aware splitting, `Notations.Core` is the lighter cut.
- `XmlWireEncoding` and the `@line:`/`@scope:` hints are explicitly interim wire encodings (until a graph index replaces them) — treat them as a transport detail, not a stable public grammar.

## Learn more

- [ADR-0026 — Notations.Bracket branch](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0026-notations-bracket-branch.md)
- [ADR-0021 — Notations quarry family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0021-notations-quarry-family.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
