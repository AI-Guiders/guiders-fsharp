namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.DotNet

open System.IO
open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type DotNetSlnxGraphPortTests() =

    let createWorkspace () =
        let root = Path.Combine(Path.GetTempPath(), "ide-session-slnx-" + System.Guid.NewGuid().ToString("N"))
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
    member _.``Slnx port builds graph with E_proj and omega``() =
        let root = createWorkspace ()

        try
            let slnx = Path.Combine(root, "Mixed.slnx")
            let graph = DotNetSlnxGraphPort.load slnx

            Assert.Equal(2, graph.Projects.Length)
            Assert.Equal(1, graph.ProjectEdges.Length)

            let result = GraphValidation.validate graph
            Assert.True(result.IsValid, result.Issues |> List.map (fun i -> i.Message) |> String.concat "; ")

            let cs = Path.Combine(root, "app", "App.cs")
            let fs = Path.Combine(root, "lib", "Lib.fs")
            Assert.True(Map.containsKey cs graph.FileOwnership)
            Assert.True(Map.containsKey fs graph.FileOwnership)
        finally
            Directory.Delete(root, recursive = true)

    [<Fact>]
    member _.``LoadSession returns DesignTime session``() =
        let root = createWorkspace ()

        try
            let session = DotNetSlnxGraphPort.loadSession (Path.Combine(root, "Mixed.slnx"))
            Assert.Equal(DesignTime, session.Phase)
            Assert.Equal(2, session.Graph.Projects.Length)
        finally
            Directory.Delete(root, recursive = true)
