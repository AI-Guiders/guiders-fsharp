namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Documentation.Correspondence

/// <summary>CRS wire kinds → graph <see cref="RelationType"/> (plan §2.2 Correspondence kernel).</summary>
module CorrespondenceRelationGraph =
    let kindToRelationType =
        function
        | CorrespondenceRelationKind.Documents -> RelationType.Documents
        | CorrespondenceRelationKind.ImplementsObligation -> RelationType.ImplementsObligation
        | CorrespondenceRelationKind.Related -> RelationType.Related
        | CorrespondenceRelationKind.Constrains -> RelationType.Constrains
        | CorrespondenceRelationKind.Normates -> RelationType.Normates
        | CorrespondenceRelationKind.VerifiedBy -> RelationType.VerifiedBy

    let relationTypeWire =
        function
        | RelationType.Documents -> Kind.Documents
        | RelationType.ImplementsObligation -> Kind.ImplementsObligation
        | RelationType.Related -> Kind.Related
        | RelationType.Constrains -> Kind.Constrains
        | RelationType.Normates -> Kind.Normates
        | RelationType.VerifiedBy -> Kind.VerifiedBy
        | _ -> invalidArg "relationType" "not a correspondence RelationType"
