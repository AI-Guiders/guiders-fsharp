namespace AIGuiders.Platform.Modeling.Notations.Bracket

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
