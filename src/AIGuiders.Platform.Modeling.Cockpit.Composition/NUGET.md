# AIGuiders.Platform.Modeling.Cockpit.Composition

Surface compositor scene inputs: what the desk seats/tiles surface assembles from — opaque JSON rows with explicit presence.

## What you get

- `SeatsSurfaceScene` — seats-mode surface inputs: schema version, MFD, view, seats, session, instrument, alert, pressure, next, focus, go, warm, pins, layouts, loci, go-verbs
- `TilesSurfaceScene` — legacy tiles-mode inputs (prefer seats)
- `SeatsSurfacePayload` — seat count badge payload
- `OptionalRow` / `Rows.absent` — explicit row presence: struct `JsonElement` has no null, so absence is a wrapper DU (`Present` / `Absent`), not null

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Cockpit.Composition

let scene =
    { SchemaVersion = "v1"
      Mfd = "nav"
      View = viewJson
      Seats = seatsJson
      Session = sessionJson
      Instrument = Rows.absent
      Alert = Rows.absent
      Pressure = Rows.absent
      Next = nextJson
      Focus = Rows.absent
      Go = Rows.absent
      Warm = Rows.absent
      Pins = []
      Layouts = [ "seats" ]
      ThrashNote = null
      Loci = Rows.absent
      GoVerbs = [] }
```

## Problem

- A desk surface is assembled from many projected inputs; some rows are present this frame, some are not.
- Hand-rolled, presence rots into null-checks scattered over compositor code — and struct `JsonElement` cannot even carry null.

## Solution

- One scene record names every input slot; presence is explicit (`OptionalRow`) instead of implied by null.
- Battle-tested as the compositor input contract of the Cascade IDE cockpit ([ADR-0036](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0036-invocation-engage-glossary.md) · [CIDE ADR-0097](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)).

## When not to use

- You want typed payloads instead of opaque rows — scenes carry wire rows; typed projections live in the DataBus family.
- You are not on seats mode and have no migration plan — `TilesSurfaceScene` is legacy input kept for transition.

## Learn more

- [ADR-0036 — invocation engage glossary](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0036-invocation-engage-glossary.md)
- [CIDE ADR-0097 — cockpit compute units → channel DTOs](https://github.com/AI-Guiders/cascade-ide/blob/main/docs/adr/0097-cockpit-compute-units-transport-to-channel-dto.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
