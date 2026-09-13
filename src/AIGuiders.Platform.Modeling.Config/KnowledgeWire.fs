namespace AIGuiders.Platform.Modeling.Config

open System
open System.IO

/// <summary>Minimal TOML wire reads for agent-notes-mcp.toml (pilot SAT predicates).</summary>
[<RequireQualifiedAccess>]
module KnowledgeWire =

    let private tryFindAgentNotesToml (workspaceRoot: string) =
        let root = Path.GetFullPath workspaceRoot
        let direct = Path.Combine(root, "agent-notes-mcp.toml")

        if File.Exists direct then
            Some direct
        else
            try
                Directory.EnumerateFiles(root, "agent-notes-mcp.toml", SearchOption.AllDirectories)
                |> Seq.tryHead
            with
            | :? IOException -> None
            | :? UnauthorizedAccessException -> None

    let private parseSectionValue (lines: string seq) (sectionName: string) (key: string) =
        let target = $"[{sectionName}]"
        let mutable inSection = false
        let mutable found = None

        for rawLine in lines do
            if Option.isSome found then
                ()
            else
                let line = rawLine.Trim()

                if line.Length = 0 || line.StartsWith '#' then
                    ()
                elif line.StartsWith '[' then
                    inSection <- line.Equals(target, StringComparison.OrdinalIgnoreCase)
                elif inSection && line.StartsWith(key, StringComparison.OrdinalIgnoreCase) then
                    let eq = line.IndexOf '='

                    if eq > 0 then
                        let value = line[(eq + 1) ..].Trim().Trim('"', '\'')
                        if value.Length > 0 then
                            found <- Some value

        found

    let tryReadKnowledgePrimary (tomlPath: string) =
        if not (File.Exists tomlPath) then
            None
        else
            parseSectionValue (File.ReadLines tomlPath) "knowledge" "primary"

    let tryReadKnowledgeRootPath (tomlPath: string) (rootId: string) =
        if not (File.Exists tomlPath) then
            None
        else
            parseSectionValue (File.ReadLines tomlPath) "knowledge.roots" rootId

    type PersonalRootResolution =
        { TomlPath: string
          PrimaryId: string
          PersonalRoot: string }

    let tryResolvePersonalRoot (workspaceRoot: string) =
        if String.IsNullOrWhiteSpace workspaceRoot then
            None
        else
            match tryFindAgentNotesToml workspaceRoot with
            | None -> None
            | Some tomlPath ->
                match tryReadKnowledgePrimary tomlPath with
                | None -> None
                | Some primary ->
                    match tryReadKnowledgeRootPath tomlPath primary with
                    | None -> None
                    | Some personalRoot ->
                        Some
                            { TomlPath = tomlPath
                              PrimaryId = primary
                              PersonalRoot = personalRoot }
