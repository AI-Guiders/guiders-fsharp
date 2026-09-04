namespace AIGuiders.Platform.Modeling.Notations.Command.Slash

open System
open System.Collections.Generic
open AIGuiders.Platform.Modeling.Notations.Command

/// <summary>Slash/console line notation: body parse and line gate (GUIDERS-ADR-0021).</summary>
[<RequireQualifiedAccess>]
module SlashCommandNotation =

    let parseBody (body: string) : SlashWireBody =
        let endsWithSpace = body.EndsWith ' '
        let tokens =
            body.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            |> Seq.toList
            :> IReadOnlyList<string>
        { Tokens = tokens; EndsWithSpaceAfterTokens = endsWithSpace }

    /// <summary>Slash gate: must start with '/', non-empty token path after it.</summary>
    let tryParseLine (slashLine: string) : SlashWireBody option =
        if String.IsNullOrWhiteSpace slashLine || slashLine.[0] <> '/' then None
        else
            let body = parseBody (slashLine.Substring(1).TrimEnd())
            if body.Tokens.Count > 0 then Some body else None
