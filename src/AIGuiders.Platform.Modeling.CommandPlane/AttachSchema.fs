namespace AIGuiders.Platform.Modeling.CommandPlane

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
