# AIGuiders.Platform.Modeling.Gdl.Presentation

Presentation topology IR and wire parser — logical display hosts with roles and channel stacks, plus physical screen bindings for multi-monitor layouts.

## What you get

- `AttentionDisplayRole` — enum of display roles:
  - `Pfd` / `Forward` / `Mfd` — primary, forward work, and multi-function surfaces
  - `PmOneOf` — a host showing one channel at a time
  - `Eicas` — alerting annunciation
  - `Briefing` / `Hud` / `Unknown`
- `ZoneComposeKind` — `Split` (channels share the host) vs `OneOf` (one visible at a time)
- `TopologyArrangement` — `SingleSurfaceCompositional` | `SingleHostOneOf` | `MultiHost`
- `LogicalDisplayHost` / `PresentationTopology` — typed hosts (`HostIndex`, stable `HostId`, `Role`, `Compose`, `ChannelStack`, `ActiveChannel`) with `HostCount`; the original wire string is kept in `SourceWire`
- `TopologyNotation.parse` — parser for topology wires:
  - `"single"` — one compositional surface, no separate hosts
  - `"(F/M)"` — one host, channels switchable (`/` = one-of)
  - `"(MFD)(F)"` — two dedicated single-channel hosts; two- and three-host wires supported
  - `+` = split inside a host; `+` and `/` cannot mix in one group
  - roles are inferred from channel names (`f`/`editor`/`work` → Forward, `pfd`/`report` → Pfd, `mfd`/`shell`/`git`/`browser` → Mfd, `eicas`/`alert` → Eicas)
- `TopologyParseResult` — `Topology`/`Error` pair with `IsSuccess`, `Ok`, `Fail` constructors
- Physical binding, kept separate from topology: `PhysicalScreenSelectorKind` (`Primary`, `Index`, `DeviceName`, `UltrawideRegion`), `PhysicalScreenSelector` with `primary` / `byIndex` / `ultrawide` constructors, `DisplayHostBinding`, and `DisplayBindingProfile` — the runtime map from `HostIndex` to a physical screen

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Presentation

match TopologyNotation.parse "(MFD)(F)" with
| { Topology = Some topology } ->
    // Arrangement = MultiHost; hosts: mfd (role Mfd), forward (role Forward)
    for host in topology.Hosts do
        printfn "%s: role=%O channels=%A" host.HostId host.Role host.ChannelStack
| { Error = Some message } -> printfn "parse failed: %s" message
| _ -> ()

// Runtime profile: logical host 0 -> second screen, host 1 -> primary
let profile =
    { ProfileId = "operator-two-screen"
      Bindings =
        ResizeArray
            [ { HostIndex = 0; Screen = PhysicalScreenSelector.byIndex 1 }
              { HostIndex = 1; Screen = PhysicalScreenSelector.primary } ] }
```

## Problem

- Multi-monitor desktop tools conflate two questions: which logical surfaces exist (and how they compose), and which physical screen each one lands on.
- Stored as free strings or raw OS monitor indices, layout config breaks the moment a monitor is unplugged, and every consumer re-parses the same ad-hoc string in its own way.

## Solution

- A three-layer split: human-written wire (`(MFD)(F)`) → parsed logical topology (`PresentationTopology` with typed hosts) → separate runtime binding (`DisplayBindingProfile`, `HostIndex` → physical screen). Strings do not cross the parse boundary into product code.
- The IR and binding model follow [GUIDERS-ADR-0058](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md); this package is its F# mirror (extraction tracked in [FSHARP-ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)). In this repo the deck parser (`Modeling.Gdl.Parse.Deck`) uses it for `topology` lines, and the GDL spine carries it inside deck presets.

## When not to use

- Not a windowing or layout engine — no pixels, grids, or window creation; it is IR plus a wire parser, projection to actual UI is the host's job.
- Physical binding is declarative only (primary / index / device name / normalized ultrawide region); no live monitor detection, DPI, or fullscreen math.
- The wire grammar is intentionally small — one one-of host, or two to three host groups, or `single`; anything richer is rejected with an error message rather than interpreted.

## Learn more

- [GUIDERS-ADR-0058 — Presentation topology IR](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0058-presentation-topology-ir.md)
- [FSHARP-ADR-0003 — Model extraction matrix](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
