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

    [<Fact>]
    let ``Fcs reports semantic errors with fsproj context`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-sem-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "SemProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Sem.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Sem.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(fs, "module Sem\nlet x = totallyUnknownIdentifier\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 1, 1, null, null)

            let result =
                backend.GetDiagnosticsAsync(req, System.Threading.CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.NotEmpty(result.Diagnostics)

            Assert.Contains(
                result.Diagnostics,
                fun d -> d.Severity = AIGuiders.Platform.Modeling.Language.Severity.Error)
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``Fcs go to definition resolves let binding`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-goto-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "GotoProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Goto.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Goto.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(
            fs,
            "module Goto\n\nlet answer = 42\n\nlet useIt () = answer\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 5, 16, null, fsproj)

            let nav =
                backend.GoToDefinitionAsync(req, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.NotNull(nav)
            Assert.Equal(fs, nav.Definition.Path)
            Assert.Equal(3, nav.Definition.Line)
            Assert.Contains(nav.Declarations, fun span -> span.Line = 3 && span.Path = fs)
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)
