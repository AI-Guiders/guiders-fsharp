# AIGuiders.Platform.Modeling.Gdl.Parse.CockpitLogic

Typed rule-graph IR for cockpit annunciation logic: rules with boolean expressions over facts plus projectors — data contracts only, no parser and no evaluator inside.

## What you get

- `CockpitRuleGraph` — the top-level record: `Planet` (owning scope), `Rules`, `Projectors`
- `CockpitRule` — one annunciation rule:
  - `Id` — stable rule identifier
  - `When` — a boolean `ExprNode` over facts (from `AIGuiders.Platform.Modeling.Gdl.Expression`):
    - `LiteralBool` / `LiteralString` / `LiteralInt`
    - `FactRef` of a fact name
    - `Compare` of two nodes, `And` / `Or` / `Not`
  - `Emit` — what to annunciate when the rule matches
  - `Severity` — free-form severity string
- `CockpitProjector` — fact projection wiring: `Id`, `Source`, `Target` (which fact feeds which zone/consumer)
- All records are `[<CLIMutable>]` — friendly for C# consumers and serializers

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Expression
open AIGuiders.Platform.Modeling.Gdl.Parse.CockpitLogic

let graph : CockpitRuleGraph =
    { Planet = "studio"
      Rules =
        [ { Id = "dim-at-night"
            When = And(FactRef "nightMode", Not(FactRef "presentationOn"))
            Emit = "lights.dim"
            Severity = "info" } ]
      Projectors =
        [ { Id = "focus"; Source = "repl.state"; Target = "zone.repl" } ] }
```

Evaluation lives next door: `AIGuiders.Platform.Modeling.Cockpit.Rules` runs `RulesEval.evaluate graph facts` and returns a per-rule match trace.

## Problem

- "Which lamp lights given which facts" tends to end up as imperative if-trees inside each host UI — the same rules re-coded per surface, no single place to review them, and no trace of which rule fired.
- Rules as prose (manuals, wiki pages) cannot be tested; rules as code cannot be reviewed by non-programmers.

## Solution

- The rule set becomes data: a typed graph of rules over boolean fact expressions, reviewable and storable independently of any UI that renders it.
- One IR, split consumers: this package holds the graph, `AIGuiders.Platform.Modeling.Cockpit.Rules` evaluates it headlessly — matching the ownership rule that modeling owns the typed circuit graph while execution owns bus, IO, and mounting ([FSHARP ADR-0002](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md)).
- The shape follows the `.cockpit.logic` authoring grammar ([ADR-0057](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md)).

## When not to use

- You want to parse `.cockpit.logic` text files — this package ships the IR only; the text parser is deliberately not here (the quarry ADR keeps it unscheduled), so build the graph in code or map it from your own source.
- You want evaluation — take `AIGuiders.Platform.Modeling.Cockpit.Rules`; expressions here are boolean/presence/compare over `obj` facts, not a rules engine with typed values or arithmetic.
- Rich severity/action taxonomies — `Emit` and `Severity` are plain strings; any enum semantics belong to the consumer.

## Learn more

- [ADR-0057 — cockpit logic authoring quarry](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md)
- [FSHARP ADR-0002 — model guild ownership (cockpit circuit graph IR)](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0002-model-guild-fsharp-ownership.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
