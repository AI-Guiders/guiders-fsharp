namespace AIGuiders.Platform.Modeling.Configurations

open System

/// <summary>Pure TOML wire parse for agent-notes-mcp.toml (no File IO in Modeling).</summary>
[<RequireQualifiedAccess>]
module KnowledgeWire =

    type PersonalRootWire =
        { TomlPath: string
          PrimaryId: string
          PersonalRoot: string }

    let parseSectionValue (lines: string seq) (sectionName: string) (key: string) =
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

    let tryBuildPersonalRootWire (tomlPath: string) (lines: string seq) =
        match parseSectionValue lines "knowledge" "primary" with
        | None -> None
        | Some primary ->
            match parseSectionValue lines "knowledge.roots" primary with
            | None -> None
            | Some personalRoot ->
                Some
                    { TomlPath = tomlPath
                      PrimaryId = primary
                      PersonalRoot = personalRoot }
