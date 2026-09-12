module AIGuiders.Platform.Modeling.Gdl.Parse.Catalog.Tests.CatalogAuthoringParityTests

open System
open System.IO
open Xunit
open AIGuiders.Platform.Authoring.Command.Bundles
open AIGuiders.Platform.Authoring.Command.Catalog
open AIGuiders.Platform.Execution.CommandPlane.Catalog.CodeGen
open AIGuiders.Platform.Modeling.Gdl.Parse.Catalog

/// Fields consumed by <see cref="CatalogCatalogEmitter"/> (planet, wire ids, surfaces, executors, mcp).
type EmitInputSnapshot =
    { Planet: string
      FederationSurfaces: string list
      WireCommandIds: (string * string) list
      Executors: (string * string) list
      ExposedMcpWireIds: string list
      McpRows: (string * string) list
      CommandIds: string list }

module EmitParity =
    let wireCommandId (planet: string) (localCommand: string) =
        $"{planet}.{localCommand.Replace(' ', '.')}"

    let snapshot (doc: CatalogDocument) : EmitInputSnapshot =
        let planet = doc.Planet

        { Planet = planet
          FederationSurfaces = doc.Defaults.CommandSurfaces |> List.sort
          WireCommandIds =
            doc.Commands
            |> List.map (fun c -> c.Command, wireCommandId planet c.Command)
            |> List.sortBy fst
          Executors = doc.Executors |> Map.toList |> List.sortBy fst
          ExposedMcpWireIds =
            doc.Mcp
            |> List.filter (fun row -> row.Expose.Equals("yes", StringComparison.OrdinalIgnoreCase))
            |> List.map (fun row -> wireCommandId planet row.Command)
            |> List.sort
          McpRows =
            doc.Mcp
            |> List.map (fun row -> row.Command, row.Expose)
            |> List.sortBy fst
          CommandIds = doc.Commands |> List.map (fun c -> c.Command) |> List.sort }

    let assertEqual (expected: EmitInputSnapshot) (actual: EmitInputSnapshot) =
        Assert.Equal(expected.Planet, actual.Planet)
        Assert.Equal<string list>(expected.FederationSurfaces, actual.FederationSurfaces)
        Assert.Equal<string list>(expected.CommandIds, actual.CommandIds)
        Assert.Equal<(string * string) list>(expected.WireCommandIds, actual.WireCommandIds)
        Assert.Equal<(string * string) list>(expected.Executors, actual.Executors)
        Assert.Equal<(string * string) list>(expected.McpRows, actual.McpRows)
        Assert.Equal<string list>(expected.ExposedMcpWireIds, actual.ExposedMcpWireIds)

    let parseAuthoring (text: string) =
        let result = CatalogParser.Parse(text, bundleLibrary = CatalogBundleLibrary.Federation)

        match result.Document with
        | null -> Assert.Fail("Expected authoring catalog document"); failwith "unreachable"
        | doc -> doc.ToModel()

    let loadFixture (name: string) =
        let path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name)
        File.ReadAllText(path)

let inlineSample =
    """catalog inline-emit

defaults
  command.surfaces = toolbar, palette
end defaults

executors
  ping = PingExecutor
end executors

commands table
  | command | help   |
  | ping    | Pong   |
end commands

mcp table
  | command | expose |
  | ping    | yes    |
end mcp
"""

[<Fact>]
let ``Inline fixture: F# parse matches C# authoring ToModel on emit fields`` () =
    let fsharpDoc = Option.get (CatalogParser.parse inlineSample).Document
    let csharpModel = EmitParity.parseAuthoring inlineSample

    EmitParity.assertEqual (EmitParity.snapshot csharpModel) (EmitParity.snapshot fsharpDoc)

    Assert.Equal(csharpModel.Planet, fsharpDoc.Planet)
    Assert.Equal(csharpModel.Defaults.CommandFlavor, fsharpDoc.Defaults.CommandFlavor)
    Assert.Equal<(string * string) list>(Map.toList csharpModel.Executors, Map.toList fsharpDoc.Executors)

[<Fact>]
let ``Emit fixture file: wire ids and federation surfaces match authoring path`` () =
    let text = EmitParity.loadFixture "emit-parity.catalog.gdl"
    let fsharpDoc = Option.get (CatalogParser.parse text).Document
    let csharpModel = EmitParity.parseAuthoring text
    let expected = EmitParity.snapshot csharpModel

    EmitParity.assertEqual expected (EmitParity.snapshot fsharpDoc)

    Assert.Equal("emit-parity.open", EmitParity.wireCommandId fsharpDoc.Planet "open")
    Assert.Equal("emit-parity.sec.add", EmitParity.wireCommandId fsharpDoc.Planet "sec.add")
    Assert.Contains("slash.bar", expected.FederationSurfaces)
    Assert.Contains("palette", expected.FederationSurfaces)

[<Fact>]
let ``CatalogMapping routes match between F# parse and C# authoring model`` () =
    let text = EmitParity.loadFixture "emit-parity.catalog.gdl"
    let fsharpPayload = CatalogMapping.toPayload (Option.get (CatalogParser.parse text).Document)
    let csharpPayload = CatalogMapping.toPayload (EmitParity.parseAuthoring text)

    Assert.Equal(Seq.length csharpPayload.Routes, Seq.length fsharpPayload.Routes)

    for csharpRoute in csharpPayload.Routes do
        let fsharpRoute =
            fsharpPayload.Routes |> Seq.find (fun r -> r.CommandId = csharpRoute.CommandId)

        Assert.Equal(csharpRoute.Path, fsharpRoute.Path)
        Assert.Equal(csharpRoute.Help, fsharpRoute.Help)
        Assert.Equal(csharpRoute.Domain, fsharpRoute.Domain)
        Assert.Equal(csharpRoute.Object, fsharpRoute.Object)
        Assert.Equal(csharpRoute.Intent, fsharpRoute.Intent)
        Assert.Equal(csharpRoute.ArgTailKind, fsharpRoute.ArgTailKind)

[<Fact>]
let ``dash.catalog.gdl: emit input fields parity with C# authoring parse`` () =
    let text = EmitParity.loadFixture (Path.Combine("Authoring", "dash.catalog.gdl"))
    let fsharpDoc = Option.get (CatalogParser.parse text).Document
    let csharpModel = EmitParity.parseAuthoring text

    EmitParity.assertEqual (EmitParity.snapshot csharpModel) (EmitParity.snapshot fsharpDoc)

    Assert.Equal("dash", fsharpDoc.Planet)
    Assert.Equal(Some "console", fsharpDoc.Defaults.CommandFlavor)
    Assert.Contains(fsharpDoc.Commands, fun c -> c.Command = "filter.date")
    let filterChannel = fsharpDoc.Channels |> List.find (fun c -> c.Sub = Some "filter")
    Assert.Equal(Some "command-console", filterChannel.CommandGrammar)

[<Fact>]
let ``dash.catalog.gdl: F# IR bridges to CatalogCatalogEmitter via FromModel`` () =
    let text = EmitParity.loadFixture (Path.Combine("Authoring", "dash.catalog.gdl"))
    let fsharpDoc = Option.get (CatalogParser.parse text).Document
    let csharpDoc = CatalogDocument.FromModel(fsharpDoc)
    let code = CatalogCatalogEmitter.EmitCSharp(csharpDoc, "Parity.Generated", "DashCatalog")

    Assert.Contains("slash.bar", code, StringComparison.Ordinal)
    Assert.Contains("dash.filter.date", code, StringComparison.Ordinal)
    Assert.Contains("dash.host.show", code, StringComparison.Ordinal)

[<Fact>]
let ``dash.catalog.gdl: profile bundle resolution remains C# authoring-only (documented gap)`` () =
    let text = EmitParity.loadFixture (Path.Combine("Authoring", "dash.catalog.gdl"))
    let fsharpDoc = Option.get (CatalogParser.parse text).Document
    let csharpModel = EmitParity.parseAuthoring text

    let fsharpDateProfile = fsharpDoc.Profiles |> List.find (fun p -> p.Name = "date-value")
    let csharpDateProfile = csharpModel.Profiles |> List.find (fun p -> p.Name = "date-value")

    Assert.Equal(Some "date-filter", fsharpDateProfile.BundleSource)
    Assert.True(fsharpDateProfile.Entries.IsEmpty)
    Assert.True(csharpDateProfile.Entries.Length > 0)
    Assert.Contains(csharpDateProfile.Entries, fun e -> e.Entry = "preset" && e.Ref = "today")
