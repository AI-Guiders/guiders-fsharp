# AIGuiders.Platform.Modeling.Cockpit.Cds

Cockpit Display System decision shapes: pure attention routing and desk-detail resolution as data in, decision out.

## What you get

- `AttentionRoutingInput` / `AttentionRoutingDecision` — normalize explicit MFD + go-verb + seats-mode into a decided MFD, go-verb, nav-forced flag
- `DeskDetailDecision` — desk_detail / nav_detail resolution (`slim` / `nav` / `full`)
- `DeskGoMapEntry` — go-verb catalog entry: organ tool + default args
- `CdsRouting.route` — the pure routing circuit:
  - explicit MFD wins; unknown pages fall back to `nav`
  - go-verb that is an MFD page routes to that page
  - layout/tile verbs are consumed before dispatch (nulled out)
  - `sys` / `chk` / `ecl` / `gates` promote to go-verb when nothing else claims them
- `CdsRouting.resolveDeskDetail` — `slim` / `nav` / `full` with focus-driven promotion

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Cockpit.Cds

// seats mode with a go-verb that is itself an MFD page
let decision =
    CdsRouting.route
        { MfdExplicit = null
          GoVerb = "QRH"
          SeatsMode = true
          DefaultMfd = null }

// decision = { Mfd = "qrh"; GoVerb = ""; DeskDetailNavForced = false }
```

## Problem

- Every UI shell that dispatches user attention to channels keeps re-answering the same questions in controller code:
  - which page is the user actually on when explicit selection, a go-verb, and a mode default collide
  - which verbs are channel routes and which are layout toggles
  - what desk detail level to show for a given focus
- Hand-rolled, this lives as nested if-chains per shell.

## Solution

- The routing circuit is pure data-in → decision-out: inputs and decisions are records, the circuit has no UI dependency.
- Battle-tested as the F# mirror of the CDP `AttentionRoutingUnit` parity ([ADR-0036](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0036-invocation-engage-glossary.md) · [CIDE ADR-0097](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)).

## When not to use

- You need channel transport (threading, queues, back-pressure) — this package decides routes only; transport lives in the DataBus family.
- You want human-facing MFD wording — this is the decision layer, presentation naming lives upstream.

## Learn more

- [ADR-0036 — invocation engage glossary](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0036-invocation-engage-glossary.md)
- [CIDE ADR-0097 — cockpit compute units → channel DTOs](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
