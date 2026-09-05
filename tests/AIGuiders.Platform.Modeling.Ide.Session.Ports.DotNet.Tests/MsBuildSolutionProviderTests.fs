namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open System.IO
open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type MsBuildSolutionProviderTests() =

    let createWorkspace () =
        let root = Path.Combine(Path.GetTempPath(), "ide-session-provider-" + System.Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(Path.Combine(root, "app")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "lib")) |> ignore

        File.WriteAllText(
            Path.Combine(root, "app", "App.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup><ProjectReference Include="..\lib\Lib.fsproj" /></ItemGroup>
            </Project>
            """
        )

        File.WriteAllText(Path.Combine(root, "app", "App.cs"), "namespace App; public class X {}")

        File.WriteAllText(
            Path.Combine(root, "lib", "Lib.fsproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup><Compile Include="Lib.fs" /></ItemGroup>
            </Project>
            """
        )

        File.WriteAllText(Path.Combine(root, "lib", "Lib.fs"), "module Lib\nlet x = 1")

        File.WriteAllText(
            Path.Combine(root, "Mixed.slnx"),
            """
            <Solution>
              <Folder Name="/app/">
                <Project Path="app/App.csproj" />
              </Folder>
              <Folder Name="/lib/">
                <Project Path="lib/Lib.fsproj" />
              </Folder>
            </Solution>
            """
        )

        root

    [<Fact>]
    member _.``Provider contract: name, entries, relations feed the graph``() =
        let root = createWorkspace ()

        try
            let provider =
                MsBuildSolutionProvider(Path.Combine(root, "Mixed.slnx"))
                :> ISolutionInfoProvider

            Assert.Equal("msbuild", provider.Name)
            Assert.Equal(2, provider.Entries().Length)
            Assert.Equal(1, provider.Relations().Length)

            let validation =
                SolutionProviders.toGraph (Path.Combine(root, "Mixed.slnx")) provider
                |> GraphValidation.validate

            Assert.True(validation.IsValid, validation.Issues |> List.map (fun i -> i.Message) |> String.concat "; ")
        finally
            Directory.Delete(root, true)

    [<Fact>]
    member _.``Fingerprint is stable within a run``() =
        let root = createWorkspace ()

        try
            let provider = MsBuildSolutionProvider(Path.Combine(root, "Mixed.slnx")) :> ISolutionInfoProvider
            Assert.Equal(provider.Fingerprint(), provider.Fingerprint())
        finally
            Directory.Delete(root, true)
    [<Fact>]
    member _.``Provider self-registered in the plugin catalog (ADR-0210 stage 1)``() =
        Assert.Equal("msbuild", Registration.name)

        Registration.init ()

        Assert.Contains("msbuild", SolutionProviderRegistry.names ())

