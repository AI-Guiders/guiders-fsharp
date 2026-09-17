namespace AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// <summary>C# / Roslyn semantic substrate edges — disambiguated from correspondence ImplementsObligation.</summary>
type TypeSystemRelationKind =
    | ImplementsInterface
    | Extends
    | Instantiates

module TypeSystemRelationKind =
    let toWire =
        function
        | ImplementsInterface -> "implements-interface"
        | Extends -> "extends"
        | Instantiates -> "instantiates"

    let tryParse (wire: string) =
        if System.String.IsNullOrWhiteSpace wire then
            None
        else
            match wire.Trim().ToLowerInvariant() with
            | "implements-interface" | "implementsinterface" -> Some ImplementsInterface
            | "extends" -> Some Extends
            | "instantiates" -> Some Instantiates
            | _ -> None
