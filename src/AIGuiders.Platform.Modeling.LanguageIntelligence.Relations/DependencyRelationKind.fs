namespace AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// <summary>E_dep substrate edges — closed kernel (plan §2.3).</summary>
type DependencyRelationKind =
    | Uses
    | TypeUses
    | Binds
    | Imports
    | SyntaxDepends

module DependencyRelationKind =
    let toWire =
        function
        | Uses -> "uses"
        | TypeUses -> "type-uses"
        | Binds -> "binds"
        | Imports -> "imports"
        | SyntaxDepends -> "syntax-depends"

    let tryParse (wire: string) =
        if System.String.IsNullOrWhiteSpace wire then
            None
        else
            match wire.Trim().ToLowerInvariant() with
            | "uses" -> Some Uses
            | "type-uses" | "typeuses" -> Some TypeUses
            | "binds" -> Some Binds
            | "imports" -> Some Imports
            | "syntax-depends" | "syntaxdepends" -> Some SyntaxDepends
            | _ -> None
