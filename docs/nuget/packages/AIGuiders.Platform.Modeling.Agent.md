package: AIGuiders.Platform.Modeling.Agent
tagline: The agent response envelope as a wire contract — named detail tiers, a one-line pulse, and typed next-step hints for agent clients.

## What you get

- `DetailTier` — four named response budgets:
  - `Pulse` — one line, glanceable
  - `Slim` — compact essentials
  - `Full` — complete payload
  - `Wide` — exhaustive dump
- `NextHint` — one suggested next step: `Kind` plus optional `CommandId`, `ToolName`, `Label`
- `AgentResponseEnvelope` — the response record: `Ok`, `Tier`, optional `Pulse` and `Reason`, optional `Next` hint list

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Agent

let response =
    { Ok = true
      Tier = DetailTier.Pulse
      Pulse = Some "3 projects indexed"
      Reason = None
      Next =
        Some
          [| { Kind = "navigate"
               CommandId = Some "go.definition"
               ToolName = None
               Label = Some "open symbol" } |] }
```

## Problem

- Agent tools answer the same question at different detail levels: a status poll wants one line, a drill-down wants everything.
- Without a shared envelope every tool invents its own ok/pulse/next-steps shape, and every client (chat surface, HUD, test harness) re-parses them ad hoc.

## Solution

- One neutral envelope: the detail level is data, the pulse is a single line, next steps are typed rows — clients render them without knowing the tool that produced them.
- Execution results are deliberately not here: `IntentOutcome` stays execution-side (`AIGuiders.Platform.Modeling.Core`); this package carries wire fields only.
- Deployed as the agent-envelope IR of the Guiders federation — [ADR-0042](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0042-intermediate-representation-family.md) · [ADR-0020](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0020-mcplane-agent-ingress.md).

## When not to use

- Need execution semantics (routing, effects, retries)? This is a thin wire mirror — records only, no logic.
- Need serialization or transport? No JSON/TOML converters ship; serialize the records yourself.
- Need structured payloads per tier? Fields are strings and option strings; rich payloads live elsewhere.

## Learn more

- [ADR-0042 — Intermediate representation family](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0042-intermediate-representation-family.md)
- [ADR-0020 — MCPlane agent ingress](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0020-mcplane-agent-ingress.md)
