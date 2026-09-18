namespace AIGuiders.Platform.Modeling.Notations.Bracket

open System

/// <summary>Wire-side CodeEdit line/scope hints in SymbolRef.Container until ResolveCtx snap (plan §10).</summary>
module CodeEditWireEncoding =
    let [<Literal>] LinePrefix = "@line:"
    let [<Literal>] ScopePrefix = "@scope:"

    let private tryParseInt (raw: string) =
        match Int32.TryParse raw with
        | true, v -> Some v
        | _ -> None

    let appendHints
        (container: string list)
        (scope: string option)
        (scopeIndex: int option)
        (lineStart: int option)
        (lineEnd: int option)
        =
        let mutable hints = []

        match lineStart with
        | Some ls ->
            let le = lineEnd |> Option.defaultValue ls

            hints <-
                if ls = le then
                    $"{LinePrefix}{ls}" :: hints
                else
                    $"{LinePrefix}{ls}-{le}" :: hints
        | None -> ()

        match scope with
        | Some sk when not (String.IsNullOrWhiteSpace sk) ->
            let idx = scopeIndex |> Option.defaultValue 1
            hints <- $"{ScopePrefix}{sk.Trim().ToLowerInvariant()}:{idx}" :: hints
        | _ -> ()

        hints @ container

    let tryDecodeLine (container: string list) =
        container
        |> List.tryPick (fun entry ->
            if not (entry.StartsWith(LinePrefix, StringComparison.Ordinal)) then
                None
            else
                let raw = entry.Substring LinePrefix.Length
                let dash = raw.IndexOf('-')

                if dash < 0 then
                    tryParseInt raw |> Option.map (fun one -> one, Some one)
                else
                    match tryParseInt (raw.Substring(0, dash)), tryParseInt (raw.Substring(dash + 1)) with
                    | Some start, Some ending -> Some(start, Some ending)
                    | _ -> None)

    let tryDecodeScope (container: string list) =
        container
        |> List.tryPick (fun entry ->
            if not (entry.StartsWith(ScopePrefix, StringComparison.Ordinal)) then
                None
            else
                let raw = entry.Substring ScopePrefix.Length
                let colon = raw.IndexOf(':')

                if colon < 0 then
                    Some(raw.Trim().ToLowerInvariant(), 1)
                else
                    let kind = raw.Substring(0, colon).Trim().ToLowerInvariant()

                    match tryParseInt (raw.Substring(colon + 1)) with
                    | Some idx when idx > 0 -> Some(kind, idx)
                    | _ -> Some(kind, 1))

    let stripHints (container: string list) =
        container
        |> List.filter (fun entry ->
            not (entry.StartsWith(LinePrefix, StringComparison.Ordinal))
            && not (entry.StartsWith(ScopePrefix, StringComparison.Ordinal)))
