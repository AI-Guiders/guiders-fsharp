namespace AIGuiders.Platform.Modeling.Ide.Session.Ports.Files

open System.IO
open Xunit
open AIGuiders.Platform.Modeling.Ide.Session

type FileGraphProviderTests() =

    let createTree () =
        let root = Path.Combine(Path.GetTempPath(), "ide-session-files-" + System.Guid.NewGuid().ToString("N"))
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
    member _.``File graph: documents as units, references as edges``() =
        let root = createTree ()

        try
            let provider = FileGraphProvider(root) :> ISolutionInfoProvider

            Assert.Equal("file-graph", provider.Name)
            Assert.Equal(5, provider.Entries().Length)
            Assert.Equal(5, provider.Relations().Length)

            let index = Path.Combine(root, "index.md")
            let schema = Path.Combine(root, "data", "schema.yaml")

            Assert.Equal(
                Some (Doc { Extension = ".md" }),
                provider.Entries()
                |> List.tryFind (fun p -> p.AbsolutePath = index)
                |> Option.map (fun p -> p.Kind)
            )

            let targets =
                provider.Relations()
                |> List.map (fun e -> ProjectId.value e.To)
                |> Set.ofList

            Assert.Contains(schema, targets)

            let validation =
                SolutionProviders.toGraph root provider
                |> GraphValidation.validate

            Assert.True(validation.IsValid, validation.Issues |> List.map (fun i -> i.Message) |> String.concat "; ")
        finally
            Directory.Delete(root, true)

    [<Fact>]
    member _.``Fingerprint is stable within a run``() =
        let root = createTree ()

        try
            let provider = FileGraphProvider(root) :> ISolutionInfoProvider
            Assert.Equal(provider.Fingerprint(), provider.Fingerprint())
        finally
            Directory.Delete(root, true)
