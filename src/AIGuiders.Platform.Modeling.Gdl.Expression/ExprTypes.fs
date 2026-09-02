namespace AIGuiders.Platform.Modeling.Gdl.Expression

/// <summary>Neutral expression AST for guarded rules (.cockpit.logic, .deck when, catalog preconditions).</summary>
type ExprNode =
    | LiteralBool of bool
    | LiteralString of string
    | LiteralInt of int
    | FactRef of string
    | Compare of ExprNode * string * ExprNode
    | And of ExprNode * ExprNode
    | Or of ExprNode * ExprNode
    | Not of ExprNode

[<CLIMutable>]
type ExprSpan = { Line: int; Column: int }

[<CLIMutable>]
type ParsedExpr = { Node: ExprNode; Span: ExprSpan }
