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

type FcsLanguageBackend() =
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

    let symbol name kind (path: string) range container =
        { Name = name
          Kind = kind
          Span = toSpan path range
          Container = container
          Children = [||] }

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
                let parseOptions =
                    { FSharpParsingOptions.Default with
                        SourceFiles = [| path |] }

                task {
                    let! parseResults = checker.ParseFile(path, SourceText.ofString source, parseOptions)

                    let diagnostics =
                        parseResults.Diagnostics
                        |> Array.map (fun (d: FSharpDiagnostic) ->
                            { Id = d.Subcategory
                              Severity = toSeverity d.Severity
                              Message = d.Message
                              Span = toSpan path d.Range
                              Tags = [||]
                              Language = LanguageIds.Fsharp })

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

        member _.GoToDefinitionAsync(_req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<LanguageNavigation>(ct)
            else
                Task.FromResult(Unchecked.defaultof<LanguageNavigation>)
