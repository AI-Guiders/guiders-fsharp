namespace AIGuiders.Platform.Modeling.Documentation.Correspondence

/// <summary>Typed correspondence relation kinds — wire SSOT via <see cref="CorrespondenceRelationKindModule.toWire"/>.</summary>
type CorrespondenceRelationKind =
    | Documents
    | ImplementsObligation
    | Related
    | Constrains
    | Normates
    | VerifiedBy

module CorrespondenceRelationKind =
    let toWire =
        function
        | Documents -> Kind.Documents
        | ImplementsObligation -> Kind.ImplementsObligation
        | Related -> Kind.Related
        | Constrains -> Kind.Constrains
        | Normates -> Kind.Normates
        | VerifiedBy -> Kind.VerifiedBy

    let tryParse (wire: string) =
        if System.String.IsNullOrWhiteSpace wire then
            None
        else
            match wire.Trim().ToLowerInvariant() with
            | Kind.Documents -> Some Documents
            | Kind.ImplementsObligation -> Some ImplementsObligation
            | Kind.Related -> Some Related
            | Kind.Constrains -> Some Constrains
            | Kind.Normates -> Some Normates
            | Kind.VerifiedBy -> Some VerifiedBy
            | _ -> None
