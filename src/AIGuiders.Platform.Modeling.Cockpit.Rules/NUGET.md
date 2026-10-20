# AIGuiders.Platform.Modeling.Cockpit.Rules

Headless cockpit rule evaluation: a rule graph with boolean expressions over facts, evaluated with a per-rule match trace.

## What you get

- `RulesEval.evaluate` — run every rule of a `CockpitRuleGraph` against a facts map; returns one `RuleMatchTrace` per rule (`RuleId` + `Matched`)
- `RulesEval.evalExpr` — evaluate a single boolean expression node:
  - literals (bool / non-empty string / int)
  - `FactRef` — fact present and non-null means true
  - `Compare` / `And` / `Or` / `Not`
- Expression nodes come from `AIGuiders.Platform.Modeling.Gdl.Parse.CockpitLogic` (the rule-graph quarry)

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Cockpit.Rules

// rules parsed from cockpit rule GDL (Gdl.Parse.CockpitLogic)
let traces = RulesEval.evaluate graph (Map [ "nightMode", box true ])

// traces = [ { RuleId = "dim-at-night"; Matched = true }; ... ]
```

## Problem

- Cockpit annunciator rules ("which lamp lights given which facts") reappear per surface; hand-rolled evaluation means the same if-trees in every host, with no trace of which rule matched.

## Solution

- Rules stay data (a parsed graph), evaluation stays pure, and every run returns a per-rule trace — explainable annunciation instead of scattered boolean logic.
- Battle-tested as the headless evaluator behind the cockpit rule quarry ([ADR-0057](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md)).

## When not to use

- You need rich typed values and arithmetic in conditions — expressions here are boolean/presence/compare over `obj` facts; heavier evaluation needs a real rules engine.
- You need rule authoring/parsing — take `Gdl.Parse.CockpitLogic` for the quarry; this package only evaluates the parsed graph.

## Learn more

- [ADR-0057 — cockpit logic authoring quarry](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
