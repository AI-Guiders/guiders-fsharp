namespace AIGuiders.Platform.Modeling.Gdl.Parse.IdeSession

open System
open AIGuiders.Platform.Modeling.Gdl.Authoring
open AIGuiders.Platform.Modeling.IdeSession.GateCatalog

type IdeSessionCatalogParseDiagnostic = { Code: string; Message: string; Line: int }

type IdeSessionCatalogParseResult =
    { Catalog: IdeSessionGateCatalog option
      Diagnostics: IdeSessionCatalogParseDiagnostic array }

[<RequireQualifiedAccess>]
module IdeSessionCatalogParser =

    let private gatesSectionKeyword = "gates"

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

    let private parseGateRows
        (rows: Map<string, string> list)
        (diagnostics: ResizeArray<IdeSessionCatalogParseDiagnostic>)
        (sourcePath: string)
        =
        let gates = ResizeArray<IdeSessionGateRow>()

        for row in rows do
            match Map.tryFind "gate" row with
            | Some gateId when not (String.IsNullOrWhiteSpace gateId) ->
                gates.Add(
                    { GateId = gateId.Trim()
                      RejectWhen =
                          row
                          |> Map.tryFind "reject-when"
                          |> Option.map (fun s -> s.Trim())
                          |> Option.defaultValue ""
                      Code =
                          row
                          |> Map.tryFind "code"
                          |> Option.map (fun s -> s.Trim())
                          |> Option.defaultValue "" }
                )
                |> ignore
            | _ ->
                diagnostics.Add(
                    { Code = "ide-session.gates.row"
                      Message = "Gate row is missing the `gate` column."
                      Line = 1 }
                )
                |> ignore

        if gates.Count = 0 then
            diagnostics.Add(
                { Code = "ide-session.gates.empty"
                  Message = $"No gate rows found in `{sourcePath}`."
                  Line = 1 }
            )
            |> ignore

        gates.ToArray()

    let parse (lines: AuthoringLine list) (sourcePath: string) : IdeSessionCatalogParseResult =
        let diagnostics = ResizeArray<IdeSessionCatalogParseDiagnostic>()
        let lineList = lines |> List.ofSeq

        let rec findGatesTable i =
            if i >= lineList.Length then
                None
            else
                let line = lineList.[i]

                match BlockReader.tryParseOpener line.Text with
                | Some opener when
                    opener.Kind = Table
                    && opener.Keyword.Equals(gatesSectionKeyword, StringComparison.OrdinalIgnoreCase)
                    ->
                    Some(line, i)
                | _ -> findGatesTable (i + 1)

        match findGatesTable 0 with
        | None ->
            diagnostics.Add(
                { Code = "ide-session.gates.missing"
                  Message = "Missing `gates table` block."
                  Line = 1 }
            )
            |> ignore

            { Catalog = None; Diagnostics = diagnostics.ToArray() }
        | Some(line, i) ->
            let blockDiagnostics = ref []
            let block = BlockReader.read lineList (i + 1) gatesSectionKeyword blockDiagnostics

            if not block.IsClosed then
                diagnostics.Add(
                    { Code = "ide-session.gates.unclosed"
                      Message = "Unclosed `gates table` block."
                      Line = line.LineNumber }
                )
                |> ignore

                { Catalog = None; Diagnostics = diagnostics.ToArray() }
            else
                let gates = parseGateRows (parseMaps block.Body) diagnostics sourcePath

                { Catalog =
                      Some
                          { SourcePath = sourcePath
                            Gates = gates }
                  Diagnostics = diagnostics.ToArray() }

    let parseText (text: string) (sourcePath: string) : IdeSessionCatalogParseResult =
        parse (AuthoringSource.fromText text) sourcePath
