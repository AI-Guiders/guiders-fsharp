namespace AIGuiders.Platform.Modeling.Notations.Command

open System

/// <summary>Federation attach slash surface (plan §4.1–§4.2).</summary>
module AttachInvocation =
    [<Literal>]
    let RootCommandId = "federation.attach"

    [<Literal>]
    let RootPath = "attach"

    [<Literal>]
    let VerbSuggestionId = "federation.attach.verb"

    let verbWireNames =
        [ "error"; "issue"; "document"; "code"; "nav"; "manual" ]

    let isVerbWire (wire: string) =
        not (String.IsNullOrWhiteSpace wire)
        && verbWireNames |> List.exists (fun v -> String.Equals(v, wire.Trim(), StringComparison.OrdinalIgnoreCase))

    let pathForVerb (verbWire: string) = $"{RootPath} {verbWire.Trim()}"

    let tryParsePath (path: string) =
        if String.IsNullOrWhiteSpace path then
            None
        else
            let trimmed = path.Trim()

            if String.Equals(trimmed, RootPath, StringComparison.OrdinalIgnoreCase) then
                Some None
            elif trimmed.StartsWith(RootPath + " ", StringComparison.OrdinalIgnoreCase) then
                let verb = trimmed.Substring(RootPath.Length + 1).Trim()

                if isVerbWire verb then Some(Some verb) else None
            else
                None
