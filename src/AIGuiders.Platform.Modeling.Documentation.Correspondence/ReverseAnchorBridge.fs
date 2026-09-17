namespace AIGuiders.Platform.Modeling.Documentation.Correspondence

open System
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>Reverse CRS anchor → federation <see cref="RelationSpec.DocToCode"/> witness (plan §2.5).</summary>
module ReverseAnchorBridge =
    let tryToDocToCodeWitness (anchor: ReverseAnchor) : RelationSpec option =
        if String.IsNullOrWhiteSpace anchor.File then
            None
        else
            let docPath = LogicalPath.Create anchor.DocPath
            let codePath = LogicalPath.Create anchor.File

            let fragmentLabel =
                match anchor.Excerpt with
                | Some excerpt when not (String.IsNullOrWhiteSpace excerpt) -> excerpt
                | _ -> anchor.DocTitle

            let source = DocumentPlace.Fragment(docPath, fragmentLabel)

            match anchor.MemberKey with
            | Some memberName when not (String.IsNullOrWhiteSpace memberName) ->
                let target =
                    CodeTarget.Symbol(
                        DocumentRef.File codePath,
                        { Container = []
                          Name = memberName.Trim()
                          Arity = None }
                    )

                Some(RelationSpec.DocToCode(source, target))
            | _ -> None
