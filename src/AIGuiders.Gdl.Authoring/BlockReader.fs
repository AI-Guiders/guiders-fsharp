namespace AIGuiders.Gdl.Authoring

type AuthoringSurfaceKind =
    | KeyValue
    | Table
    | IndentedTree

type AuthoringSectionOpener = { Keyword: string; Kind: AuthoringSurfaceKind }

type AuthoringBlock =
    { Body: AuthoringLine list
      EndLineIndex: int
      IsClosed: bool }

[<RequireQualifiedAccess>]
module BlockReader =
    let private treeSections = set [ "channels" ]

    let private kvSections = set [ "defaults"; "executors" ]

    let private tableCapableSections =
        set
            [ "variables"
              "helps"
              "phrases"
              "profiles"
              "commands"
              "bindings"
              "melodies"
              "mcp" ]

    let tryParseOpener (line: string) =
        let trimmed = line.Trim()

        if trimmed.EndsWith " table" then
            Some
                { Keyword = trimmed.Substring(0, trimmed.Length - " table".Length).Trim()
                  Kind = Table }
        elif Set.contains trimmed treeSections then
            Some { Keyword = trimmed; Kind = IndentedTree }
        elif Set.contains trimmed kvSections || Set.contains trimmed tableCapableSections then
            Some { Keyword = trimmed; Kind = KeyValue }
        else
            None

    let read
        (lines: AuthoringLine list)
        (bodyStartIndex: int)
        (keyword: string)
        (diagnostics: AuthoringDiagnostic list ref)
        =
        let rec loop i acc =
            if i >= List.length lines then
                let lineNo =
                    if bodyStartIndex > 0 then
                        (List.item (bodyStartIndex - 1) lines).LineNumber
                    else
                        1

                diagnostics.Value <-
                    AuthoringDiagnostic.create InvalidSyntax $"Unclosed block `{keyword}`." lineNo
                    :: diagnostics.Value

                { Body = List.rev acc
                  EndLineIndex = List.length lines - 1
                  IsClosed = false }
            else
                let line = List.item i lines

                if line.Text.StartsWith($"end {keyword}") then
                    { Body = List.rev acc
                      EndLineIndex = i
                      IsClosed = true }
                elif System.String.IsNullOrWhiteSpace line.Text then
                    loop (i + 1) acc
                else
                    loop (i + 1) (line :: acc)

        loop bodyStartIndex []
