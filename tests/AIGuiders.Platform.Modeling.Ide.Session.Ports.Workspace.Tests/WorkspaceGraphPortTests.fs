namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.Workspace

open System.IO
open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type WorkspaceGraphPortTests() =

    let createTree () =
        let root = Path.Combine(Path.GetTempPath(), "ide-session-workspace-" + System.Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(Path.Combine(root, "docs")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "data")) |> ignore
        Directory.CreateDirectory(Path.Combine(root, "config")) |> ignore

        File.WriteAllText(
            Path.Combine(root, "index.md"),
            "# Index\n\n[Lib](docs/lib.md)\n[Data](data/catalog.json)\n[Web](https://example.com/other.md)\n"
        )

        File.WriteAllText(
            Path.Combine(root, "docs", "lib.md"),
            "# Lib\n\n[Tool](../config/tool.toml)\n"
        )

        File.WriteAllText(
            Path.Combine(root, "data", "catalog.json"),
            "{ \"catalog\": \"../config/tool.toml\" }\n"
        )

        File.WriteAllText(
            Path.Combine(root, "config", "tool.toml"),
            "schema = \"../data/schema.yaml\"\n"
        )

        File.WriteAllText(Path.Combine(root, "data", "schema.yaml"), "kind: schema\n")

        root

    [<Fact>]
    member _.``Workspace graph: document files as units, links as enrichment``() =
        let root = createTree ()

        try
            let graph = WorkspaceGraphPort.load root

            Assert.Equal(5, graph.Files.Length)
            Assert.Equal(5, graph.Links.Length)

            let schema = Path.Combine(root, "data", "schema.yaml")

            Assert.True(WorkspaceGraph.hasFile schema graph)

            Assert.Contains(
                { FromPath = Path.Combine(root, "config", "tool.toml"); ToPath = schema },
                graph.Links
            )

            Assert.Contains(
                { FromPath = Path.Combine(root, "index.md"); ToPath = Path.Combine(root, "data", "catalog.json") },
                graph.Links
            )
        finally
            Directory.Delete(root, true)

    [<Fact>]
    member _.``External refs are not links``() =
        let root = createTree ()

        try
            let graph = WorkspaceGraphPort.load root
            let index = Path.Combine(root, "index.md")

            let fromIndex = graph.Links |> List.filter (fun l -> l.FromPath = index)
            Assert.Equal(2, fromIndex.Length)
        finally
            Directory.Delete(root, true)

    [<Fact>]
    member _.``Fingerprint is stable within a run``() =
        let root = createTree ()

        try
            Assert.Equal(WorkspaceGraphPort.fingerprint root, WorkspaceGraphPort.fingerprint root)
        finally
            Directory.Delete(root, true)
