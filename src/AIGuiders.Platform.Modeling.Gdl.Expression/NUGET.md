# AIGuiders.Platform.Modeling.Gdl.Expression

A deliberately small expression AST — literals, fact references, comparisons, and boolean operators — for guarded conditions in declarative files.

## What you get

- `ExprNode` — the neutral condition tree:
  - `LiteralBool` / `LiteralString` / `LiteralInt`
  - `FactRef` of string — reference to a named fact
  - `Compare` of `ExprNode * string * ExprNode` — left, string operator, right
  - `And` / `Or` / `Not`
- `ExprSpan` — source position (`Line`, `Column`); `[<CLIMutable>]` for C# construction
- `ParsedExpr` — node plus span, also `[<CLIMutable>]`
- Zero package dependencies — a pure type definition assembly

## Quick start

```fsharp
open AIGuiders.Platform.Modeling.Gdl.Expression

// "build.failed and not review.open"
let condition : ExprNode =
    ExprNode.And(
        ExprNode.FactRef "build.failed",
        ExprNode.Not(ExprNode.FactRef "review.open"))

let parsed : ParsedExpr =
    { Node = condition
      Span = { Line = 3; Column = 7 } }

match parsed.Node with
| ExprNode.And _ -> printfn "guarded rule"
| _ -> ()
```

## Problem

- Declarative files grow guarded conditions: `when` clauses in rule files, visibility conditions in layout decks, preconditions in command catalogs.
- Each grammar then either invents its own condition representation or embeds a general-purpose expression engine with far more power (and attack surface) than the grammar needs — and C# and F# consumers cannot share one tree.

## Solution

- One deliberately tiny tree covers the whole condition language: three literal kinds, fact refs, a single comparison node, and boolean operators — deterministic, side-effect free, trivially walkable.
- `[<CLIMutable>]` records make the tree friendly to C# consumers; spans keep diagnostics anchored to the source file.
- This is the shared expression IR of the GDL family — the expressiveness ceiling for guarded rules is specified in [GUIDERS-ADR-0057](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md), and its F# extraction status is tracked in [FSHARP-ADR-0003](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md). In this repo the cockpit-logic graph parser (`Modeling.Gdl.Parse.CockpitLogic`) produces it and `Modeling.Cockpit.Rules` evaluates it.

## When not to use

- No parser and no evaluator here — this package is only the AST; parsing lives in the `Gdl.Parse.*` packages and evaluation belongs to the consumer.
- Deliberately no arithmetic, string operations, function calls, or lambdas — if your conditions need those, this ceiling is the wrong tool.
- Comparison operators are unvalidated strings (`">="`, `"="`, …); operator semantics are a contract between your parser and evaluator, not this package.

## Learn more

- [GUIDERS-ADR-0057 — Cockpit logic quarry (expression ceiling)](https://github.com/AI-Guiders/guiders-dotnet-platform/blob/main/docs/adr/GUIDERS-ADR-0057-cockpit-logic-authoring-quarry.md)
- [FSHARP-ADR-0003 — Model extraction matrix](https://github.com/AI-Guiders/guiders-fsharp/blob/main/docs/adr/GUIDERS-FSHARP-ADR-0003-model-extraction-matrix.md)

---

MIT · [Source & issues](https://github.com/AI-Guiders/guiders-fsharp) · part of the Guiders federation
