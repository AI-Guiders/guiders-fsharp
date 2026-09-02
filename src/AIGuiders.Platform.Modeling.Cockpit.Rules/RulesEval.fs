namespace AIGuiders.Platform.Modeling.Cockpit.Rules

open AIGuiders.Platform.Modeling.Gdl.Expression
open AIGuiders.Platform.Modeling.Gdl.Parse.CockpitLogic

[<CLIMutable>]
type RuleMatchTrace = { RuleId: string; Matched: bool }

[<RequireQualifiedAccess>]
module RulesEval =
    let rec evalExpr (facts: Map<string, obj>) (node: ExprNode) =
        match node with
        | LiteralBool b -> b
        | LiteralString s -> not (System.String.IsNullOrEmpty s)
        | LiteralInt _ -> true
        | FactRef name ->
            match Map.tryFind name facts with
            | Some value when value <> null -> true
            | _ -> false
        | Compare(l, _, r) -> evalExpr facts l = evalExpr facts r
        | And(l, r) -> evalExpr facts l && evalExpr facts r
        | Or(l, r) -> evalExpr facts l || evalExpr facts r
        | Not inner -> not (evalExpr facts inner)

    let evaluate (graph: CockpitRuleGraph) (facts: Map<string, obj>) =
        graph.Rules
        |> List.map (fun rule ->
            { RuleId = rule.Id
              Matched = evalExpr facts rule.When })
