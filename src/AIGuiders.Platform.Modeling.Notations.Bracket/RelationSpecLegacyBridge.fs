namespace AIGuiders.Platform.Modeling.Notations.Bracket

open System
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>Transitional RelationSpec → legacy span for language resolvers (plan §8 migration).</summary>
module RelationSpecLegacyBridge =
    let tryToLegacySpan (spec: RelationSpec) : BracketAnchorSpan option =
        match spec with
        | RelationSpec.CodeEdit (CodeTarget.Symbol (DocumentRef.File path, symbol)) when not path.IsEmpty ->
            let scopeKind =
                match symbol.Container with
                | [] -> None
                | parts -> Some(String.Join(".", parts))

            Some
                { BracketAnchorSpan.empty with
                    File = Some path.Value
                    MemberKey = Some symbol.Name
                    ScopeKind = scopeKind }
        | _ -> None
