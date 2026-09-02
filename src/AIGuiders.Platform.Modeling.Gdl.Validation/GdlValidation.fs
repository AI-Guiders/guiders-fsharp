namespace AIGuiders.Platform.Modeling.Gdl.Validation

open AIGuiders.Platform.Modeling.Gdl.Core

type ValidationSeverity =
    | Error
    | Warning

type ValidationIssue =
    { Code: string
      Severity: ValidationSeverity
      Message: string
      DocumentPath: string option }

[<RequireQualifiedAccess>]
module GdlProjectValidation =
    let private duplicateDocumentPaths (project: GdlProject) =
        project.Documents
        |> List.groupBy (fun entry -> entry.Ref.LogicalPath)
        |> List.choose (fun (path, entries) ->
            if entries.Length > 1 then
                Some
                    { Code = "GDL_DUPLICATE_DOCUMENT"
                      Severity = Error
                      Message = $"Duplicate logical document path: {path}"
                      DocumentPath = Some path }
            else
                None)

    let private deckPresetNames (entry: GdlProjectEntry) =
        match entry.Fragment with
        | GdlFragment.Deck deck ->
            deck.Presets
            |> List.groupBy (fun preset -> preset.Name)
            |> List.choose (fun (name, presets) ->
                if presets.Length > 1 then
                    Some
                        { Code = "GDL_DECK_DUPLICATE_PRESET"
                          Severity = Error
                          Message = $"Preset '{name}' is declared more than once."
                          DocumentPath = Some entry.Ref.LogicalPath }
                else
                    None)
        | _ -> []

    let private deckZoneCoverage (entry: GdlProjectEntry) =
        match entry.Fragment with
        | GdlFragment.Deck deck ->
            let boundZones = deck.ZoneBindings |> Map.keys |> Set.ofSeq

            deck.Presets
            |> List.collect (fun preset ->
                [ preset.ForwardZoneId
                  yield! preset.MfdZoneIds |> List.map Some ]
                |> List.choose id
                |> List.choose (fun zoneId ->
                    if boundZones.Contains zoneId then
                        None
                    else
                        Some
                            { Code = "GDL_DECK_UNKNOWN_ZONE"
                              Severity = Warning
                              Message = $"Preset references zone '{zoneId}' missing from zones table."
                              DocumentPath = Some entry.Ref.LogicalPath }))
        | _ -> []

    let validate (project: GdlProject) : ValidationIssue list =
        [ yield! duplicateDocumentPaths project

          for entry in project.Documents do
              yield! deckPresetNames entry
              yield! deckZoneCoverage entry ]

    let isValid (project: GdlProject) : bool =
        validate project
        |> List.forall (fun issue -> issue.Severity <> Error)
