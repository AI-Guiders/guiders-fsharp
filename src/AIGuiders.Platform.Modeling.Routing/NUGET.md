# AIGuiders.Platform.Modeling.Routing

Route refusal algebra — two pure helpers that turn a failed route into an honest refusal outcome.

## What you get

- `RouteRefusal.refuse` — route a refusal through your own factory: `refuse factory raw reason go` is `factory raw reason go`, made explicit at the refusal site
- `RouteRefusal.outcomeNotOk` — map a failed `RoutedIntent` (from `AIGuiders.Platform.Modeling.Core`) into an `IntentOutcome`:
  - `Ok = false`, route identity fields carried over (`Raw`, `Verb`, `Go`, `Cmd`)
  - `Pulse` defaults to the route's `Reason` when no override is passed
  - `Reason` defaults to `"route_not_ok"` when the route carries none
  - unset outcome fields (`Action`, `Seat`, `Path`, `DocId`, `Ship`) are left default

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Core
open AIGuiders.Platform.Modeling.Routing

let route : RoutedIntent =
    { Verb = "Buffer"; Raw = "read path=foo"; Ok = false
      Go = "buffer"; Organ = ""; Path = ""; Detail = ""
      Scene = ""; Cmd = ""; OldString = ""; NewString = ""
      Op = ""; Reason = "no buffer open" }

let refused : IntentOutcome = RouteRefusal.outcomeNotOk route None
// refused.Ok = false; refused.Reason = "no buffer open"; refused.Pulse = "no buffer open"
```

## Problem

- Every command router ends the same way: the input parsed to a route, the route refused (no target, bad args, seat busy) — and now someone must turn that failed route into an outcome the host can show:
  - which route fields survive into the refusal
  - what the pulse line says when nobody set one
  - what the reason says when there was none
- Hand-rolled per host, this copying drifts: one host forgets the `Reason` default, another invents a different pulse rule.

## Solution

- Refusal as named algebra instead of per-host field copying: one function owns the route-to-outcome mapping, defaults included.
- The F# half of the intent-organ seam (parse → `Route`, route + override → `Execute` lives in the C# `IIntentOrgan` host, [platform ADR-0010](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0010-platform-mechanics.md)); consumed cross-language by the platform smoke tests, extraction mapped in [FSHARP ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md).

## When not to use

- Routing itself — this package neither parses raw input nor picks an organ; you bring your own `Route`/`Execute` (the `IIntentOrgan` seam is the C# Execution layer).
- It is deliberately tiny: two functions over Core shapes, nothing else — if you need dispatch, retries or queuing, that is not here and is not planned here.

## Learn more

- [FSHARP ADR-0003 — Model extraction matrix (Modeling.Routing: refusal algebra)](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)
- [ADR-0010 — Platform mechanics (intent organ seam)](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0010-platform-mechanics.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
