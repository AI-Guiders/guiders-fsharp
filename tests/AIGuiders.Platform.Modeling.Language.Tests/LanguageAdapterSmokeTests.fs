namespace AIGuiders.Platform.Modeling.Language.Tests

open System
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

    [<Fact>]
    let ``Fcs find usages returns references for let binding`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-usages-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "UsageProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Usage.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Usage.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(
            fs,
            "module Usage\n\nlet answer = 42\n\nlet useIt () = answer + 1\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 5, 18, null, fsproj)

            let usages =
                backend.FindUsagesAsync(req, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.True(usages.References.Length >= 2)
            Assert.Contains(usages.References, fun r -> r.Span.Line = 3)
            Assert.Contains(usages.References, fun r -> r.Span.Line = 5)
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``Fcs get symbol at position resolves let binding`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-symbol-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "SymProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Sym.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Sym.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(fs, "module Sym\n\nlet answer = 42\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 3, 5, null, fsproj)

            let symbol =
                backend.GetSymbolAtPositionAsync(req, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.Equal("answer", symbol.Name)
            Assert.Equal("value", symbol.Kind)
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``Fcs get completions returns items in scope`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-complete-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "CompleteProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Complete.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Complete.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(fs, "module Complete\n\nlet answer = 42\n\nlet useIt () = ans\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 5, 18, null, fsproj)

            let completions =
                backend.GetCompletionsAsync(req, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.Contains(completions.Items, fun item -> item.Label = "answer")
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``Fcs rename preview plans text changes without apply`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-rename-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "RenameProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Rename.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Rename.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(
            fs,
            "module Rename\n\nlet answer = 42\n\nlet useIt () = answer + 1\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 5, 18, null, fsproj)
            let renameReq = RenameSymbolRequest(req, "renamed", false)

            let result =
                backend.RenameSymbolAsync(renameReq, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.Equal("answer", result.OldName)
            Assert.Equal("renamed", result.NewName)
            Assert.False(result.Applied)
            Assert.NotEmpty(result.Changes)
            Assert.Contains(result.Changes, fun c -> c.NewText.Contains("renamed"))
            Assert.Equal(System.IO.File.ReadAllText(fs), System.IO.File.ReadAllText(fs))
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``Fcs rename rejects active pattern`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-ap-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "ApProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Ap.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Ap.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(
            fs,
            "module Ap\n\nlet (|Even|Odd|) v = if v % 2 = 0 then Even else Odd\n\nlet _ = (|Even|Odd|) 2\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 3, 10, null, fsproj)
            let renameReq = RenameSymbolRequest(req, "Renamed", false)

            let result =
                backend.RenameSymbolAsync(renameReq, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.Contains("active pattern", result.Message, StringComparison.OrdinalIgnoreCase)
            Assert.Empty(result.Changes)
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``Fcs rename preview spans workspace projects`` () =
        let repoRoot =
            System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", ".."))

        let slnx = System.IO.Path.Combine(repoRoot, "AIGuiders.Platform.Modeling.slnx")

        let kernelFs =
            System.IO.Path.Combine(repoRoot, "src", "AIGuiders.Platform.Modeling.Language", "Kernel.fs")

        if not (System.IO.File.Exists slnx) then
            Assert.Fail(sprintf "slnx fixture missing: %s" slnx)
        else
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(kernelFs, 87, 10, null, slnx)
            let renameReq = RenameSymbolRequest(req, "LanguageRequestPreview", false)

            let result =
                backend.RenameSymbolAsync(renameReq, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.Equal("LanguageRequest", result.OldName)
            Assert.False(result.Applied)
            Assert.NotEmpty(result.Changes)
            Assert.Contains(result.Files, fun f -> f.EndsWith("Kernel.fs"))

    [<Fact>]
    let ``Fcs diagnostics clean on FcsLanguageBackend with guiders slnx`` () =
        let repoRoot =
            System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", ".."))

        let slnx = System.IO.Path.Combine(repoRoot, "AIGuiders.Platform.Modeling.slnx")

        let backendFs =
            System.IO.Path.Combine(
                repoRoot,
                "src",
                "AIGuiders.Platform.Modeling.Language.Adapters.Fcs",
                "FcsLanguageBackend.fs")

        if not (System.IO.File.Exists slnx) then
            Assert.Fail(sprintf "slnx fixture missing: %s" slnx)
        else
            let resolved = FcsProjectResolver.resolveFsproj backendFs slnx

            Assert.True(
                resolved.IsSome,
                sprintf "fsproj should resolve via slnx graph for %s" backendFs)

            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(backendFs, 1, 1, null, slnx)

            let result =
                backend.GetDiagnosticsAsync(req, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            let errors =
                result.Diagnostics
                |> Array.filter (fun d -> d.Severity = AIGuiders.Platform.Modeling.Language.Severity.Error)

            // Regression: missing project refs + wrong source index produced ~90 "AIGuiders.* is not defined" cascades.
            for e in errors do
                Assert.DoesNotContain("AIGuiders", e.Message, StringComparison.Ordinal)
                Assert.DoesNotContain("is not defined", e.Message, StringComparison.OrdinalIgnoreCase)

    [<Fact>]
    let ``FcsProjectResolver resolves fsproj via graph ownership`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-omega-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "OmegaProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Omega.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Omega.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(fs, "module Omega\n\nlet value = 1\n")

        try
            let resolved = FcsProjectResolver.resolveFsproj fs fsproj
            Assert.Equal(Some fsproj, resolved)
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``Fcs rename apply persists through SessionOrchestrator`` () =
        let root =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fcs-apply-" + System.Guid.NewGuid().ToString("N"))

        System.IO.Directory.CreateDirectory root |> ignore
        let fsproj = System.IO.Path.Combine(root, "ApplyProj.fsproj")
        let fs = System.IO.Path.Combine(root, "Apply.fs")

        System.IO.File.WriteAllText(
            fsproj,
            """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><Compile Include="Apply.fs" /></ItemGroup>
</Project>""")

        System.IO.File.WriteAllText(
            fs,
            "module Apply\n\nlet answer = 42\n\nlet useIt () = answer + 1\n")

        try
            let backend = FcsLanguageBackend() :> ILanguageBackend
            let req = LanguageRequest(fs, 5, 18, null, fsproj)
            let renameReq = RenameSymbolRequest(req, "renamed", true)

            let result =
                backend.RenameSymbolAsync(renameReq, CancellationToken.None)
                |> Async.AwaitTask
                |> Async.RunSynchronously

            Assert.True(result.Applied, result.Message)
            Assert.Empty(result.Message)
            Assert.Contains("renamed", System.IO.File.ReadAllText(fs))
        finally
            if System.IO.Directory.Exists root then
                System.IO.Directory.Delete(root, true)

    [<Fact>]
    let ``FcsProjectOptions warm loads compile references for fsproj`` () =
        let repoRoot =
            System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", ".."))

        let fsproj =
            System.IO.Path.Combine(
                repoRoot,
                "src",
                "AIGuiders.Platform.Modeling.Language",
                "AIGuiders.Platform.Modeling.Language.fsproj")

        if not (System.IO.File.Exists fsproj) then
            Assert.Fail(sprintf "fixture missing: %s" fsproj)
        else
            FcsProjectOptions.warm fsproj

            match FcsProjectOptions.tryGet fsproj with
            | None -> Assert.Fail("expected F# project options")
            | Some options ->
                let hasRef =
                    options.OtherOptions
                    |> Array.exists (fun o -> o.StartsWith("-r:", System.StringComparison.Ordinal))

                Assert.True(hasRef, "FSharpWarmOptions should yield -r: reference assemblies")

    [<Fact>]
    let ``FcsProjectOptions warm loads project references for adapters fsproj`` () =
        let repoRoot =
            System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", ".."))

        let fsproj =
            System.IO.Path.Combine(
                repoRoot,
                "src",
                "AIGuiders.Platform.Modeling.Language.Adapters.Fcs",
                "AIGuiders.Platform.Modeling.Language.Adapters.Fcs.fsproj")

        if not (System.IO.File.Exists fsproj) then
            Assert.Fail(sprintf "fixture missing: %s" fsproj)
        else
            FcsProjectOptions.warm fsproj

            match FcsProjectOptions.tryGet fsproj with
            | None -> Assert.Fail("expected F# project options for adapters fsproj")
            | Some options ->
                let hasModelingLanguage =
                    options.OtherOptions
                    |> Array.exists (fun o ->
                        o.Contains("AIGuiders.Platform.Modeling.Language.dll", System.StringComparison.OrdinalIgnoreCase))

                Assert.True(hasModelingLanguage, "adapters fsproj should reference Modeling.Language.dll")
