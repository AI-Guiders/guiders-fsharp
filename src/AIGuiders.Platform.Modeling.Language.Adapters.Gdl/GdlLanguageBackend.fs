namespace AIGuiders.Platform.Modeling.Language.Adapters.Gdl

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open AIGuiders.Platform.Modeling.Language
open AIGuiders.Platform.Execution.Language
open AIGuiders.Platform.Modeling.Gdl.Authoring
open AIGuiders.Platform.Modeling.Gdl.Parse.Deck

type GdlLanguageBackend() =
    let languageId = LanguageIds.Gdl

    let isDeckFile (path: string) =
        let fileName = Path.GetFileName(path)
        fileName.EndsWith(".deck.gdl", StringComparison.OrdinalIgnoreCase)

    let isGdlFile (path: string) =
        let ext = Path.GetExtension(path)

        ext.Equals(".gdl", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".gdlproj", StringComparison.OrdinalIgnoreCase)

    let readSource (req: LanguageRequest) =
        if not (String.IsNullOrWhiteSpace req.SourceText) then
            req.SourceText
        elif File.Exists req.FilePath then
            File.ReadAllText req.FilePath
        else
            ""

    let toSpan (path: string) line column =
        { Path = path
          Line = line
          Column = column
          EndLine = line
          EndColumn = column + 1 }

    let toDiagnostic (path: string) (diag: AuthoringDiagnostic) =
        { Id = string diag.Code
          Severity = Severity.Error
          Message = diag.Message
          Span = toSpan path diag.Line (diag.Column + 1)
          Tags = [||]
          Language = languageId }

    let zoneSymbol (path: string) name line =
        { Name = name
          Kind = "zone"
          Span = toSpan path line 1
          Container = "zones"
          Children = [||] }

    let presetSymbol (path: string) (preset: AttentionPreset) =
        { Name = preset.Name
          Kind = "preset"
          Span = toSpan path 1 1
          Container = "deck"
          Children = [||] }

    interface ILanguageBackend with
        member _.LanguageId = languageId

        member _.CanHandle(path: string, _hint) = isGdlFile path

        member _.GetDiagnosticsAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<DiagnosticsResult>(ct)
            elif not (isDeckFile req.FilePath) then
                Task.FromResult { Diagnostics = [||] }
            else
                let path = req.FilePath
                let text = readSource req
                let result = DeckParser.parse text (Some path)
                let diagnostics = result.Diagnostics |> List.map (toDiagnostic path) |> Array.ofList
                Task.FromResult { Diagnostics = diagnostics }

        member _.GetDocumentSymbolsAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<DocumentSymbolsResult>(ct)
            elif not (isDeckFile req.FilePath) then
                Task.FromResult
                    { Root =
                        { Name = Path.GetFileName req.FilePath
                          Kind = "file"
                          Span = toSpan req.FilePath 1 1
                          Container = ""
                          Children = [||] } }
            else
                let path = req.FilePath
                let text = readSource req
                let result = DeckParser.parse text (Some path)

                match result.Document with
                | None ->
                    Task.FromResult
                        { Root =
                            { Name = Path.GetFileName path
                              Kind = "file"
                              Span = toSpan path 1 1
                              Container = ""
                              Children = [||] } }
                | Some document ->
                    let presetChildren = document.Presets |> List.map (presetSymbol path) |> Array.ofList

                    let zoneChildren =
                        document.ZoneBindings
                        |> Map.toList
                        |> List.mapi (fun i (zoneId, _) -> zoneSymbol path zoneId (i + 1))
                        |> Array.ofList

                    let deckChild =
                        { Name = document.Planet
                          Kind = "deck"
                          Span = toSpan path 1 1
                          Container = ""
                          Children = Array.append presetChildren zoneChildren }

                    Task.FromResult
                        { Root =
                            { Name = Path.GetFileName path
                              Kind = "file"
                              Span = toSpan path 1 1
                              Container = ""
                              Children = [| deckChild |] } }

        member _.GoToDefinitionAsync(_req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<LanguageNavigation>(ct)
            else
                Task.FromResult(Unchecked.defaultof<LanguageNavigation>)

        member _.FindUsagesAsync(_req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<FindUsagesResult>(ct)
            else
                Task.FromResult { References = [||] }

        member _.GetCompletionsAsync(_req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<CompletionsResult>(ct)
            else
                Task.FromResult { Items = [||] }

        member _.GetSymbolAtPositionAsync(_req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<SymbolAtPositionResult>(ct)
            else
                Task.FromResult(Unchecked.defaultof<SymbolAtPositionResult>)

        member _.RenameSymbolAsync(renameReq, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<RenameSymbolResult>(ct)
            else
                Task.FromResult
                    { OldName = ""
                      NewName = renameReq.NewName
                      SymbolKind = ""
                      Applied = false
                      Message = ""
                      Files = [||]
                      Changes = [||] }
