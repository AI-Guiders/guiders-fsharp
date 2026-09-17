namespace AIGuiders.Platform.Modeling.CommandPlane

open System
open AIGuiders.Platform.Modeling.LanguageIntelligence.Relations

/// <summary>Human attach verb picker (plan §4.3) — maps to <see cref="RelationSpec"/> cases.</summary>
type AttachVerb =
    | Error
    | Issue
    | Document
    | Code
    | Nav
    | Manual

type AttachSchemaStep = { Id: string; Prompt: string }

type AttachSchema =
    { Verb: AttachVerb
      TargetCase: string
      Steps: AttachSchemaStep list }

module AttachSchema =
    let forVerb =
        function
        | Error ->
            { Verb = Error
              TargetCase = "Diag"
              Steps = [ { Id = "pick_diagnostic"; Prompt = "Pick diagnostic" } ] }
        | Issue ->
            { Verb = Issue
              TargetCase = "Resource"
              Steps = [ { Id = "pick_issue"; Prompt = "Pick issue or merge request" } ] }
        | Document ->
            { Verb = Document
              TargetCase = "DocToCode"
              Steps =
                [ { Id = "pick_doc"; Prompt = "Pick documentation place" }
                  { Id = "pick_code"; Prompt = "Pick code target" } ] }
        | Code ->
            { Verb = Code
              TargetCase = "CodeEdit"
              Steps =
                [ { Id = "pick_file"; Prompt = "Pick file" }
                  { Id = "pick_member"; Prompt = "Pick member" } ] }
        | Nav ->
            { Verb = Nav
              TargetCase = "Nav"
              Steps = [ { Id = "pick_nav"; Prompt = "Pick navigation target" } ] }
        | Manual ->
            { Verb = Manual
              TargetCase = "*"
              Steps =
                [ { Id = "pick_kind"; Prompt = "Pick RelationSpec kind" }
                  { Id = "browse_all"; Prompt = "Browse all attach targets" } ] }

    let relationSpecCaseName (spec: RelationSpec) =
        match spec with
        | RelationSpec.CodeEdit _ -> "CodeEdit"
        | RelationSpec.DocToCode _ -> "DocToCode"
        | RelationSpec.Diag _ -> "Diag"
        | RelationSpec.Address _ -> "Address"
        | RelationSpec.Nav _ -> "Nav"
        | RelationSpec.Resource _ -> "Resource"

    let tryParseVerb (name: string) =
        match name.Trim().ToLowerInvariant() with
        | "error" -> Some Error
        | "issue" -> Some Issue
        | "document" -> Some Document
        | "code" -> Some Code
        | "nav" -> Some Nav
        | "manual" -> Some Manual
        | _ -> None

    let tryParseVerbWire (wire: string) = tryParseVerb wire

    let validateSpecVector (verb: string) (targetCase: string) (steps: string list) =
        match tryParseVerb verb with
        | None -> [ $"unknown attach verb \"{verb}\"" ]
        | Some parsed ->
            let schema = forVerb parsed
            let errors = ResizeArray<string>()

            if schema.TargetCase <> targetCase then
                errors.Add($"verb \"{verb}\" targetCase expected \"{targetCase}\", got \"{schema.TargetCase}\"")

            let actual = schema.Steps |> List.map (fun s -> s.Id)

            if actual <> steps then
                let expected = String.Join(", ", steps)
                let got = String.Join(", ", actual)
                errors.Add($"verb \"{verb}\" steps expected [{expected}], got [{got}]")

            List.ofSeq errors

module AttachSchemaCatalog =
    [<Literal>]
    let VerbSuggestionId = "federation.attach.verb"

    let verbWireName =
        function
        | AttachVerb.Error -> "error"
        | AttachVerb.Issue -> "issue"
        | AttachVerb.Document -> "document"
        | AttachVerb.Code -> "code"
        | AttachVerb.Nav -> "nav"
        | AttachVerb.Manual -> "manual"

    let allVerbs =
        [ AttachVerb.Error
          AttachVerb.Issue
          AttachVerb.Document
          AttachVerb.Code
          AttachVerb.Nav
          AttachVerb.Manual ]

    let schemas = allVerbs |> List.map AttachSchema.forVerb
