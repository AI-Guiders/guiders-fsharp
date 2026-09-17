namespace AIGuiders.Platform.Modeling.Notations.Bracket

open System
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>Wire-side XML element target until TreeNode graph index (ADR-0063 §10.4).</summary>
module XmlWireEncoding =
    let [<Literal>] Marker = "__xml__"

    let elementTarget (file: LogicalPath) (elementPath: string) (attr: string option) (upsertRole: string option) : CodeTarget =
        let container =
            [ Marker
              match attr with
              | None -> ""
              | Some a -> a
              match upsertRole with
              | None -> ""
              | Some r -> r ]

        CodeTarget.Symbol(
            DocumentRef.File file,
            { Container = container
              Name = elementPath
              Arity = None }
        )

/// <summary>Transitional RelationSpec → legacy span for language resolvers (plan §8 migration).</summary>
module RelationSpecLegacyBridge =
    let private optNonEmpty (s: string) =
        if String.IsNullOrWhiteSpace s then None else Some (s.Trim())

    let tryToLegacySpan (spec: RelationSpec) : BracketAnchorSpan option =
        match spec with
        | RelationSpec.CodeEdit (CodeTarget.Symbol (DocumentRef.File path, symbol)) when not path.IsEmpty ->
            match symbol.Container with
            | XmlWireEncoding.Marker :: attr :: role :: _ ->
                Some
                    { BracketAnchorSpan.empty with
                        File = Some path.Value
                        XmlPath = Some symbol.Name
                        Attr = (optNonEmpty attr)
                        Role = (optNonEmpty role) }
            | _ ->
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
