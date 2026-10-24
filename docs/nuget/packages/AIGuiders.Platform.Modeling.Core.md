package: AIGuiders.Platform.Modeling.Core
tagline: Cross-tool spine pieces — typed entity identity carriers, the routed-intent / execution-outcome envelope shapes, and pulse-line truncation rules.

## What you get

- Identity carriers (`Core.Identity`):
  - `CarrierWire` — raw ingress shapes: `Numeric` (bigint), `Uuid`, `Text`, `Path`
  - single-case carriers `NumericId` / `GuidId` / `Sha256`
  - `Identity<'entity, 'carrier>` — phantom-typed id; ready-made `DocId`, `DiagnosticRef`, `NodeId`, `ArtifactRef`, `CommitRef`
  - `ProjectId` — case-insensitive logical-path wrapper (`create`, `path`, `fromAbsolute`); `GitPin`
  - boundary parsers: `CarrierWire.parseNumeric / parseUuid / parsePath`, `ParseCommit.parse` (64-hex sha256 → `CommitRef`)
- `IntentOutcome` — host-execute result record: `Raw`, `Verb`, `Ok`, `Action`, `Seat`, `Go`, `Path`, `DocId`, `Cmd`, `Pulse`, `Reason`, `Ship`
- `RoutedIntent` — parsed intent before host execute: `Verb`, `Go`, `Organ`, `Path`, `Detail`, `Scene`, `Cmd`, `OldString` / `NewString`, `Op`, `Reason`
- `PulseFormat` — one-line pulse rules: `truncate`, `joinBits`, `DefaultMaxChars = 240`, `InventoryObserveMaxChars = 480`

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Core
open AIGuiders.Platform.Modeling.Core.Identity

// Typed ids instead of raw ints/strings crossing boundaries
let doc = DocId.mint (NumericId.ofCounter 42L)
let node = NodeId.mint (NumericId.ofCounter 7L)

// Wire ingress, validated
let commit = ParseCommit.parse (new string('a', 64))  // Ok CommitRef (sha256)
let bad = ParseCommit.parse "abc123"                  // Error "commit hash must be 64 hex characters (sha256)"

// Pulse lines, budgeted
let pulse = PulseFormat.joinBits [ "3 projects"; "index warm" ] PulseFormat.DefaultMaxChars
```

## Problem

- Pipelines pass entity references (documents, syntax nodes, commits) as raw strings and ints — nothing stops a document counter landing in a node slot.
- Agent and intent plumbing re-declares "what did we route" and "what did the host execute" record shapes per product.
- Status lines get truncated differently in every tool.

## Solution

- Phantom-typed `Identity` over narrow carriers: ids stop being interchangeable, and parse boundaries stay explicit (`CarrierWire`, `ParseCommit`).
- Two neutral envelopes (`RoutedIntent`, `IntentOutcome`) shared by routing and execution across products, plus pulse formatting with named budgets.
- Spine of the Guiders federation — identity carriers per [ADR-0063](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md); envelope placement per [ADR-0042](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0042-intermediate-representation-family.md) · [ADR-0020](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0020-mcplane-agent-ingress.md).

## When not to use

- Rich identifiers (ULIDs, opaque tokens, human-readable keys) — only int64 / Guid / sha256 / logical-path carriers ship; bigint wire values outside int64 are rejected.
- Serialization or persistence — the envelopes are `[<CLIMutable>]` data only, no JSON/text converters here.
- Short git hashes — `ParseCommit` accepts full 64-hex sha256 only; sha1 and short forms are errors, not resolved.

## Learn more

- [ADR-0042 — Intermediate representation family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0042-intermediate-representation-family.md)
- [ADR-0020 — MCPlane agent ingress](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0020-mcplane-agent-ingress.md)
- [ADR-0063 — Anchors federation reincarnation](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0063-anchors-federation-reincarnation.md)
