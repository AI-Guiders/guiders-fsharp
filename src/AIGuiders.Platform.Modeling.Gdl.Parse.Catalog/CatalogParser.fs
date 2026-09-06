namespace AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

open System
open AIGuiders.Platform.Modeling.Gdl.Authoring

/// F# mirror of Platform Authoring.Command.Catalog parser (phase 1: structure, tables,
/// channels tree, defaults, profiles, diagnostics). Grammar cross-validation
/// (CatalogGrammarValidator / ValidateChannels) is a follow-up port.
[<RequireQualifiedAccess>]
module CatalogParser =

    let private keywords =
        [ "channels"
          "defaults"
          "variables"
          "helps"
          "phrases"
          "profiles"
          "commands"
          "bindings"
          "melodies"
          "mcp"
          "executors" ]

    let private sectionKeyword (text: string) =
        let t = text.Trim()

        keywords
        |> List.tryPick (fun k ->
            if t.Equals(k, StringComparison.OrdinalIgnoreCase) then Some(k, false)
            elif t.Equals(k + " table", StringComparison.OrdinalIgnoreCase) then Some(k, true)
            else None)

    let private splitCells (text: string) : string[] =
        let inner = text.Trim()

        if not (inner.StartsWith "|") then [||]
        else
            let body = inner.Substring(1)
            let cut = if body.EndsWith "|" then body.Substring(0, body.Length - 1) else body
            cut.Split('|') |> Array.map (fun c -> c.Trim())

    let private isSeparator (cells: string[]) =
        cells.Length > 0
        && cells |> Array.forall (fun c -> c.Length > 0 && c.Replace("-", "").Length = 0)

    let private parseMaps (body: AuthoringLine list) : Map<string, string> list =
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
                [ for i in 0 .. (min (cells.Length - 1) (lower.Length - 1)) do
                    if lower.[i].Length > 0 then yield lower.[i], cells.[i] ]
                |> Map.ofList)
        | _ -> []

    let private col (map: Map<string, string>) (key: string) =
        map |> Map.tryFind key |> Option.defaultValue ""

    let private parseChannels (body: AuthoringLine list) (diagnostics: AuthoringDiagnostic list ref) =
        let channels = System.Collections.Generic.List<CatalogChannel>()
        let mutable current: string option = None
        let len = List.length body
        let mutable i: int = 0

        while i < len do
            let line: AuthoringLine = List.item i body
            let t = line.Text.Trim()

            if String.IsNullOrWhiteSpace t then ()
            elif t = "grammar" then
                let rec findEnd (j: int) : int option =
                    if j >= len then None
                    else if (List.item j body).Text.Trim() = "end grammar" then Some j
                    else findEnd (j + 1)

                match findEnd (i + 1) with
                | Some endIdx ->
                    let mutable cmd: string option = None
                    let mutable arg: string option = None

                    for j in (i + 1) .. (endIdx - 1) do
                        let inner = (List.item j body).Text.Trim()
                        let kv = inner.Split('=', 2, StringSplitOptions.TrimEntries)

                        if kv.Length = 2 then
                            if kv.[0].Equals("command", StringComparison.OrdinalIgnoreCase) then cmd <- Some kv.[1]
                            elif kv.[0].Equals("argument", StringComparison.OrdinalIgnoreCase) then arg <- Some kv.[1]

                    if channels.Count > 0 then
                        let last = channels.[channels.Count - 1]
                        channels.[channels.Count - 1] <-
                            { last with
                                CommandGrammar = cmd
                                ArgumentGrammar = arg }
                    else
                        diagnostics.Value <-
                            AuthoringDiagnostic.create
                                MissingGrammarDeclaration
                                "`grammar` block without a preceding channel entry."
                                line.LineNumber
                            :: diagnostics.Value

                    i <- endIdx
                | None ->
                    diagnostics.Value <-
                        AuthoringDiagnostic.create
                            MissingGrammarDeclaration
                            "Unclosed `grammar` block in channels."
                            line.LineNumber
                        :: diagnostics.Value
            elif t.Contains "=" then
                let kv = t.Split('=', 2, StringSplitOptions.TrimEntries)

                match current with
                | Some surf ->
                    channels.Add
                        { Surface = surf
                          Sub = Some kv.[0]
                          PlanetId = Some kv.[1]
                          CommandGrammar = None
                          ArgumentGrammar = None }
                | None ->
                    channels.Add
                        { Surface = kv.[0]
                          Sub = None
                          PlanetId = Some kv.[1]
                          CommandGrammar = None
                          ArgumentGrammar = None }
            else
                current <- Some t

            i <- i + 1

        Seq.toList channels

    let private parseDefaults (line: AuthoringLine) (defaults: CatalogDefaults) (diagnostics: AuthoringDiagnostic list ref) =
        let t = line.Text.Trim()
        let kv = t.Split('=', 2, StringSplitOptions.TrimEntries)

        if kv.Length <> 2 then
            diagnostics.Value <-
                AuthoringDiagnostic.create
                    InvalidSyntax
                    $"Expected `key = value` in defaults: `{line.Text}`."
                    line.LineNumber
                :: diagnostics.Value

            defaults
        else
            let value = kv.[1]
            let key = kv.[0]

            if key = "variable.kind" then { defaults with VariableKind = Some value }
            elif key = "command.scope" then { defaults with CommandScope = Some value }
            elif key = "command.surfaces" then
                { defaults with
                    CommandSurfaces =
                        value.Split(',', StringSplitOptions.TrimEntries ||| StringSplitOptions.RemoveEmptyEntries)
                        |> Array.toList }
            elif key = "grammar.keyboard.binding" then { defaults with GrammarKeyboardBinding = Some value }
            elif key = "grammar.keyboard.melody" then { defaults with GrammarKeyboardMelody = Some value }
            elif key = "binding.chord-root" then { defaults with BindingChordRoot = Some value }
            elif key = "command.flavor" then { defaults with CommandFlavor = Some value }
            else
                diagnostics.Value <-
                    AuthoringDiagnostic.create
                        InvalidSyntax
                        $"Unknown defaults key `{key}`."
                        line.LineNumber
                    :: diagnostics.Value

                defaults

    let rec private parseLines (lines: AuthoringLine list) =
        let diagnostics = ref ([]: AuthoringDiagnostic list)
        let planet = ref None
        let imports = ref []
        let defaults = ref CatalogDocument.empty.Defaults
        let channels = ref []
        let variables = ref []
        let helps = ref []
        let phrases = ref []
        let profiles = ref []
        let commands = ref []
        let bindings = ref []
        let melodies = ref []
        let mcp = ref []
        let executors = ref Map.empty
        let seenCommands = ref Set.empty

        let applySection (kw: string) (body: AuthoringLine list) =
            match kw with
            | "channels" -> channels.Value <- parseChannels body diagnostics
            | "variables" ->
                variables.Value <-
                    body
                    |> List.filter (fun l -> not (String.IsNullOrWhiteSpace l.Text))
                    |> List.map (fun l -> { Name = l.Text.Trim(); Kind = None })
            | "profiles" ->
                for line in body do
                    let t = line.Text.Trim()
                    let kv = t.Split('=', 2, StringSplitOptions.TrimEntries)

                    if kv.Length <> 2 then
                        diagnostics.Value <-
                            AuthoringDiagnostic.create
                                InvalidSyntax
                                $"Expected `profile = ...` in profiles: `{line.Text}`."
                                line.LineNumber
                            :: diagnostics.Value
                    elif kv.[1].StartsWith "bundle " then
                        profiles.Value <-
                            { Name = kv.[0]
                              Entries = []
                              BundleSource = Some(kv.[1].Substring("bundle ".Length).Trim()) }
                            :: profiles.Value
                    else
                        let dotted = kv.[0].Split('.')

                        if dotted.Length < 3 then
                            diagnostics.Value <-
                                AuthoringDiagnostic.create
                                    InvalidSyntax
                                    $"Expected `profile.arg.entry = ref` in profiles: `{line.Text}`."
                                    line.LineNumber
                                :: diagnostics.Value
                        else
                            let name = dotted.[0]
                            let entry = { Arg = dotted.[1]; Entry = String.Join(".", dotted.[2..]); Ref = kv.[1] }

                            let existing =
                                profiles.Value
                                |> List.tryFind (fun p -> p.Name = name)

                            profiles.Value <-
                                match existing with
                                | Some p ->
                                    (profiles.Value
                                     |> List.filter (fun p -> p.Name <> name))
                                    @ [ { p with Entries = p.Entries @ [ entry ] } ]
                                | None -> profiles.Value @ [ { Name = name; Entries = [ entry ]; BundleSource = None } ]
            | "executors" ->
                for line in body do
                    let kv = line.Text.Trim().Split('=', 2, StringSplitOptions.TrimEntries)

                    if kv.Length = 2 then executors.Value <- Map.add kv.[0] kv.[1] executors.Value
            | "defaults" ->
                for line in body do
                    if not (String.IsNullOrWhiteSpace line.Text) then
                        defaults.Value <- parseDefaults line defaults.Value diagnostics
            | tableKw ->
                let maps = parseMaps body

                match tableKw with
                | "helps" ->
                    helps.Value <-
                        maps
                        |> List.map (fun m -> { Target = col m "target"; Field = col m "field"; Text = col m "text" })
                | "phrases" ->
                    phrases.Value <-
                        maps |> List.map (fun m -> { Name = col m "name"; Phrase = col m "phrase" })
                | "commands" ->
                    for m in maps do
                        let command = col m "command"

                        if command.Length > 0 then
                            if Set.contains command seenCommands.Value then
                                diagnostics.Value <-
                                    AuthoringDiagnostic.create
                                        DuplicateRow
                                        $"Duplicate command `{command}`."
                                        (List.item 0 body).LineNumber
                                    :: diagnostics.Value

                            seenCommands.Value <- Set.add command seenCommands.Value
                            commands.Value <- { Command = command; Columns = m } :: commands.Value
                | "bindings" ->
                    bindings.Value <-
                        maps
                        |> List.map (fun m ->
                            { Gesture = col m "gesture"
                              Command = col m "command"
                              Role = m |> Map.tryFind "role" |> Option.filter (fun r -> r.Length > 0) })
                | "melodies" ->
                    melodies.Value <-
                        maps |> List.map (fun m -> { Slug = col m "slug"; Command = col m "command" })
                | "mcp" ->
                    mcp.Value <-
                        maps
                        |> List.map (fun m ->
                            { Command = col m "command"
                              Expose =
                                m
                                |> Map.tryFind "expose"
                                |> Option.filter (fun e -> e.Length > 0)
                                |> Option.defaultValue "yes" })
                | _ -> ()

        let rec loop (i: int) : CatalogParseResult =
            if i >= List.length lines then
                match planet.Value with
                | None ->
                    diagnostics.Value <-
                        AuthoringDiagnostic.create MissingCatalogHeader "Missing `catalog <planet>` header." 1
                        :: diagnostics.Value

                    { Document = None; Diagnostics = List.rev diagnostics.Value }
                | Some p ->
                    { Document =
                        Some
                            { Planet = p
                              Imports = List.rev imports.Value
                              Defaults = defaults.Value
                              Channels = channels.Value
                              Variables = List.rev variables.Value
                              Helps = helps.Value
                              Phrases = phrases.Value
                              Profiles = profiles.Value
                              Commands = List.rev commands.Value
                              Bindings = bindings.Value
                              Melodies = melodies.Value
                              Mcp = mcp.Value
                              Executors = executors.Value }
                      Diagnostics = List.rev diagnostics.Value }
            else
                let line = List.item i lines
                let trimmed = line.Text.Trim()

                if String.IsNullOrWhiteSpace trimmed then loop (i + 1)
                elif planet.Value.IsNone && trimmed.StartsWith "catalog " then
                    planet.Value <- Some(trimmed.Substring("catalog ".Length).Trim())
                    loop (i + 1)
                elif trimmed.StartsWith "import " then
                    let wire = trimmed.Substring("import ".Length).Trim()

                    if wire.StartsWith "<" && wire.EndsWith ">" then
                        imports.Value <- wire.Substring(1, wire.Length - 2) :: imports.Value

                    loop (i + 1)
                elif trimmed.StartsWith "end catalog" then
                    loop (i + 1)
                else
                    match sectionKeyword trimmed with
                    | Some(kw, _) ->
                        let block = BlockReader.read lines (i + 1) kw diagnostics
                        applySection kw block.Body
                        let next = if block.IsClosed then block.EndLineIndex + 1 else List.length lines
                        loop next
                    | None ->
                        diagnostics.Value <-
                            AuthoringDiagnostic.create
                                InvalidSyntax
                                $"Unexpected line in catalog document: `{line.Text}`."
                                line.LineNumber
                            :: diagnostics.Value

                        loop (i + 1)

        loop 0

    let parse (text: string) : CatalogParseResult = parseLines (AuthoringSource.fromText text)

    let parseFile (path: string) : CatalogParseResult = parseLines (AuthoringSource.fromFile path)
