namespace AIGuiders.Platform.Modeling.Language.Tests

open System.Threading
open Xunit
open AIGuiders.Platform.Execution.Language
open AIGuiders.Platform.Modeling.Language.Adapters.Fcs
open AIGuiders.Platform.Modeling.Language.Adapters.Gdl

module LanguageAdapterSmokeTests =
    [<Fact>]
    let ``Fcs parses simple fs source`` () =
        let backend = FcsLanguageBackend() :> ILanguageBackend
        let req =
            LanguageRequest(
                FilePath = "Sample.fs",
                Line = 1,
                Column = 1,
                SourceText = "module Sample\n\nlet answer = 42")

        let result =
            backend.GetDiagnosticsAsync(req, CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.NotNull(result)
        Assert.Empty(result.Diagnostics)

        let symbols =
            backend.GetDocumentSymbolsAsync(req, CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Equal("Sample.fs", symbols.Root.Name)
        Assert.Contains(symbols.Root.Children, fun s -> s.Name = "answer" && s.Kind = "let")

    [<Fact>]
    let ``Gdl parses deck fixture`` () =
        let fixturePath =
            System.IO.Path.Combine(
                System.AppContext.BaseDirectory,
                "Fixtures",
                "Authoring",
                "dashspec-studio.deck.gdl")

        let text = System.IO.File.ReadAllText(fixturePath)
        let backend = GdlLanguageBackend() :> ILanguageBackend
        let req = LanguageRequest(fixturePath, 1, 1, text, null)

        let result =
            backend.GetDiagnosticsAsync(req, CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Empty(result.Diagnostics)

        let symbols =
            backend.GetDocumentSymbolsAsync(req, CancellationToken.None)
            |> Async.AwaitTask
            |> Async.RunSynchronously

        Assert.Single(symbols.Root.Children, fun deck -> deck.Kind = "deck" && deck.Name = "dashspec-studio")
