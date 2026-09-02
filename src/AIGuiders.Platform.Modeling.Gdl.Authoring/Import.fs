namespace AIGuiders.Platform.Modeling.Gdl.Authoring

type AuthoringImportTargetKind =
    | LogicalPath
    | WireLibrary

type AuthoringImportDirective =
    { Kind: AuthoringImportTargetKind
      Path: string
      Alias: string option
      Legacy: bool }

[<RequireQualifiedAccess>]
module AuthoringImportLine =
    let [<Literal>] NormativeKeyword = "import"

    let private tryReadAlias (remainder: string) =
        if System.String.IsNullOrWhiteSpace remainder then
            Ok None
        elif remainder.StartsWith("as ", System.StringComparison.OrdinalIgnoreCase) then
            let alias = remainder.Substring(3).Trim()
            if alias.Length = 0 then Error "import alias is empty" else Ok(Some alias)
        else
            Error "import expected `as <alias>`"

    let private tryReadQuoted (text: string) =
        let quote = text.[0]

        match text.IndexOf(quote, 1) with
        | -1 -> Error "unclosed import path quote"
        | endIndex ->
            let path = text.Substring(1, endIndex - 1)
            let rest = text.Substring(endIndex + 1).Trim()
            Ok(path, rest)

    let tryParse (line: string) =
        let text = line |> AuthoringSource.stripComment |> fun s -> s.Trim()

        if text.Length = 0 then
            None
        else
            let legacy, rest =
                if text.StartsWith("!include ", System.StringComparison.Ordinal) then
                    true, text.Substring("!include ".Length).Trim()
                elif text.StartsWith("import ", System.StringComparison.Ordinal) then
                    false, text.Substring("import ".Length).Trim()
                else
                    false, ""

            if rest.Length = 0 then
                None
            elif rest.[0] = '"' || rest.[0] = '\'' then
                match tryReadQuoted rest with
                | Ok(path, remainder) ->
                    match tryReadAlias remainder with
                    | Ok alias ->
                        Some
                            { Kind = LogicalPath
                              Path = path
                              Alias = alias
                              Legacy = legacy }
                    | Error _ -> None
                | Error _ -> None
            elif rest.[0] = '<' then
                match rest.IndexOf '>' with
                | close when close <= 1 -> None
                | close ->
                    let path = rest.Substring(1, close - 1).Trim()

                    if path.Length = 0 then
                        None
                    else
                        match tryReadAlias (rest.Substring(close + 1).Trim()) with
                        | Ok alias ->
                            Some
                                { Kind = WireLibrary
                                  Path = path
                                  Alias = alias
                                  Legacy = legacy }
                        | Error _ -> None
            else
                None
