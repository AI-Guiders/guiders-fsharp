namespace AIGuiders.Platform.Modeling.Configurations

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions

[<CLIMutable>]
type ConfigPredicateDiagnostic = { Code: string; Message: string }

[<CLIMutable>]
type ConfigPredicateResult =
    { Satisfied: bool
      Note: string
      Diagnostic: ConfigPredicateDiagnostic }

[<RequireQualifiedAccess>]
module ContractPredicates =

    let private sectionRegex =
        Regex(
            @"<!--\s*section:(?<id>[A-Za-z0-9._-]+)\s*-->\s*(?<content>.*?)\s*<!--\s*/section:\k<id>\s*-->",
            RegexOptions.Compiled ||| RegexOptions.Singleline)

    let private idRegex = Regex("^[A-Za-z0-9._-]+$", RegexOptions.Compiled)

    let private pilotHotNotesPath = "{personal}/agent-notes.md"

    let private pilotManifestPath =
        "{personal}/knowledge/META/memory-architecture-v1.json"

    let private defaultManifestRelative = "knowledge/META/memory-architecture-v1.json"

    let private manifestPathRegex =
        Regex(
            @"(?m)^\s*l0_manifest\s*:\s*(?<path>\S+)\s*$",
            RegexOptions.Compiled ||| RegexOptions.CultureInvariant)

    let private expandPathTemplate (template: string) (personalRoot: string) =
        template.Replace("{personal}", personalRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))

    let private resolveSourcePath (document: ConfigDocument) (sourceId: string) (fallback: string) =
        document.Sources
        |> Array.tryFind (fun s -> s.Id.Equals(sourceId, StringComparison.OrdinalIgnoreCase))
        |> Option.map (fun s -> s.Path)
        |> Option.defaultValue fallback

    let private parseSections (content: string) =
        let map = System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)

        for match' in sectionRegex.Matches(content) do
            let id = match'.Groups["id"].Value
            let body = match'.Groups["content"].Value
            map.[id] <- body

        map

    let private sliceAbovePublicCut (content: string) =
        let marker = "<!-- public-cut -->"
        let idx = content.IndexOf(marker, StringComparison.Ordinal)

        if idx < 0 then
            content
        else
            content.Substring(0, idx)

    let private parseL0FromMemoryArchitectureSection (content: string) =
        let lines = content.Replace("\r\n", "\n").Split('\n')
        let mutable inL0 = false
        let ids = ResizeArray<string>()

        for line in lines do
            let t = line.Trim()

            if t.StartsWith("### L0:", StringComparison.OrdinalIgnoreCase) then
                inL0 <- true
            elif inL0 then
                if t.StartsWith("### ", StringComparison.Ordinal) then
                    inL0 <- false
                elif t.StartsWith("- ", StringComparison.Ordinal) then
                    let rest = t.Substring(2).Trim()
                    let id = rest.Split([| ' '; '('; '\t' |], 2, StringSplitOptions.None).[0].Trim()

                    if id.Length > 0 && idRegex.IsMatch id then
                        ids.Add id

        if ids.Count > 0 then Some(ids.ToArray()) else None

    let private tryLoadManifestL0FromJson (manifestJson: string) =
        if String.IsNullOrWhiteSpace manifestJson then
            None
        else
            try
                use doc = JsonDocument.Parse manifestJson

                let mutable l0El = Unchecked.defaultof<JsonElement>

                if doc.RootElement.TryGetProperty("l0", &l0El) then

                    if l0El.ValueKind = JsonValueKind.Array then
                        let ids =
                            l0El.EnumerateArray()
                            |> Seq.choose (fun item ->
                                if item.ValueKind = JsonValueKind.String then
                                    let id =
                                        match item.GetString() with
                                        | null -> ""
                                        | s -> s.Trim()

                                    if id.Length > 0 && idRegex.IsMatch id then
                                        Some id
                                    else
                                        None
                                else
                                    None)
                            |> Seq.toArray

                        if ids.Length > 0 then Some ids else None
                    else
                        None
                else
                    None
            with _ ->
                None

    let private resolveRelativeManifestPath (personalRoot: string) (manifestRef: string) =
        let trimmed = manifestRef.Trim().Trim('"')

        let knowledgePrefix = "knowledge" + string Path.DirectorySeparatorChar

        if
            trimmed.StartsWith("knowledge/", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith(knowledgePrefix, StringComparison.OrdinalIgnoreCase)
        then
            Path.GetFullPath(Path.Combine(personalRoot, trimmed))
        elif trimmed.StartsWith("./") || trimmed.StartsWith("." + string Path.DirectorySeparatorChar) then
            Path.GetFullPath(Path.Combine(personalRoot, trimmed))
        elif Path.IsPathRooted trimmed then
            Path.GetFullPath trimmed
        else
            Path.GetFullPath(Path.Combine(personalRoot, "knowledge", trimmed))

    let private tryResolveManifestFullPath
        (personalRoot: string)
        (notesContent: string)
        (configuredManifestFullPath: string)
        =
        let sections = parseSections notesContent

        let fromSection =
            if sections.ContainsKey "memory-architecture-v1" then
                let memoryArch = sections.["memory-architecture-v1"]
                let match' = manifestPathRegex.Match(memoryArch)

                if match'.Success then
                    Some(resolveRelativeManifestPath personalRoot (match'.Groups["path"].Value.Trim()))
                else
                    None
            else
                None

        match fromSection with
        | Some fullPath -> fullPath
        | None ->
            if String.IsNullOrWhiteSpace configuredManifestFullPath then
                resolveRelativeManifestPath personalRoot defaultManifestRelative
            else
                configuredManifestFullPath

    let private fail code message =
        { Satisfied = false
          Note = ""
          Diagnostic = { Code = code; Message = message } }

    let private ok note =
        { Satisfied = true
          Note = note
          Diagnostic = Unchecked.defaultof<ConfigPredicateDiagnostic> }

    /// <summary>
    /// Pure predicate: hot notes + optional manifest JSON already loaded by Execution sources.
    /// </summary>
    let evaluateHotL0SectionsPresent
        (wire: KnowledgeWire.PersonalRootWire)
        (hotNotesContent: string)
        (manifestJsonContent: string option)
        (document: ConfigDocument)
        =
        let hotTemplate = resolveSourcePath document "personal-hot" pilotHotNotesPath
        let manifestTemplate = resolveSourcePath document "l0-manifest" pilotManifestPath
        let notesPath = expandPathTemplate hotTemplate wire.PersonalRoot
        let configuredManifestPath = expandPathTemplate manifestTemplate wire.PersonalRoot

        if String.IsNullOrWhiteSpace hotNotesContent then
            fail "config-hot-notes-missing" (sprintf "Personal hot file not found: '%s'." notesPath)
        else
            let slice =
                document.Sources
                |> Array.tryFind (fun s -> s.Id.Equals("personal-hot", StringComparison.OrdinalIgnoreCase))
                |> Option.map (fun s -> s.Slice)
                |> Option.defaultValue "above_public_cut"

            let scopedContent =
                if slice.Equals("above_public_cut", StringComparison.OrdinalIgnoreCase) then
                    sliceAbovePublicCut hotNotesContent
                else
                    hotNotesContent

            let sections = parseSections scopedContent

            let manifestFullPath =
                tryResolveManifestFullPath wire.PersonalRoot hotNotesContent configuredManifestPath

            let l0Ids =
                manifestJsonContent
                |> Option.bind tryLoadManifestL0FromJson
                |> Option.orElse (
                    sections
                    |> fun map ->
                        if map.ContainsKey "memory-architecture-v1" then
                            parseL0FromMemoryArchitectureSection map.["memory-architecture-v1"]
                        else
                            None
                )

            match l0Ids with
            | None ->
                fail
                    "config-l0-manifest-missing"
                    (sprintf "No L0 ids from manifest '%s' or memory-architecture-v1 section." manifestFullPath)
            | Some ids when ids.Length = 0 ->
                fail "config-l0-empty" "L0 manifest resolved but contains no section ids."
            | Some ids ->
                let missing =
                    ids
                    |> Array.filter (fun id -> not (sections.ContainsKey id))
                    |> Array.truncate 8

                if missing.Length > 0 then
                    let listed = String.Join(", ", missing)

                    fail
                        "config-l0-sections-missing"
                        (sprintf "Missing L0 section(s) in '%s': %s." notesPath listed)
                else
                    ok
                        (sprintf
                            "hot_l0_sections_present via '%s' (%d L0 section(s) from '%s')"
                            notesPath
                            ids.Length
                            manifestFullPath)
