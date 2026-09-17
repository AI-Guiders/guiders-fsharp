namespace AIGuiders.Platform.Modeling.Ide.Session

open AIGuiders.Platform.Modeling.Core.Identity
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// Session G picker rows for attach contextual providers (plan §4.4).
module SessionGraphPickerChoices =
    type PickerChoice = { Id: string; Label: string }

    let private formatSymbol (symbol: SymbolRef) =
        match symbol.Container with
        | [] -> symbol.Name
        | parts -> (parts @ [ symbol.Name ]) |> String.concat "."

    let private documentPath (registry: DocumentRegistry) (docRef: DocumentRef) =
        match docRef with
        | DocumentRef.File path -> path.Value
        | DocumentRef.DocId id ->
            match DocumentRegistryOps.tryGetPath id registry with
            | Some path -> path.Value
            | None -> $"doc:{NumericId.value (DocId.carrier id)}"

    let private tryAddSemantic (registry: DocumentRegistry) (seen: Set<string>) (node: GraphNodeRef) (choices: PickerChoice list) =
        match node with
        | GraphNodeRef.Semantic(docRef, symbol) ->
            let id = formatSymbol symbol

            if Set.contains id seen then
                choices, seen
            else
                let path = documentPath registry docRef
                let label = $"{id} — {path}"
                { Id = id; Label = label } :: choices, Set.add id seen
        | _ -> choices, seen

    let semanticSymbolPickerChoices (runtime: SessionRuntime) : PickerChoice list =
        let mutable choices = []
        let mutable seen = Set.empty

        for relation in runtime.Session.Graph.Relations do
            let c1, s1 = tryAddSemantic runtime.Registry seen relation.From choices
            choices <- c1
            seen <- s1
            let c2, s2 = tryAddSemantic runtime.Registry seen relation.To choices
            choices <- c2
            seen <- s2

        choices |> List.sortBy (fun choice -> choice.Label)

    let relationSpecKindChoices : PickerChoice list =
        [ "CodeEdit"; "DocToCode"; "Diag"; "Address"; "Nav"; "Resource" ]
        |> List.map (fun kind -> { Id = kind; Label = kind })

    let browseAllPickerChoices (runtime: SessionRuntime) : PickerChoice list =
        let diag =
            DiagnosticIndexOps.pickerChoices runtime.Diagnostics
            |> List.map (fun choice -> { Id = $"diag:{choice.Id}"; Label = $"Diag: {choice.Label}" })

        let paths =
            DocumentRegistryOps.documentPathPickerChoices runtime.Registry
            |> List.map (fun choice -> { Id = $"file:{choice.Id}"; Label = $"File: {choice.Label}" })

        let symbols =
            semanticSymbolPickerChoices runtime
            |> List.map (fun choice -> { Id = $"symbol:{choice.Id}"; Label = $"Symbol: {choice.Label}" })

        List.concat [ diag; paths; symbols ] |> List.sortBy (fun choice -> choice.Label)
