namespace AIGuiders.Platform.Modeling.Gdl.Parse.Config

open System
open System.Collections.Generic
open AIGuiders.Platform.Modeling.Config
open AIGuiders.Platform.Modeling.Gdl.Authoring

type ConfigParseDiagnostic = { Code: string; Message: string; Line: int }

type ConfigParseResult =
    { Document: ConfigDocument option
      Diagnostics: ConfigParseDiagnostic array }

[<RequireQualifiedAccess>]
module ConfigParser =

    let private tryParsePair (text: string) =
        let eq = text.IndexOf('=')

        if eq <= 0 then
            None
        else
            let key = text[.. (eq - 1)].Trim()
            let value = text[(eq + 1) ..].Trim()

            if key.Length > 0 then Some(key, value) else None

    let private splitCells (text: string) =
        let inner = text.Trim()

        if not (inner.StartsWith "|") then
            [||]
        else
            let body = inner.Substring 1
            let cut = if body.EndsWith "|" then body.Substring(0, body.Length - 1) else body
            cut.Split('|') |> Array.map (fun c -> c.Trim())

    let private isSeparator (cells: string[]) =
        cells.Length > 0
        && cells |> Array.forall (fun c -> c.Length > 0 && c.Replace("-", "").Length = 0)

    let private parseMaps (body: AuthoringLine list) =
        let rows =
            body
            |> List.filter (fun l -> l.Text.TrimStart().StartsWith "|")
            |> List.map (fun l -> splitCells l.Text)

        match rows with
        | header :: rest when header.Length > 0 ->
            let lower = header |> Array.map (fun h -> h.ToLowerInvariant())

            rest
            |> List.filter (fun cells -> not (isSeparator cells))
            |> List.map (fun cells ->
                [ for i in 0 .. min (cells.Length - 1) (lower.Length - 1) do
                      if lower.[i].Length > 0 then
                          yield lower.[i], cells.[i] ]
                |> Map.ofList)
        | _ -> []

    let private parseDefaultsBlock
        (lines: AuthoringLine list)
        (startIndex: int)
        (defaults: IDictionary<string, string>)
        (diagnostics: ResizeArray<ConfigParseDiagnostic>)
        =
        let rec loop i =
            if i >= lines.Length then
                diagnostics.Add(
                    { Code = "config-unclosed-defaults"
                      Message = "Unclosed `defaults` block (expected `end defaults`)."
                      Line = if startIndex > 0 then lines.[startIndex - 1].LineNumber else 1 }
                )

                lines.Length - 1
            else
                let line = lines.[i]
                let text = line.Text.Trim()

                if text.Equals("end defaults", StringComparison.OrdinalIgnoreCase) then
                    i
                elif String.IsNullOrWhiteSpace text then
                    loop (i + 1)
                else
                    match tryParsePair line.Text with
                    | Some(key, value) ->
                        defaults.[key] <- value
                        loop (i + 1)
                    | None ->
                        diagnostics.Add(
                            { Code = "config-invalid-default"
                              Message = $"Invalid defaults entry `{text}`."
                              Line = line.LineNumber }
                        )

                        loop (i + 1)

        loop startIndex

    let private parseTableRows
        (lines: AuthoringLine list)
        (startIndex: int)
        (requireId: bool)
        (onRow: Map<string, string> -> int -> unit)
        (diagnostics: ResizeArray<ConfigParseDiagnostic>)
        (missingIdCode: string)
        (missingIdMessage: string)
        =
        let tableLines = ResizeArray<AuthoringLine>()
        let mutable i = startIndex

        while i < lines.Length do
            let text = lines.[i].Text.Trim()

            if String.IsNullOrWhiteSpace text then
                i <- i + 1
            elif not (text.StartsWith "|") then
                i <- lines.Length
            else
                tableLines.Add lines.[i] |> ignore
                i <- i + 1

        let maps = parseMaps (tableLines |> Seq.toList)

        for map in maps do
            match Map.tryFind "id" map with
            | Some id when not (String.IsNullOrWhiteSpace id) ->
                onRow map (if tableLines.Count > 0 then tableLines.[0].LineNumber else startIndex + 1)
            | _ when requireId ->
                diagnostics.Add(
                    { Code = missingIdCode
                      Message = missingIdMessage
                      Line =
                          if tableLines.Count > 0 then
                              tableLines.[tableLines.Count - 1].LineNumber
                          else
                              startIndex + 1 }
                )
                |> ignore
            | _ -> onRow map (if tableLines.Count > 0 then tableLines.[0].LineNumber else startIndex + 1)

        max startIndex (i - 1)

    let private parseContractsTable
        (lines: AuthoringLine list)
        (startIndex: int)
        (contracts: ResizeArray<ConfigContractRow>)
        (diagnostics: ResizeArray<ConfigParseDiagnostic>)
        =
        parseTableRows
            lines
            startIndex
            true
            (fun map line ->
                contracts.Add(
                    { Id = map |> Map.find "id"
                      Requires = map |> Map.tryFind "requires" |> Option.defaultValue ""
                      Ensures = map |> Map.tryFind "ensures" |> Option.defaultValue ""
                      Line = line }
                )
                |> ignore)
            diagnostics
            "config-contract-missing-id"
            "Contracts table row is missing `id`."

    let private parseSourcesTable
        (lines: AuthoringLine list)
        (startIndex: int)
        (sources: ResizeArray<ConfigSourceRow>)
        (diagnostics: ResizeArray<ConfigParseDiagnostic>)
        =
        parseTableRows
            lines
            startIndex
            true
            (fun map line ->
                sources.Add(
                    { Id = map |> Map.find "id"
                      Kind = map |> Map.tryFind "kind" |> Option.defaultValue ""
                      Path = map |> Map.tryFind "path" |> Option.defaultValue ""
                      Slice = map |> Map.tryFind "slice" |> Option.defaultValue ""
                      Line = line }
                )
                |> ignore)
            diagnostics
            "config-source-missing-id"
            "Sources table row is missing `id`."

    let private parseFactsTable
        (lines: AuthoringLine list)
        (startIndex: int)
        (facts: ResizeArray<ConfigFactRow>)
        (diagnostics: ResizeArray<ConfigParseDiagnostic>)
        =
        parseTableRows
            lines
            startIndex
            false
            (fun map line ->
                let contract = map |> Map.tryFind "contract" |> Option.defaultValue ""
                let verifiedBy = map |> Map.tryFind "verified_by" |> Option.defaultValue ""

                if not (String.IsNullOrWhiteSpace contract) && not (String.IsNullOrWhiteSpace verifiedBy) then
                    facts.Add({ Contract = contract; VerifiedBy = verifiedBy; Line = line }) |> ignore)
            diagnostics
            "config-fact-missing-contract"
            "Facts table row is missing `contract`."

    let private skipTableBody (lines: AuthoringLine list) (startIndex: int) =
        let mutable i = startIndex

        while i < lines.Length do
            let text = lines.[i].Text.Trim()

            if String.IsNullOrWhiteSpace text then
                i <- i + 1
            elif not (text.StartsWith "|") then
                i <- lines.Length
            else
                i <- i + 1

        max startIndex (i - 1)

    let parse (lines: AuthoringLine list) : ConfigParseResult =
        let diagnostics = ResizeArray<ConfigParseDiagnostic>()
        let mutable name: string option = None
        let mutable basedOnAdr: string option = None
        let defaults = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        let sources = ResizeArray<ConfigSourceRow>()
        let contracts = ResizeArray<ConfigContractRow>()
        let facts = ResizeArray<ConfigFactRow>()
        let lineList = lines |> List.ofSeq
        let mutable i = 0

        while i < lineList.Length do
            let line = lineList.[i]
            let text = line.Text.Trim()

            if String.IsNullOrWhiteSpace text then
                i <- i + 1
            elif text.StartsWith("config ", StringComparison.OrdinalIgnoreCase) then
                name <- Some(text["config ".Length ..].Trim())
                i <- i + 1
            elif text.StartsWith("based on adr:", StringComparison.OrdinalIgnoreCase) then
                basedOnAdr <- Some(text["based on adr:".Length ..].Trim())
                i <- i + 1
            elif text.StartsWith("import ", StringComparison.OrdinalIgnoreCase) then
                i <- i + 1
            elif text.Equals("defaults", StringComparison.OrdinalIgnoreCase) then
                i <- parseDefaultsBlock lineList (i + 1) defaults diagnostics
                i <- i + 1
            elif text.Equals("sources table", StringComparison.OrdinalIgnoreCase) then
                i <- parseSourcesTable lineList (i + 1) sources diagnostics
                i <- i + 1
            elif text.Equals("contracts table", StringComparison.OrdinalIgnoreCase) then
                i <- parseContractsTable lineList (i + 1) contracts diagnostics
                i <- i + 1
            elif text.Equals("facts table", StringComparison.OrdinalIgnoreCase) then
                i <- parseFactsTable lineList (i + 1) facts diagnostics
                i <- i + 1
            elif text.EndsWith(" table", StringComparison.OrdinalIgnoreCase) then
                i <- skipTableBody lineList (i + 1)
                i <- i + 1
            else
                i <- i + 1

        match name with
        | None ->
            diagnostics.Add(
                { Code = "config-missing-header"
                  Message = "Missing `config <name>` header."
                  Line = if lineList.Length > 0 then lineList.[0].LineNumber else 1 }
            )

            { Document = None; Diagnostics = diagnostics.ToArray() }
        | Some n ->
            { Document =
                  Some
                      { Name = n
                        BasedOnAdr = basedOnAdr
                        Defaults = defaults
                        Sources = sources.ToArray()
                        Contracts = contracts.ToArray()
                        Facts = facts.ToArray() }
              Diagnostics = diagnostics.ToArray() }

    let parseText (text: string) : ConfigParseResult =
        AuthoringSource.fromText text |> parse

    let parseFile (path: string) : ConfigParseResult =
        AuthoringSource.fromFile path |> parse
