namespace AIGuiders.Platform.Modeling.Gdl.Parse.Deck

open AIGuiders.Platform.Modeling.Gdl.Authoring
open AIGuiders.Platform.Modeling.Gdl.Presentation

[<RequireQualifiedAccess>]
module DeckParser =
    let rec private parseLines (lines: AuthoringLine list) =
        let diagnostics = ref ([]: AuthoringDiagnostic list)
        loop 0 None [] Map.empty lines diagnostics

    and private loop i planet presets zoneBindings lines diagnostics =
        if i >= List.length lines then
            match planet with
            | None ->
                diagnostics.Value <-
                    AuthoringDiagnostic.create MissingDeckHeader "Missing `deck <planet>` header." 1
                    :: diagnostics.Value

                { Document = None; Diagnostics = List.rev diagnostics.Value }
            | Some planet ->
                { Document =
                    Some
                        { Planet = planet
                          Presets = List.rev presets
                          ZoneBindings = zoneBindings }
                  Diagnostics = List.rev diagnostics.Value }
        else
            let line = List.item i lines

            if System.String.IsNullOrWhiteSpace line.Text then
                loop (i + 1) planet presets zoneBindings lines diagnostics
            elif planet.IsNone && line.Text.TrimStart().StartsWith "deck " then
                let planet = line.Text.Trim().Substring("deck ".Length).Trim()
                loop (i + 1) (Some planet) presets zoneBindings lines diagnostics
            elif line.Text.TrimStart().StartsWith "preset " then
                let presetName = line.Text.Trim().Substring("preset ".Length).Trim()
                let block = BlockReader.read lines (i + 1) "preset" diagnostics
                let preset = parsePreset presetName block.Body diagnostics
                let next = if block.IsClosed then block.EndLineIndex + 1 else List.length lines
                loop next planet (preset :: presets) zoneBindings lines diagnostics
            elif line.Text.Trim().Equals("zones", System.StringComparison.OrdinalIgnoreCase) then
                let block = BlockReader.read lines (i + 1) "zones" diagnostics
                let zoneBindings = mergeZoneBindings block.Body zoneBindings diagnostics
                let next = if block.IsClosed then block.EndLineIndex + 1 else List.length lines
                loop next planet presets zoneBindings lines diagnostics
            elif line.Text.TrimStart().StartsWith "end deck" then
                loop (i + 1) planet presets zoneBindings lines diagnostics
            else
                diagnostics.Value <-
                    AuthoringDiagnostic.create InvalidSyntax $"Unexpected line in deck document: `{line.Text}`." line.LineNumber
                    :: diagnostics.Value

                loop (i + 1) planet presets zoneBindings lines diagnostics

    and private parsePreset name body diagnostics =
        let mutable forward = None
        let mutable mfdZones = []
        let mutable eicas = None
        let mutable topology = None

        for line in body do
            let text = line.Text.Trim()

            if text.StartsWith "topology " then
                let wire = text.Substring("topology ".Length).Trim()
                let parsed = TopologyNotation.parse wire

                if not parsed.IsSuccess then
                    diagnostics.Value <-
                        AuthoringDiagnostic.create TopologyWireInvalid (Option.defaultValue "Invalid topology wire." parsed.Error) line.LineNumber
                        :: diagnostics.Value
                else
                    topology <- parsed.Topology
            elif text.StartsWith "forward " then
                forward <- Some(text.Substring("forward ".Length).Trim())
            elif text.StartsWith "mfd " then
                let tail = text.Substring("mfd ".Length)

                mfdZones <-
                    tail.Split('|', System.StringSplitOptions.TrimEntries ||| System.StringSplitOptions.RemoveEmptyEntries)
                    |> Array.toList
                    |> List.append mfdZones
            elif text.StartsWith "eicas " then
                eicas <- Some(text.Substring("eicas ".Length).Trim())
            else
                diagnostics.Value <-
                    AuthoringDiagnostic.create InvalidSyntax $"Unknown preset line: `{line.Text}`." line.LineNumber
                    :: diagnostics.Value

        { Name = name
          Topology = topology
          ForwardZoneId = forward
          MfdZoneIds = mfdZones
          EicasPolicy = eicas }

    and private mergeZoneBindings body zoneBindings diagnostics =
        let mutable bindings = zoneBindings

        for line in body do
            let parts = line.Text.Trim().Split('=', 2, System.StringSplitOptions.TrimEntries)

            if parts.Length <> 2 || System.String.IsNullOrWhiteSpace parts.[0] then
                diagnostics.Value <-
                    AuthoringDiagnostic.create InvalidSyntax $"Expected `zone-id = role` in zones block: `{line.Text}`." line.LineNumber
                    :: diagnostics.Value
            else
                if Map.containsKey parts.[0] bindings then
                    diagnostics.Value <-
                        AuthoringDiagnostic.create DuplicateRow $"Duplicate zone binding `{parts.[0]}`." line.LineNumber
                        :: diagnostics.Value

                bindings <- Map.add parts.[0] parts.[1] bindings

        bindings

    let parse (text: string) (_sourcePath: string option) =
        parseLines (AuthoringSource.fromText text)

    let parseFile (path: string) =
        parseLines (AuthoringSource.fromFile path)
