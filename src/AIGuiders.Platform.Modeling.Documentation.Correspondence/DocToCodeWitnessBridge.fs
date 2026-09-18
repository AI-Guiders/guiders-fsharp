namespace AIGuiders.Platform.Modeling.Documentation.Correspondence

open System
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations
open AIGuiders.Platform.Modeling.Paths

/// <summary>CRS doc→code witness → federation <see cref="RelationSpec.DocToCode"/> (plan §2.5).</summary>
module DocToCodeWitnessBridge =
    let tryToRelationSpec (witness: DocToCodeWitness) : RelationSpec option =
        if String.IsNullOrWhiteSpace witness.File then
            None
        else
            let docPath = LogicalPath.Create witness.DocPath
            let codePath = LogicalPath.Create witness.File

            let fragmentLabel =
                match witness.Excerpt with
                | Some excerpt when not (String.IsNullOrWhiteSpace excerpt) -> excerpt
                | _ -> witness.DocTitle

            let source = DocumentPlace.Fragment(docPath, fragmentLabel)

            match witness.MemberKey with
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
