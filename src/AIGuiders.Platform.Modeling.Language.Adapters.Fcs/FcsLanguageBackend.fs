namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Diagnostics
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open AIGuiders.Platform.Modeling.Language
open AIGuiders.Platform.Execution.Language

type FcsLanguageBackend(?projectOptionsSource: IFcsProjectOptionsSource) =
    let projectOptionsSource =
        defaultArg projectOptionsSource FcsProjectOptions.Default

    let checker = FSharpChecker.Create(keepAssemblyContents = false)

    let toSpan (path: string) (range: range) =
        { Path = path
          Line = range.StartLine
          Column = range.StartColumn + 1
          EndLine = range.EndLine
          EndColumn = range.EndColumn + 1 }

    let toSeverity (severity: FSharpDiagnosticSeverity) =
        match severity with
        | FSharpDiagnosticSeverity.Error -> Severity.Error
        | FSharpDiagnosticSeverity.Warning -> Severity.Warning
        | _ -> Severity.Info

    let readSource (req: LanguageRequest) =
        if not (String.IsNullOrWhiteSpace req.SourceText) then
            req.SourceText
        elif File.Exists req.FilePath then
            File.ReadAllText req.FilePath
        else
            ""

    let toDiagnostic (path: string) (d: FSharpDiagnostic) =
        { Id = d.Subcategory
          Severity = toSeverity d.Severity
          Message = d.Message
          Span = toSpan path d.Range
          Tags = [||]
          Language = LanguageIds.Fsharp }

    let collectCheckDiagnostics checkAnswer =
        match checkAnswer with
        | FSharpCheckFileAnswer.Succeeded results -> results.Diagnostics
        | FSharpCheckFileAnswer.Aborted -> Array.empty

    let getDiagnosticsFromParse (parseResults: FSharpParseFileResults) path =
        parseResults.Diagnostics
        |> Array.map (fun (d: FSharpDiagnostic) -> toDiagnostic path d)

    let getDiagnosticsFromCheck parseResults checkAnswer path =
        Array.append
            (getDiagnosticsFromParse parseResults path)
            (collectCheckDiagnostics checkAnswer |> Array.map (fun d -> toDiagnostic path d))

    let symbol name kind (path: string) range container =
        { Name = name
          Kind = kind
          Span = toSpan path range
          Container = container
          Children = [||] }

    let getLineText (source: string) (line: int) =
        let lines = source.Replace("\r\n", "\n").Split('\n')
        let index = line - 1

        if index >= 0 && index < lines.Length then
            lines.[index]
        else
            ""

    let isIdentChar (c: char) =
        System.Char.IsLetterOrDigit c || c = '_' || c = '\''

    let tryExtractIdentifier (line: string) (column: int) =
        if String.IsNullOrEmpty line then
            None
        else
            let col0 = min (max 0 (column - 1)) (line.Length - 1)

            let mutable start = col0
            let mutable end_ = col0

            while start > 0 && isIdentChar line.[start - 1] do
                start <- start - 1

            while end_ < line.Length - 1 && isIdentChar line.[end_ + 1] do
                end_ <- end_ + 1

            if start <= end_ && isIdentChar line.[start] then
                Some(line.Substring(start, end_ - start + 1), end_ + 1)
            else
                None

    let tryNavigationFromCheck (path: string) (checkAnswer: FSharpCheckFileAnswer) (line: int) (column: int) (source: string) =
        match checkAnswer with
        | FSharpCheckFileAnswer.Succeeded checkResults ->
            let lineStr = getLineText source line

            match tryExtractIdentifier lineStr column with
            | None -> None
            | Some (name, colAtEnd) ->
                match checkResults.GetSymbolUseAtLocation(line, colAtEnd, lineStr, [ name ]) with
                | None -> None
                | Some symbolUse ->
                    let declOpt =
                        symbolUse.Symbol.SignatureLocation
                        |> Option.orElse symbolUse.Symbol.DeclarationLocation

                    match declOpt with
                    | None -> None
                    | Some decl ->
                        if String.IsNullOrWhiteSpace decl.FileName && decl.StartLine <= 0 then
                            None
                        else
                            let declPath =
                                if String.IsNullOrWhiteSpace decl.FileName then
                                    path
                                else
                                    decl.FileName

                            let span = toSpan declPath decl
                            Some { Definition = span; Declarations = [| span |] }
        | FSharpCheckFileAnswer.Aborted -> None

    let collectSymbols (path: string) (parseResults: FSharpParseFileResults) =
        let collected = ResizeArray<LanguageSymbol>()

        let visitBinding (container: string) (binding: SynBinding) =
            match binding with
            | SynBinding(headPat = SynPat.Named(ident = SynIdent(ident = ident)); range = range) ->
                collected.Add(symbol ident.idText "let" path range container)
            | _ -> ()

        let rec visitDecl (container: string) (decl: SynModuleDecl) =
            match decl with
            | SynModuleDecl.Let(bindings = bindings) ->
                bindings |> List.iter (visitBinding container)
            | SynModuleDecl.NestedModule(decls = decls) ->
                decls |> List.iter (visitDecl container)
            | SynModuleDecl.Types(typeDefns = typeDefns) ->
                typeDefns
                |> List.iter (fun (SynTypeDefn(typeInfo = SynComponentInfo(longId = idents; range = range))) ->
                    let name =
                        match idents with
                        | [] -> "_"
                        | id :: _ -> id.idText

                    collected.Add(symbol name "type" path range container))
            | _ -> ()

        match parseResults.ParseTree with
        | ParsedInput.ImplFile(ParsedImplFileInput(contents = modules)) ->
            for SynModuleOrNamespace(longId = idents; decls = decls) in modules do
                let container =
                    match idents with
                    | [] -> Path.GetFileNameWithoutExtension path |> Option.ofObj |> Option.defaultValue path
                    | head :: _ -> head.idText

                for decl in decls do
                    visitDecl container decl
        | ParsedInput.SigFile _ -> ()

        collected.ToArray()

    let symbolKind (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpEntity -> "type"
        | :? FSharpMemberOrFunctionOrValue as m when m.IsProperty -> "property"
        | :? FSharpMemberOrFunctionOrValue as m when m.IsModuleValueOrMember -> "value"
        | :? FSharpMemberOrFunctionOrValue as m when m.IsConstructor -> "constructor"
        | :? FSharpMemberOrFunctionOrValue -> "member"
        | _ -> "symbol"

    let qualifiedName (symbol: FSharpSymbol) =
        let name = symbol.FullName
        if String.IsNullOrWhiteSpace name then symbol.DisplayName else name

    let trySymbolUseAt (path: string) (checkResults: FSharpCheckFileResults) (line: int) (column: int) (source: string) =
        let lineStr = getLineText source line

        match tryExtractIdentifier lineStr column with
        | None -> None
        | Some (name, colAtEnd) ->
            checkResults.GetSymbolUseAtLocation(line, colAtEnd, lineStr, [ name ])

    let definitionSpan (path: string) (symbolUse: FSharpSymbolUse) =
        let declOpt =
            symbolUse.Symbol.DeclarationLocation
            |> Option.orElse symbolUse.Symbol.SignatureLocation

        match declOpt with
        | Some decl when not (String.IsNullOrWhiteSpace decl.FileName) || decl.StartLine > 0 ->
            let declPath =
                if String.IsNullOrWhiteSpace decl.FileName then
                    path
                else
                    decl.FileName

            toSpan declPath decl
        | _ -> toSpan path symbolUse.Range

    let referenceFromUse (path: string) (target: SourceSpan) (symbolUse: FSharpSymbolUse) =
        let usePath =
            if String.IsNullOrWhiteSpace symbolUse.FileName then
                path
            else
                symbolUse.FileName

        { Span = toSpan usePath symbolUse.Range
          Target = target
          Kind = "reference" }

    let readFileText (path: string) (preferredSource: string) =
        if not (String.IsNullOrWhiteSpace preferredSource) then
            preferredSource
        elif File.Exists path then
            File.ReadAllText path
        else
            ""

    let replaceRangeInSource (source: string) (range: range) (replacement: string) =
        let normalized = source.Replace("\r\n", "\n")
        let lines = normalized.Split('\n')
        let lineIdx = range.StartLine - 1

        if lineIdx < 0 || lineIdx >= lines.Length then
            source
        else
            let line = lines.[lineIdx]
            let startCol = max 0 range.StartColumn
            let length = max 0 (range.EndColumn - range.StartColumn + 1)
            let endExclusive = min line.Length (startCol + length)

            let newLine =
                if startCol >= line.Length then
                    line + replacement
                else
                    line.Substring(0, startCol) + replacement + line.Substring(endExclusive)

            lines.[lineIdx] <- newLine
            String.Join(Environment.NewLine, lines)

    let completionKind glyph =
        match glyph with
        | FSharpGlyph.Class
        | FSharpGlyph.Struct
        | FSharpGlyph.Interface
        | FSharpGlyph.Enum
        | FSharpGlyph.EnumMember
        | FSharpGlyph.Delegate
        | FSharpGlyph.Typedef -> "type"
        | FSharpGlyph.Method
        | FSharpGlyph.OverridenMethod -> "method"
        | FSharpGlyph.Property -> "property"
        | FSharpGlyph.Field -> "field"
        | FSharpGlyph.Event -> "event"
        | FSharpGlyph.NameSpace -> "namespace"
        | FSharpGlyph.Variable -> "variable"
        | FSharpGlyph.Constant -> "constant"
        | FSharpGlyph.Union -> "union"
        | FSharpGlyph.ExtensionMethod -> "extension"
        | _ -> "text"

    let emptyRename newName =
        { OldName = ""
          NewName = newName
          SymbolKind = ""
          Applied = false
          Files = [||]
          Changes = [||] }

    let loadProjectContext (path: string) (source: string) (sourceText: ISourceText) (req: LanguageRequest) =
        task {
            let ext = Path.GetExtension(path)

            if ext.Equals(".fsx", StringComparison.OrdinalIgnoreCase) then
                let! projectOptions, _scriptDiags =
                    checker.GetProjectOptionsFromScript(path, sourceText, assumeDotNetFramework = false)

                let! projectResults = checker.ParseAndCheckProject(projectOptions)
                let! parseResults, checkAnswer = checker.ParseAndCheckFileInProject(path, 0, sourceText, projectOptions)
                return Some(projectOptions, projectResults, parseResults, checkAnswer)
            else
                match
                    FcsProjectResolver.resolveFsproj path req.SolutionOrProjectPath
                    |> Option.bind (fun fsproj ->
                        match projectOptionsSource.TryLoad fsproj with
                        | Result.Ok options -> Some options
                        | Result.Error _ -> None)
                with
                | Some projectOptions ->
                    let! projectResults = checker.ParseAndCheckProject(projectOptions)
                    let! parseResults, checkAnswer = checker.ParseAndCheckFileInProject(path, 0, sourceText, projectOptions)
                    return Some(projectOptions, projectResults, parseResults, checkAnswer)
                | None -> return None
        }

    interface ILanguageBackend with
        member _.LanguageId = LanguageIds.Fsharp

        member _.CanHandle(path: string, _hint) =
            match Path.GetExtension(path) with
            | ".fs"
            | ".fsproj"
            | ".fsx" -> true
            | _ -> false

        member _.GetDiagnosticsAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<DiagnosticsResult>(ct)
            else
                let path = req.FilePath
                let source = readSource req
                let sourceText = SourceText.ofString source

                task {
                    let ext = Path.GetExtension(path)

                    if ext.Equals(".fsx", StringComparison.OrdinalIgnoreCase) then
                        let! projectOptions, _scriptDiags =
                            checker.GetProjectOptionsFromScript(path, sourceText, assumeDotNetFramework = false)

                        let! parseResults, checkAnswer =
                            checker.ParseAndCheckFileInProject(path, 0, sourceText, projectOptions)

                        let diagnostics = getDiagnosticsFromCheck parseResults checkAnswer path
                        return { Diagnostics = diagnostics }
                    else
                        match
                            FcsProjectResolver.resolveFsproj path req.SolutionOrProjectPath
                            |> Option.bind (fun fsproj ->
                                match projectOptionsSource.TryLoad fsproj with
                                | Result.Ok options -> Some options
                                | Result.Error _ -> None)
                        with
                        | Some projectOptions ->
                            let! parseResults, checkAnswer =
                                checker.ParseAndCheckFileInProject(path, 0, sourceText, projectOptions)

                            let diagnostics = getDiagnosticsFromCheck parseResults checkAnswer path
                            return { Diagnostics = diagnostics }
                        | None ->
                            let parseOptions =
                                { FSharpParsingOptions.Default with
                                    SourceFiles = [| path |] }

                            let! parseResults = checker.ParseFile(path, sourceText, parseOptions)
                            let diagnostics = getDiagnosticsFromParse parseResults path
                            return { Diagnostics = diagnostics }
                }

        member _.GetDocumentSymbolsAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<DocumentSymbolsResult>(ct)
            else
                let path = req.FilePath
                let source = readSource req
                let parseOptions =
                    { FSharpParsingOptions.Default with
                        SourceFiles = [| path |] }

                task {
                    let! parseResults = checker.ParseFile(path, SourceText.ofString source, parseOptions)
                    let children = collectSymbols path parseResults
                    let fileName = Path.GetFileName path |> Option.ofObj |> Option.defaultValue path

                    return
                        { Root =
                            { Name = fileName
                              Kind = "file"
                              Span =
                                { Path = path
                                  Line = 1
                                  Column = 1
                                  EndLine = 1
                                  EndColumn = 1 }
                              Container = ""
                              Children = children } }
                }

        member _.GoToDefinitionAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<LanguageNavigation>(ct)
            else
                let path = req.FilePath
                let source = readSource req
                let sourceText = SourceText.ofString source

                task {
                    let ext = Path.GetExtension(path)

                    let! navigation =
                        if ext.Equals(".fsx", StringComparison.OrdinalIgnoreCase) then
                            task {
                                let! projectOptions, _scriptDiags =
                                    checker.GetProjectOptionsFromScript(path, sourceText, assumeDotNetFramework = false)

                                let! _, checkAnswer =
                                    checker.ParseAndCheckFileInProject(path, 0, sourceText, projectOptions)

                                return tryNavigationFromCheck path checkAnswer req.Line req.Column source
                            }
                        else
                            match
                                FcsProjectResolver.resolveFsproj path req.SolutionOrProjectPath
                                |> Option.bind (fun fsproj ->
                                    match projectOptionsSource.TryLoad fsproj with
                                    | Result.Ok options -> Some options
                                    | Result.Error _ -> None)
                            with
                            | Some projectOptions ->
                                task {
                                    let! _, checkAnswer =
                                        checker.ParseAndCheckFileInProject(path, 0, sourceText, projectOptions)

                                    return tryNavigationFromCheck path checkAnswer req.Line req.Column source
                                }
                            | None -> Task.FromResult(None)

                    return
                        match navigation with
                        | Some nav -> nav
                        | None -> Unchecked.defaultof<LanguageNavigation>
                }

        member _.FindUsagesAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<FindUsagesResult>(ct)
            else
                let path = req.FilePath
                let source = readSource req
                let sourceText = SourceText.ofString source

                task {
                    let! contextOpt = loadProjectContext path source sourceText req

                    match contextOpt with
                    | None -> return { References = [||] }
                    | Some(_, projectResults, _, checkAnswer) ->
                        match checkAnswer with
                        | FSharpCheckFileAnswer.Aborted -> return { References = [||] }
                        | FSharpCheckFileAnswer.Succeeded checkResults ->
                            match trySymbolUseAt path checkResults req.Line req.Column source with
                            | None -> return { References = [||] }
                            | Some symbolUse ->
                                let target = definitionSpan path symbolUse
                                let references =
                                    projectResults.GetUsesOfSymbol symbolUse.Symbol
                                    |> Array.map (fun use' -> referenceFromUse path target use')

                                return { References = references }
                }

        member _.GetCompletionsAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<CompletionsResult>(ct)
            else
                let path = req.FilePath
                let source = readSource req
                let sourceText = SourceText.ofString source

                task {
                    let! contextOpt = loadProjectContext path source sourceText req

                    match contextOpt with
                    | None -> return { Items = [||] }
                    | Some(_, _, parseResults, checkAnswer) ->
                        match checkAnswer with
                        | FSharpCheckFileAnswer.Aborted -> return { Items = [||] }
                        | FSharpCheckFileAnswer.Succeeded checkResults ->
                            let lineStr = getLineText source req.Line
                            let col0 = max 0 (req.Column - 1)
                            let partialLongName = QuickParse.GetPartialLongNameEx(lineStr, col0)

                            let decls =
                                checkResults.GetDeclarationListInfo(
                                    Some parseResults,
                                    req.Line,
                                    lineStr,
                                    partialLongName,
                                    (fun () -> []))

                            let items =
                                decls.Items
                                |> Array.map (fun item ->
                                    { Label = item.NameInList
                                      Kind = completionKind item.Glyph
                                      Detail = ""
                                      InsertText = item.NameInList })

                            return { Items = items }
                }

        member _.GetSymbolAtPositionAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<SymbolAtPositionResult>(ct)
            else
                let path = req.FilePath
                let source = readSource req
                let sourceText = SourceText.ofString source

                task {
                    let! contextOpt = loadProjectContext path source sourceText req

                    match contextOpt with
                    | None -> return Unchecked.defaultof<SymbolAtPositionResult>
                    | Some(_, _, _, checkAnswer) ->
                        match checkAnswer with
                        | FSharpCheckFileAnswer.Aborted -> return Unchecked.defaultof<SymbolAtPositionResult>
                        | FSharpCheckFileAnswer.Succeeded checkResults ->
                            match trySymbolUseAt path checkResults req.Line req.Column source with
                            | None -> return Unchecked.defaultof<SymbolAtPositionResult>
                            | Some symbolUse ->
                                let span = toSpan path symbolUse.Range

                                return
                                    { Kind = symbolKind symbolUse.Symbol
                                      Name = symbolUse.Symbol.DisplayName
                                      QualifiedName = qualifiedName symbolUse.Symbol
                                      Span = span }
                }

        member _.RenameSymbolAsync(renameReq, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<RenameSymbolResult>(ct)
            else
                let req = renameReq.Request
                let newName = renameReq.NewName
                let apply = renameReq.Apply

                if String.IsNullOrWhiteSpace newName then
                    Task.FromResult(emptyRename newName)
                else
                    let path = req.FilePath
                    let source = readSource req
                    let sourceText = SourceText.ofString source

                    task {
                        let! contextOpt = loadProjectContext path source sourceText req

                        match contextOpt with
                        | None -> return emptyRename newName
                        | Some(_, projectResults, _, checkAnswer) ->
                            match checkAnswer with
                            | FSharpCheckFileAnswer.Aborted -> return emptyRename newName
                            | FSharpCheckFileAnswer.Succeeded checkResults ->
                                match trySymbolUseAt path checkResults req.Line req.Column source with
                                | None -> return emptyRename newName
                                | Some symbolUse ->
                                    let oldName = symbolUse.Symbol.DisplayName
                                    let kind = symbolKind symbolUse.Symbol

                                    let uses =
                                        projectResults.GetUsesOfSymbol symbolUse.Symbol
                                        |> Array.distinctBy (fun use' -> use'.Range, use'.FileName)

                                    let grouped =
                                        uses
                                        |> Array.groupBy (fun use' ->
                                            if String.IsNullOrWhiteSpace use'.FileName then
                                                path
                                            else
                                                use'.FileName)

                                    let mutable changes = ResizeArray<RenameFileChange>()

                                    for filePath, fileUses in grouped do
                                        let fileSource =
                                            if String.Equals(filePath, path, StringComparison.OrdinalIgnoreCase) then
                                                source
                                            else
                                                readFileText filePath ""

                                        let mutable updated = fileSource

                                        for use' in fileUses |> Array.sortByDescending (fun u -> u.Range.StartLine, u.Range.StartColumn) do
                                            updated <- replaceRangeInSource updated use'.Range newName

                                        if not (String.Equals(updated, fileSource, StringComparison.Ordinal)) then
                                            changes.Add({ Path = filePath; NewText = updated })

                                            if apply then
                                                File.WriteAllText(filePath, updated)

                                    let filePaths = changes |> Seq.map (fun c -> c.Path) |> Array.ofSeq

                                    return
                                        { OldName = oldName
                                          NewName = newName
                                          SymbolKind = kind
                                          Applied = apply
                                          Files = filePaths
                                          Changes = changes.ToArray() }
                    }
