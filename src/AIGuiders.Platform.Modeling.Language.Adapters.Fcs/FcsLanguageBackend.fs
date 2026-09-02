namespace AIGuiders.Platform.Modeling.Language.Adapters.Fcs

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Diagnostics
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
