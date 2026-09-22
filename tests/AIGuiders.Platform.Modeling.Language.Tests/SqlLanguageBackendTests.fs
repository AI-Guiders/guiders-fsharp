namespace AIGuiders.Platform.Modeling.Language.Tests

open System.Threading
open Xunit
open AIGuiders.Platform.Modeling.Language
open AIGuiders.Platform.Modeling.Language.Adapters.Sql

module SqlLanguageBackendTests =
    let private request path text =
        { FilePath = path
          Line = 1
          Column = 1
          SourceText = text
          SolutionOrProjectPath = "" }

    [<Fact>]
    let ``Postgres backend rejects SELECT TOP`` () =
        let backend = SqlPostgresLanguageBackend() :> ILanguageBackend

        let result =
            backend.GetDiagnosticsAsync(
                request "report.sql" "SELECT TOP 10 * FROM users",
                CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Contains(result.Diagnostics, fun diagnostic ->
            diagnostic.Severity = Error
            && diagnostic.Message.Contains("TOP"))

    [<Fact>]
    let ``Mssql backend rejects trailing LIMIT`` () =
        let backend = SqlMssqlLanguageBackend() :> ILanguageBackend

        let result =
            backend.GetDiagnosticsAsync(
                request "report.sql" "SELECT * FROM users LIMIT 10",
                CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Contains(result.Diagnostics, fun diagnostic ->
            diagnostic.Severity = Error
            && diagnostic.Message.Contains("LIMIT"))

    [<Fact>]
    let ``Valid create table produces statement symbols`` () =
        let backend = SqlLanguageBackend() :> ILanguageBackend

        let symbols =
            backend.GetDocumentSymbolsAsync(
                request "001_init.sql" "CREATE TABLE demo (id INT PRIMARY KEY);",
                CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Equal(1, symbols.Root.Children.Length)
        Assert.StartsWith("create", symbols.Root.Children.[0].Kind)

    [<Fact>]
    let ``Parse error is reported for broken sql`` () =
        let backend = SqlLanguageBackend() :> ILanguageBackend

        let result =
            backend.GetDiagnosticsAsync(
                request "broken.sql" "SELECT FROM",
                CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.NotEmpty(result.Diagnostics)
        Assert.All(result.Diagnostics, fun diagnostic -> Assert.Equal(Error, diagnostic.Severity))
