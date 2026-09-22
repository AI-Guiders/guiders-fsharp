namespace AIGuiders.Platform.Modeling.Language.Adapters.Sql

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open AIGuiders.Platform.Modeling.Language
open OutWit.Database.Parser
open OutWit.Database.Parser.Statements

module private SqlLanguageBackendCore =
    let readSource (req: LanguageRequest) =
        if String.IsNullOrWhiteSpace req.SourceText then "" else req.SourceText

    let toSpan (path: string) line column endLine endColumn =
        { Path = path
          Line = max 1 line
          Column = max 1 column
          EndLine = max 1 endLine
          EndColumn = max 1 endColumn }

    let parseDiagnostic (path: string) (languageId: string) (error: WitSqlParsingError) =
        { Id = "sql.parse"
          Severity = Error
          Message = error.Message
          Span = toSpan path error.Line error.Column error.Line (error.Column + 1)
          Tags = [| "sql.script"; "parse" |]
          Language = languageId }

    let statementKind (statement: WitSqlStatement) =
        let name = statement.GetType().Name

        if name.StartsWith("WitSqlStatement", StringComparison.Ordinal) then
            name.Substring("WitSqlStatement".Length).ToLowerInvariant()
        else
            name.ToLowerInvariant()

    let statementSymbol (path: string) (index: int) (statement: WitSqlStatement) =
        let kind = statementKind statement
        let line = max 1 statement.Line
        let column = max 1 statement.Column

        { Name = $"{kind}_{index}"
          Kind = kind
          Span = toSpan path line column line (column + 1)
          Container = "script"
          Children = [||] }

type SqlLanguageBackendImpl(languageId: string, dialect: SqlDialect, canHandleFn: string -> ProjectHint -> bool) =
    let canHandle = canHandleFn

    interface ILanguageBackend with
        member _.LanguageId = languageId

        member _.CanHandle(path, hint) = canHandle path hint

        member _.GetDiagnosticsAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<DiagnosticsResult>(ct)
            else
                let path = req.FilePath
                let text = SqlLanguageBackendCore.readSource req
                let parseResult = WitSql.TryParse text

                let parseDiagnostics =
                    parseResult.Errors
                    |> Seq.map (SqlLanguageBackendCore.parseDiagnostic path languageId)
                    |> Array.ofSeq

                let lawDiagnostics =
                    SqlInvariantLaws.evaluate dialect path languageId text |> List.toArray

                Task.FromResult
                    { Diagnostics = Array.append parseDiagnostics lawDiagnostics }

        member _.GetDocumentSymbolsAsync(req, ct) =
            if ct.IsCancellationRequested then
                Task.FromCanceled<DocumentSymbolsResult>(ct)
            else
                let path = req.FilePath
                let text = SqlLanguageBackendCore.readSource req
                let parseResult = WitSql.TryParse text

                let children =
                    parseResult.Statements
                    |> Seq.mapi (fun index statement -> SqlLanguageBackendCore.statementSymbol path (index + 1) statement)
                    |> Array.ofSeq

                Task.FromResult
                    { Root =
                        { Name = Path.GetFileName path
                          Kind = "file"
                          Span = SqlLanguageBackendCore.toSpan path 1 1 1 1
                          Container = ""
                          Children = children } }

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

type SqlLanguageBackend() =
    inherit SqlLanguageBackendImpl(LanguageIds.Sql, SqlDialect.Generic, SqlLanguageActivation.canHandleGeneric)

type SqlPostgresLanguageBackend() =
    inherit SqlLanguageBackendImpl(LanguageIds.SqlPostgres, SqlDialect.Postgres, SqlLanguageActivation.canHandlePostgres)

type SqlMssqlLanguageBackend() =
    inherit SqlLanguageBackendImpl(LanguageIds.SqlMssql, SqlDialect.Mssql, SqlLanguageActivation.canHandleMssql)

type SqlSqliteLanguageBackend() =
    inherit SqlLanguageBackendImpl(LanguageIds.SqlSqlite, SqlDialect.Sqlite, SqlLanguageActivation.canHandleSqlite)
